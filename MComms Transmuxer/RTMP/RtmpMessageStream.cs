namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;
    using MComms_Transmuxer.SmoothStreaming;

    public class CriticalStreamException : Exception
    {
        public CriticalStreamException(string message)
            : base(message)
        {
        }
    }

    public class RtmpMessageStream : IDisposable
    {
        private MediaType videoMediaType = null;
        private MediaType audioMediaType = null;
        private bool firstVideoFrame = true;
        private bool firstAudioFrame = true;
        private Guid videoStreamId = Guid.Empty;
        private Guid audioStreamId = Guid.Empty;
        private SmoothStreamingSegmenter segmenter = null;
        private string publishName = null;
        private string publishUri = null;
        private bool publishing = false;

        private bool flvDump = Properties.Settings.Default.EnableFlvDump;
        private string flvDumpPath = null;
        private FileStream flvDumpStream = null;
        private long flvFirstTimestamp = -1;
        private RtmpMessageMetadata metadataMessage = null;

        private long timestampFirstSync = long.MinValue;
        private long timestampSync = long.MinValue;
        private long timestampAdjust = 0;
        private DateTime absoluteTimeOrigin = DateTime.Now;
        private bool firstTimestamp = true;

        public RtmpMessageStream(int messageStreamId)
        {
            this.MessageStreamId = messageStreamId;
        }

        public int MessageStreamId { get; set; }

        public string PublishName
        {
            get
            {
                return this.publishName;
            }
            set
            {
                this.publishName = value;
                this.publishUri = Properties.Settings.Default.PublishingRoot + this.publishName + ".isml";
                this.flvDumpPath = Properties.Settings.Default.FlvSaveFolder + this.publishName + (this.MessageStreamId - 1).ToString() + ".flv";
            }
        }

        public string FullPublishName { get; set; }

        public bool Publishing
        {
            get
            {
                return this.publishing;
            }
            set
            {
                this.publishing = value;
                if (this.publishing)
                {
                    if (this.segmenter == null)
                    {
                        this.segmenter = new SmoothStreamingSegmenter(this.publishUri);
                    }
                }
                else
                {
                    if (this.segmenter != null)
                    {
                        this.segmenter.Dispose();
                        this.segmenter = null;
                    }
                }
            }
        }

        #region IDisposable

        /// <summary>
        /// Disposes the segmenter.
        /// </summary>
        public void Dispose()
        {
            if (segmenter != null)
            {
                segmenter.Dispose();
                segmenter = null;
            }

            if (this.flvDumpStream != null)
            {
                this.flvDumpStream.Flush();
                this.flvDumpStream.Close();
                this.flvDumpStream.Dispose();
                this.flvDumpStream = null;
            }
        }

        #endregion

        public void ProcessMetadata(RtmpMessageMetadata msg)
        {
            switch (msg.MessageType)
            {
                case RtmpIntMessageType.DataMetadata:
                    {
                        ProcessStreamMetadata(msg);
                        break;
                    }

                case RtmpIntMessageType.DataTimestamp:
                    {
                        ProcessTimestamp(msg);
                        break;
                    }

                default:
                    {
                        // skip unsupported data
                        return;
                    }
            }
        }

        public void ProcessMediaData(RtmpMessageMedia msg)
        {
            if (this.segmenter == null)
            {
                throw new CriticalStreamException(string.Format("Command {0}, media data is unexpected in current state, dropping session...", msg.MessageType));
            }

            switch (msg.MessageType)
            {
                case RtmpIntMessageType.Audio:
                    this.ProcessAudioData(msg);
                    break;

                case RtmpIntMessageType.Video:
                    this.ProcessVideoData(msg);
                    break;

                default:
                    Global.Log.WarnFormat("Unexpected message type {0}", msg.MessageType);
                    break;
            }

            if (this.flvDump && this.flvDumpStream != null)
            {
                // drop data till the first non-zero video frame
                // or the first non-zero audio frame if there is no video data
                if (this.flvFirstTimestamp <= 0)
                {
                    if (this.videoMediaType == null)
                    {
                        if (msg.MessageType == RtmpIntMessageType.Audio && msg.Timestamp > 0)
                        {
                            this.flvFirstTimestamp = msg.Timestamp;
                        }
                    }
                    else
                    {
                        if (msg.MessageType == RtmpIntMessageType.Video && msg.Timestamp > 0 && msg.KeyFrame)
                        {
                            this.flvFirstTimestamp = msg.Timestamp;
                        }
                    }
                }

                if (this.flvFirstTimestamp > 0 || msg.PacketType == RtmpMediaPacketType.Configuration)
                {
                    //Global.Log.DebugFormat("Writing to FLV: {0}, timestamp {3}, corrected {1}, keyframe {2}", msg.MessageType, (msg.Timestamp - this.flvFirstTimestamp), msg.KeyFrame, msg.Timestamp);

                    PacketBuffer packet = null;

                    try
                    {
                        FlvTagHeader hdr = new FlvTagHeader
                        {
                            TagType = msg.OrigMessageType,
                            DataSize = (uint)msg.MediaData.ActualBufferSize,
                            StreamId = 0,
                            Timestamp = 0
                        };

                        if (msg.PacketType != RtmpMediaPacketType.Configuration)
                        {
                            hdr.Timestamp = (uint)(msg.Timestamp - this.flvFirstTimestamp - this.timestampAdjust);
                        }

                        packet = hdr.ToPacketBuffer();

                        using (EndianBinaryWriter writer = new EndianBinaryWriter(this.flvDumpStream, true))
                        {
                            // write tag header
                            writer.Write(packet.Buffer, 0, packet.ActualBufferSize);
                            // write media data
                            writer.Write(msg.MediaData.Buffer, 0, msg.MediaData.ActualBufferSize);
                            // write previous tag size
                            writer.Write((uint)(hdr.HeaderSize + msg.MediaData.ActualBufferSize));
                        }
                    }
                    catch (Exception ex)
                    {
                        if (this.flvDumpStream != null)
                        {
                            this.flvDumpStream.Dispose();
                            this.flvDumpStream = null;
                        }

                        Global.Log.ErrorFormat("FLV write failed: {0}", ex.ToString());
                    }
                    finally
                    {
                        if (packet != null)
                        {
                            packet.Release();
                        }
                    }
                }
            }
        }

        private void ProcessAudioData(RtmpMessageMedia msg)
        {
            if (msg.AudioCodec != RtmpAudioCodec.AAC)
            {
                // unsupported codec
                throw new CriticalStreamException(string.Format("Command {0}, unsupported audio codec {1}, dropping session...", msg.MessageType, msg.AudioCodec));
            }

            if (this.firstAudioFrame)
            {
                if (this.audioMediaType == null)
                {
                    this.audioMediaType = new MediaType { ContentType = MediaContentType.Audio };
                }

                this.audioMediaType.Codec = MediaCodec.AAC;
                this.audioMediaType.SampleRate = msg.SampleRate;
                this.audioMediaType.Channels = msg.Channels;
                this.audioMediaType.SampleSize = msg.SampleSize;

                // first received frame contains codec private data
#if DEBUG
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < msg.MediaData.ActualBufferSize; ++i)
                {
                    sb.AppendFormat("{0:X2}", msg.MediaData.Buffer[i]);
                }
                Global.Log.DebugFormat("Audio private data: {0}", sb.ToString());
#endif

                // check packet type
                if (msg.PacketType != RtmpMediaPacketType.Configuration)
                {
                    throw new CriticalStreamException(string.Format("Command {0}, not audio configuration packet {1}", msg.MessageType, msg.PacketType));
                }

                // check packet type
                int privateDataLen = msg.MediaData.ActualBufferSize - msg.MediaDataOffset;

                if (privateDataLen <= 0)
                {
                    throw new CriticalStreamException(string.Format("Command {0}, no audio private data found", msg.MessageType));
                }

                if (privateDataLen < 3)
                {
                    throw new CriticalStreamException(string.Format("Command {0}, wrong audio private data format", msg.MessageType));
                }

                this.audioMediaType.PrivateData = new byte[privateDataLen];
                Array.Copy(msg.MediaData.Buffer, msg.MediaData.ActualBufferSize - privateDataLen, this.audioMediaType.PrivateData, 0, privateDataLen);

                this.audioStreamId = this.segmenter.RegisterStream(this.audioMediaType);

                this.firstAudioFrame = false;
            }
            else
            {
                if (this.firstTimestamp)
                {
                    if (this.timestampFirstSync == long.MinValue)
                    {
                        this.absoluteTimeOrigin = this.absoluteTimeOrigin.AddMilliseconds(-msg.Timestamp);
                        Global.Log.DebugFormat("Synchronization info: absolute time {0:yy-MM-dd HH:mm:ss.fff}", this.absoluteTimeOrigin);
                    }

                    this.firstTimestamp = false;
                }

                long adjustedTimestamp = (msg.Timestamp - this.timestampAdjust) * 10000;
                DateTime absoluteTime = this.absoluteTimeOrigin.AddTicks(adjustedTimestamp);

                if (msg.PacketType == RtmpMediaPacketType.Media)
                {
                    // push to Smooth Streaming segmenter
                    this.segmenter.PushMediaData(this.audioStreamId, absoluteTime, adjustedTimestamp, true, msg.MediaData.Buffer, msg.MediaDataOffset, msg.MediaData.ActualBufferSize - msg.MediaDataOffset);
                }
                else if (msg.PacketType == RtmpMediaPacketType.Eos)
                {
                    Global.Log.DebugFormat("Received audio EOS");
                }
                else
                {
                    Global.Log.DebugFormat("Received unexpected audio packet type {0}", msg.PacketType);
                }
            }
        }

        private void ProcessVideoData(RtmpMessageMedia msg)
        {
            if (msg.VideoCodec != RtmpVideoCodec.AVC)
            {
                // unsupported codec
                throw new CriticalStreamException(string.Format("Command {0}, unsupported video codec {1}, dropping session...", msg.MessageType, msg.VideoCodec));
            }

            if (this.firstVideoFrame)
            {
                if (this.videoMediaType == null)
                {
                    this.videoMediaType = new MediaType { ContentType = MediaContentType.Video };
                }

                this.videoMediaType.Codec = MediaCodec.H264;

                // first received frame contains codec private data
#if DEBUG
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < msg.MediaData.ActualBufferSize; ++i)
                {
                    sb.AppendFormat("{0:X2}", msg.MediaData.Buffer[i]);
                }
                Global.Log.DebugFormat("Video private data: {0}", sb.ToString());
#endif

                if (msg.PacketType != RtmpMediaPacketType.Configuration)
                {
                    throw new CriticalStreamException(string.Format("Command {0}, not video configuration packet {1}", msg.MessageType, msg.PacketType));
                }

                // check packet type
                int privateDataLen = msg.MediaData.ActualBufferSize - msg.MediaDataOffset;

                if (privateDataLen <= 0)
                {
                    throw new CriticalStreamException(string.Format("Command {0}, no video private data found", msg.MessageType));
                }

                if (privateDataLen < 7 || msg.MediaData.Buffer[msg.MediaDataOffset] != 1)
                {
                    throw new CriticalStreamException(string.Format("Command {0}, wrong video private data format", msg.MessageType));
                }

                this.videoMediaType.PrivateData = new byte[privateDataLen];
                Array.Copy(msg.MediaData.Buffer, msg.MediaData.ActualBufferSize - privateDataLen, this.videoMediaType.PrivateData, 0, privateDataLen);

                // apply reserved zeroes
                this.videoMediaType.PrivateData[4] |= 0xFC;
                this.videoMediaType.PrivateData[5] |= 0xE0;

                this.videoStreamId = this.segmenter.RegisterStream(this.videoMediaType);

                this.firstVideoFrame = false;
            }
            else
            {
                if (this.firstTimestamp)
                {
                    if (this.timestampFirstSync == long.MinValue)
                    {
                        this.absoluteTimeOrigin = this.absoluteTimeOrigin.AddMilliseconds(-msg.Timestamp);
                        Global.Log.DebugFormat("Synchronization info: absolute time {0:yy-MM-dd HH:mm:ss.fff}", this.absoluteTimeOrigin);
                    }

                    this.firstTimestamp = false;
                }

                long adjustedTimestamp = (msg.Timestamp - this.timestampAdjust) * 10000;
                DateTime absoluteTime = this.absoluteTimeOrigin.AddTicks(adjustedTimestamp);

                if (msg.PacketType == RtmpMediaPacketType.Media)
                {
                    // push to Smooth Streaming segmenter
                    this.segmenter.PushMediaData(this.videoStreamId, absoluteTime, adjustedTimestamp, msg.KeyFrame, msg.MediaData.Buffer, msg.MediaDataOffset, msg.MediaData.ActualBufferSize - msg.MediaDataOffset);
                }
                else if (msg.PacketType == RtmpMediaPacketType.Eos)
                {
                    Global.Log.DebugFormat("Received video EOS");
                }
                else
                {
                    Global.Log.DebugFormat("Received unexpected video packet type {0}", msg.PacketType);
                }
            }
        }

        private void ProcessStreamMetadata(RtmpMessageMetadata msg)
        {
            // store it for flv dump
            this.metadataMessage = msg;

            RtmpAmfObject metadata = (RtmpAmfObject)msg.Parameters[msg.FirstDataIndex];

            if (metadata == null)
            {
                Global.Log.WarnFormat("Command {0}, metadata format not recognized", msg.MessageType);
                return;
            }

            MediaType videoMediaType = new MediaType { ContentType = MediaContentType.Video };
            MediaType audioMediaType = new MediaType { ContentType = MediaContentType.Audio };
            bool videoFound = false;
            bool audioFound = false;

            if (metadata.Numbers.ContainsKey("videocodecid"))
            {
                videoFound = true;
                RtmpVideoCodec videoCodec = (RtmpVideoCodec)(int)metadata.Numbers["videocodecid"];
                if (videoCodec != RtmpVideoCodec.AVC)
                {
                    // unsupported codec
                    throw new CriticalStreamException(string.Format("Command {0}, unsupported video codec {1}, dropping session...", msg.MessageType, videoCodec));
                }
                videoMediaType.Codec = MediaCodec.H264;
            }

            if (metadata.Numbers.ContainsKey("width"))
            {
                videoFound = true;
                videoMediaType.Width = (int)metadata.Numbers["width"];
            }

            if (metadata.Numbers.ContainsKey("height"))
            {
                videoFound = true;
                videoMediaType.Height = (int)metadata.Numbers["height"];
            }

            if (metadata.Numbers.ContainsKey("videoframerate"))
            {
                videoFound = true;
                videoMediaType.Framerate = new Fraction(metadata.Numbers["videoframerate"], 1.0);
            }
            else if (metadata.Numbers.ContainsKey("framerate"))
            {
                videoFound = true;
                videoMediaType.Framerate = new Fraction(metadata.Numbers["framerate"], 1.0);
            }
            else
            {
                videoMediaType.Framerate = new Fraction();
            }

            if (metadata.Numbers.ContainsKey("videodatarate"))
            {
                videoFound = true;
                videoMediaType.Bitrate = (int)metadata.Numbers["videodatarate"] * 1000;
            }

            if (metadata.Numbers.ContainsKey("audiocodecid"))
            {
                videoFound = true;
                RtmpAudioCodec audioCodec = (RtmpAudioCodec)(int)metadata.Numbers["audiocodecid"];
                if (audioCodec != RtmpAudioCodec.AAC)
                {
                    // unsupported codec
                    throw new CriticalStreamException(string.Format("Command {0}, unsupported audio codec {1}, dropping session...", msg.MessageType, audioCodec));
                }
                audioMediaType.Codec = MediaCodec.AAC;
            }

            if (metadata.Numbers.ContainsKey("audioonly"))
            {
                audioFound = true;
            }

            if (metadata.Numbers.ContainsKey("audiodatarate"))
            {
                audioFound = true;
                audioMediaType.Bitrate = (int)metadata.Numbers["audiodatarate"] * 1000;
            }

            if (metadata.Numbers.ContainsKey("audiosamplerate"))
            {
                audioFound = true;
                audioMediaType.SampleRate = (int)metadata.Numbers["audiosamplerate"];
            }

            if (metadata.Numbers.ContainsKey("audiosamplesize"))
            {
                audioFound = true;
                audioMediaType.SampleSize = (int)metadata.Numbers["audiosamplesize"];
            }

            if (metadata.Numbers.ContainsKey("audiochannels"))
            {
                audioFound = true;
                audioMediaType.Channels = (int)metadata.Numbers["audiochannels"];
            }
            else if (metadata.Booleans.ContainsKey("stereo"))
            {
                audioFound = true;
                audioMediaType.Channels = metadata.Booleans["stereo"] ? 2 : 1;
            }

            if (audioFound)
            {
                this.audioMediaType = audioMediaType;
            }

            if (videoFound)
            {
                this.videoMediaType = videoMediaType;
            }

            if (this.flvDump)
            {
                PacketBuffer headerPacket = null;
                PacketBuffer metadataPacket = null;

                try
                {
                    this.flvDumpStream = new FileStream(this.flvDumpPath, FileMode.Create, FileAccess.Write, FileShare.Read);

                    FlvFileHeader fileHeader = new FlvFileHeader(audioFound, videoFound);
                    headerPacket = fileHeader.ToPacketBuffer();
                    metadataPacket = this.metadataMessage.ToFlvTag();

                    using (EndianBinaryWriter writer = new EndianBinaryWriter(this.flvDumpStream, true))
                    {
                        // write file header
                        writer.Write(headerPacket.Buffer, 0, headerPacket.ActualBufferSize);
                        // write first previous tag size 0
                        writer.Write((int)0);
                        // write metadata
                        writer.Write(metadataPacket.Buffer, 0, metadataPacket.ActualBufferSize);
                        // write previous tag size
                        writer.Write(metadataPacket.ActualBufferSize);
                    }
                }
                catch (Exception ex)
                {
                    if (this.flvDumpStream != null)
                    {
                        this.flvDumpStream.Dispose();
                        this.flvDumpStream = null;
                    }

                    Global.Log.ErrorFormat("Can't open FLV dump file for writing: {0}", ex.ToString());
                }
                finally
                {
                    if (headerPacket != null)
                    {
                        headerPacket.Release();
                    }

                    if (metadataPacket != null)
                    {
                        metadataPacket.Release();
                    }
                }
            }
        }

        private void ProcessTimestamp(RtmpMessageMetadata msg)
        {
            RtmpAmfObject timestampData = (RtmpAmfObject)msg.Parameters[msg.FirstDataIndex];
            DateTime absoluteTime = DateTime.MinValue;

            long gap = 0;

            if (timestampData.Strings.ContainsKey("tc"))
            {
                // SMPTE timestamp
                TimeSpan timestamp = new TimeSpan();

                try
                {
                    Regex rg = new Regex(@"^(?<hms>[0-9]{2}:[0-9]{2}:[0-9]{2}):\s*(?<frame>[0-9]{1,3})$");
                    Match m = rg.Match(timestampData.Strings["st"]);

                    if (m.Success && m.Groups["hms"].Success && m.Groups["frame"].Success)
                    {
                        timestamp = TimeSpan.ParseExact(m.Groups["hms"].Value, @"hh\:mm\:ss", CultureInfo.InvariantCulture);

                        long frame = long.Parse(m.Groups["frame"].Value);
                        timestamp += new TimeSpan((long)(frame * 10000000 * this.videoMediaType.Framerate.Den / this.videoMediaType.Framerate.Num));

                        absoluteTime = absoluteTime.AddYears(1000) + timestamp;
                    }
                    else
                    {
                        Global.Log.ErrorFormat("Wrong onFI/tc format: {0} regex failed", timestampData.Strings["tc"]);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Global.Log.ErrorFormat("Wrong onFI/tc format: {0}, exception {1}", timestampData.Strings["tc"], ex.ToString());
                    return;
                }

                gap = timestamp.Ticks / 10000 - msg.Timestamp;
                absoluteTime = absoluteTime.AddMilliseconds(-msg.Timestamp);
            }
            else
            {
                if (timestampData.Strings.ContainsKey("sd"))
                {
                    try
                    {
                        absoluteTime = DateTime.ParseExact(timestampData.Strings["sd"], "dd-MM-yyyy", CultureInfo.InvariantCulture);
                    }
                    catch (Exception)
                    {
                        try
                        {
                            absoluteTime = DateTime.ParseExact(timestampData.Strings["sd"], "dd-MM-yy", CultureInfo.InvariantCulture);
                        }
                        catch (Exception ex)
                        {
                            Global.Log.ErrorFormat("Wrong onFI/sd format: {0}, exception {1}", timestampData.Strings["sd"], ex.ToString());
                            return;
                        }
                    }
                }
                else
                {
                    Global.Log.ErrorFormat("Wrong onFI format: no 'sd' parameter");
                    return;
                }

                if (timestampData.Strings.ContainsKey("st"))
                {
                    try
                    {
                        Regex rg = new Regex(@"^(?<hms>[0-9]{2}:[0-9]{2}:[0-9]{2})\.\s*(?<ms>[0-9]{1,3})$");
                        Match m = rg.Match(timestampData.Strings["st"]);

                        if (m.Success && m.Groups["hms"].Success && m.Groups["ms"].Success)
                        {
                            TimeSpan ts = TimeSpan.ParseExact(m.Groups["hms"].Value, @"hh\:mm\:ss", CultureInfo.InvariantCulture);
                            absoluteTime += ts;

                            int milliseconds = int.Parse(m.Groups["ms"].Value);
                            absoluteTime = absoluteTime.AddMilliseconds(milliseconds);
                        }
                        else
                        {
                            Global.Log.ErrorFormat("Wrong onFI/st format: {0} regex failed", timestampData.Strings["st"]);
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        Global.Log.ErrorFormat("Wrong onFI/st format: {0}, exception {1}", timestampData.Strings["st"], ex.ToString());
                        return;
                    }
                }
                else
                {
                    Global.Log.ErrorFormat("Wrong onFI format: no 'st' parameter");
                    return;
                }

                gap = absoluteTime.Ticks / 10000 - msg.Timestamp;
                absoluteTime = absoluteTime.AddMilliseconds(-msg.Timestamp);
            }

            if (this.timestampFirstSync == long.MinValue)
            {
                Global.Log.DebugFormat("Synchronization info: gap {0}, absolute time {1:yy-MM-dd HH:mm:ss.fff}", gap, absoluteTime);
                this.timestampFirstSync = this.timestampSync = gap;
                this.segmenter.AdjustAbsoluteTime((absoluteTime - this.absoluteTimeOrigin).Ticks);
                this.absoluteTimeOrigin = absoluteTime;
            }
            else
            {
                if (Math.Abs(gap - this.timestampSync) >= 1000)
                {
                    // need resync
                    Global.Log.DebugFormat("Need resync: old gap {0}, new gap {1}, diff {2}, adjustment {3}", this.timestampSync, gap, gap - this.timestampSync, this.timestampFirstSync - gap);
                    this.timestampSync = gap;
                    this.timestampAdjust = this.timestampFirstSync - this.timestampSync;
                }
            }
        }
    }
}
