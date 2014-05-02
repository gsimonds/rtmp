namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
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
        private int videoStreamId = -1;
        private int audioStreamId = -1;
        private bool headerSent = false;
        private SmoothStreamingSegmenter segmenter = null;
        private string publishName = null;
        private string publishUri = null;
        private bool publishing = false;

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
                // TODO: set proper
                this.publishUri = "http://192.168.0.101/sspush.isml";
            }
        }

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
        }

        #endregion

        public void ProcessMetadata(RtmpMessageMetadata msg)
        {
            RtmpAmfObject metadata = null;
            if (msg.Parameters.Count >= 2)
            {
                int startIndex = msg.Parameters.Count;

                if ((string)msg.Parameters[0] == "@setDataFrame")
                {
                    if (msg.Parameters.Count >= 3 && (string)msg.Parameters[1] == "onMetaData")
                    {
                        startIndex = 2;
                    }
                }
                else if ((string)msg.Parameters[0] == "onMetaData")
                {
                    if (msg.Parameters[1].GetType() == typeof(RtmpAmfObject))
                    {
                        startIndex = 1;
                    }
                }

                for (int i = 2; i < msg.Parameters.Count; ++i)
                {
                    if (msg.Parameters[i].GetType() == typeof(RtmpAmfObject))
                    {
                        metadata = (RtmpAmfObject)msg.Parameters[i];
                        break;
                    }
                }
            }

            if (metadata != null)
            {
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
            }
            else
            {
                Global.Log.WarnFormat("Command {0}, metadata format not recognized", msg.MessageType);
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
                if (!this.headerSent)
                {
                    this.PublishHeader();
                }

                // start pushing media data after header has been sent
                if (this.headerSent)
                {
                    if (msg.PacketType == RtmpMediaPacketType.Media)
                    {
                        // push to Smooth Streaming segmenter
                        this.segmenter.PushMediaData(this.audioStreamId, msg.Timestamp * 10000, true, msg.MediaData.Buffer, msg.MediaDataOffset, msg.MediaData.ActualBufferSize - msg.MediaDataOffset);
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
                if (!this.headerSent)
                {
                    this.PublishHeader();
                }

                // start pushing media data after header has been sent
                if (this.headerSent)
                {
                    if (msg.PacketType == RtmpMediaPacketType.Media)
                    {
                        // push to Smooth Streaming segmenter
                        this.segmenter.PushMediaData(this.videoStreamId, msg.Timestamp * 10000, msg.KeyFrame, msg.MediaData.Buffer, msg.MediaDataOffset, msg.MediaData.ActualBufferSize - msg.MediaDataOffset);
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
        }

        private void PublishHeader()
        {
            if (this.headerSent) return;

            // we havent's received audio configuration yet
            if (this.audioMediaType != null && this.audioStreamId < 0) return;

            // we havent's received video configuration yet
            if (this.videoMediaType != null && this.videoStreamId < 0) return;

            if (this.audioStreamId >= 0)
            {
                this.segmenter.PushHeader(this.audioStreamId);
            }

            if (this.videoStreamId >= 0)
            {
                this.segmenter.PushHeader(this.videoStreamId);
            }

            this.headerSent = true;
        }
    }
}
