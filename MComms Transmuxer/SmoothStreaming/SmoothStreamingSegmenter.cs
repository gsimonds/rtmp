namespace MComms_Transmuxer.SmoothStreaming
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;
    using MComms_Transmuxer.RTMP;

    /// <summary>
    /// Smooth streaming segmenter prepares segments, handles synchronization on stream re-connection
    /// </summary>
    public class SmoothStreamingSegmenter : IDisposable
    {
        #region Private constants and fields

        /// <summary>
        /// Pointer to unmanaged buffer used to communicate with unmanaged SSF SDK wrapper
        /// </summary>
        private IntPtr mediaDataPtr = IntPtr.Zero;

        /// <summary>
        /// Unmanaged buffer size
        /// </summary>
        private int mediaDataPtrSize = 0;

        /// <summary>
        /// Publish URI
        /// </summary>
        private string publishUri = null;

        /// <summary>
        /// Smooth streaming publisher
        /// </summary>
        private SmoothStreamingPublisher publisher = null;

        /// <summary>
        /// Map from stream GUID to muxer's stream id
        /// </summary>
        private Dictionary<Guid, int> publishStreamId2MuxerStreamId = new Dictionary<Guid, int>();

        /// <summary>
        /// Whether we're synchronized
        /// </summary>
        private bool synchronized = false;

        /// <summary>
        /// Current timestamp offset
        /// </summary>
        private long timestampOffset = 0;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates new instance of SmoothStreamingSegmenter
        /// </summary>
        /// <param name="publishUri">Publish URI</param>
        public SmoothStreamingSegmenter(string publishUri)
        {
            this.mediaDataPtrSize = Global.MediaAllocator.BufferSize;
            this.mediaDataPtr = Marshal.AllocHGlobal(this.mediaDataPtrSize);
            this.publishUri = publishUri;
            this.publisher = SmoothStreamingPublisher.Create(this.publishUri);
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Releases resources.
        /// </summary>
        public void Dispose()
        {
            if (this.mediaDataPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(this.mediaDataPtr);
                this.mediaDataPtr = IntPtr.Zero;
            }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Registers new stream with specified media type
        /// </summary>
        /// <param name="mediaType">Media type to register</param>
        /// <returns>Registered stream GUID</returns>
        public Guid RegisterStream(MediaType mediaType)
        {
            int streamId = -1;

            Guid publishStreamId = Guid.Empty;

            if (mediaType.ContentType == MediaContentType.Video)
            {
                publishStreamId = this.publisher.RegisterMediaType(mediaType);

                SmoothStreamingSegmenter.MPEG2VIDEOINFO mvih = new SmoothStreamingSegmenter.MPEG2VIDEOINFO();
                mvih.hdr.rcSource = new SmoothStreamingSegmenter.RECT { right = mediaType.Width, bottom = mediaType.Height };
                mvih.hdr.rcTarget = new SmoothStreamingSegmenter.RECT { right = mediaType.Width, bottom = mediaType.Height };
                mvih.hdr.dwBitRate = (uint)mediaType.Bitrate;
                if (mediaType.Framerate.Num != 0 && mediaType.Framerate.Den != 0)
                {
                    mvih.hdr.AvgTimePerFrame = (long)(Global.SmoothStreamingTimescale * mediaType.Framerate.Den / mediaType.Framerate.Num);
                }
                else
                {
                    // framerate not specified, assume 30fps
                    mvih.hdr.AvgTimePerFrame = 333333;
                }
                mvih.hdr.dwPictAspectRatioX = (uint)mediaType.Width;
                mvih.hdr.dwPictAspectRatioY = (uint)mediaType.Height;

                mvih.hdr.bmiHeader.biSize = (uint)Marshal.SizeOf(mvih.hdr.bmiHeader);
                mvih.hdr.bmiHeader.biWidth = mediaType.Width;
                mvih.hdr.bmiHeader.biHeight = mediaType.Height;
                mvih.hdr.bmiHeader.biPlanes = 1;
                mvih.hdr.bmiHeader.biBitCount = 24;
                mvih.hdr.bmiHeader.biCompression = 0x31435641; // the only valid value is 'AVC1' as per Microsoft documentation
                mvih.hdr.bmiHeader.biSizeImage = (uint)(mvih.hdr.bmiHeader.biWidth * mvih.hdr.bmiHeader.biHeight * mvih.hdr.bmiHeader.biBitCount / 8);
                mvih.hdr.bmiHeader.biXPelsPerMeter = 0;
                mvih.hdr.bmiHeader.biYPelsPerMeter = 0;
                mvih.hdr.bmiHeader.biClrUsed = 0;
                mvih.hdr.bmiHeader.biClrImportant = 0;

                mvih.cbSequenceHeader = (uint)mediaType.PrivateData.Length;
                mvih.dwProfile = mediaType.PrivateData[1]; // taking it from AVC configuration record
                mvih.dwLevel = mediaType.PrivateData[3]; // taking it from AVC configuration record
                mvih.dwFlags = (uint)(mediaType.PrivateData[4] & 0x03) + 1; // NAL unit size

                byte[] privateData = new byte[mediaType.PrivateData.Length * 2];
                int privateDataSize = 0;
                int nalUnitLength = (int)mvih.dwFlags;
                StringBuilder sb = new StringBuilder();

                // copy SPS
                sb.Append("00000001");
                int numOfSps = mediaType.PrivateData[5] & 0x1F;
                int byteOffset = 6;
                for (int i = 0; i < numOfSps; ++i)
                {
                    int spsLength = (int)mediaType.PrivateData[byteOffset] << 8 | mediaType.PrivateData[byteOffset + 1];
                    if (i == 0)
                    {
                        byte[] lenBytes = EndianBitConverter.Big.GetBytes(spsLength);
                        Array.Copy(lenBytes, 4 - nalUnitLength, privateData, privateDataSize, nalUnitLength);
                        privateDataSize += nalUnitLength;
                        Array.Copy(mediaType.PrivateData, byteOffset + 2, privateData, privateDataSize, spsLength);
                        privateDataSize += spsLength;
                        for (int j = 0; j < spsLength; ++j)
                        {
                            sb.AppendFormat("{0:x2}", mediaType.PrivateData[byteOffset + 2 + j]);
                        }
                    }
                    byteOffset += 2 + spsLength;
                }

                sb.Append("00000001");
                int numOfPps = mediaType.PrivateData[byteOffset];
                byteOffset++;
                for (int i = 0; i < numOfPps; ++i)
                {
                    int ppsLength = (int)mediaType.PrivateData[byteOffset] << 8 | mediaType.PrivateData[byteOffset + 1];
                    if (i == 0)
                    {
                        byte[] lenBytes = EndianBitConverter.Big.GetBytes(ppsLength);
                        Array.Copy(lenBytes, 4 - nalUnitLength, privateData, privateDataSize, nalUnitLength);
                        privateDataSize += nalUnitLength;
                        Array.Copy(mediaType.PrivateData, byteOffset + 2, privateData, privateDataSize, ppsLength);
                        privateDataSize += ppsLength;
                        for (int j = 0; j < ppsLength; ++j)
                        {
                            sb.AppendFormat("{0:x2}", mediaType.PrivateData[byteOffset + 2 + j]);
                        }
                        break;
                    }
                }

                // save IIS compatible private data,
                // we'll use it later to compare to publishing point's manifest
                mediaType.PrivateDataIisString = sb.ToString();

                int totalDataSize = Marshal.SizeOf(mvih) + privateDataSize;
                Marshal.StructureToPtr(mvih, this.mediaDataPtr, true);
                IntPtr extraDataPtr = IntPtr.Add(this.mediaDataPtr, Marshal.SizeOf(mvih) - 4); // TODO: research this 4 byte diff
                Marshal.Copy(privateData, 0, extraDataPtr, privateDataSize);

                streamId = this.publisher.AddStream(publishStreamId, 2 /* video */, mediaType.Bitrate, 0, totalDataSize - 4, this.mediaDataPtr);
            }
            else if (mediaType.ContentType == MediaContentType.Audio)
            {
                publishStreamId = this.publisher.RegisterMediaType(mediaType);

                WAVEFORMATEX wfx = new WAVEFORMATEX();
                wfx.wFormatTag = 0x00FF; // AAC
                wfx.nChannels = (ushort)mediaType.Channels;
                wfx.nSamplesPerSec = (uint)mediaType.SampleRate;
                wfx.nAvgBytesPerSec = (uint)mediaType.Bitrate / 8;
                wfx.nBlockAlign = 4;
                wfx.wBitsPerSample = 16;
                wfx.cbSize = (ushort)mediaType.PrivateData.Length;

                int totalDataSize = Marshal.SizeOf(wfx) + mediaType.PrivateData.Length;
                Marshal.StructureToPtr(wfx, this.mediaDataPtr, true);
                IntPtr extraDataPtr = IntPtr.Add(this.mediaDataPtr, Marshal.SizeOf(wfx) - 2);
                Marshal.Copy(mediaType.PrivateData, 0, extraDataPtr, mediaType.PrivateData.Length);

                streamId = this.publisher.AddStream(publishStreamId, 1 /* audio */, mediaType.Bitrate, 0, totalDataSize - 2, this.mediaDataPtr);
            }
            else
            {
                throw new CriticalStreamException(string.Format("Unsupported content type {0}", mediaType.ContentType));
            }

            if (streamId < 0)
            {
                throw new CriticalStreamException(string.Format("MCSSF_AddStream failed {0}", streamId));
            }

            if (this.publishStreamId2MuxerStreamId.ContainsKey(publishStreamId))
            {
                this.publishStreamId2MuxerStreamId[publishStreamId] = streamId;
            }
            else
            {
                this.publishStreamId2MuxerStreamId.Add(publishStreamId, streamId);
            }

            return publishStreamId;
        }

        /// <summary>
        /// Prepares segment and pushes media data to publisher when segment is ready
        /// </summary>
        /// <param name="publishStreamId">Stream GUID</param>
        /// <param name="absoluteTime">Current system time</param>
        /// <param name="timestamp">Current timestamp</param>
        /// <param name="keyFrame">Is it keyframe</param>
        /// <param name="buffer">Buffer with media data</param>
        /// <param name="offset">Buffer offset</param>
        /// <param name="length">Buffer length</param>
        public void PushMediaData(Guid publishStreamId, DateTime absoluteTime, long timestamp, bool keyFrame, byte[] buffer, int offset, int length)
        {
            int muxId = -1;
            do
            {
                muxId = this.publisher.GetMuxId(publishStreamId);
                if (muxId < 0)
                {
                    // stream re-registration necessary (eg added another bitrate)
                    MediaType mt = this.publisher.GetMediaType(publishStreamId);
                    if (mt == null)
                    {
                        throw new CriticalStreamException(string.Format("Media type {0} already unregistered", publishStreamId));
                    }
                    else
                    {
                        this.RegisterStream(mt);
                    }
                }
            }
            while (muxId < 0);

            if (!this.synchronized)
            {
                // make sure that publishing point matches to our media
                this.publisher.CompareHeader();

                DateTime lastAbsoluteTime = DateTime.MinValue;
                long lastTimestamp = long.MinValue;
                this.publisher.GetSynchronizationInfo(out lastAbsoluteTime, out lastTimestamp);

                if (lastTimestamp > long.MinValue)
                {
                    // this is a continuation of previously connected stream
                    this.timestampOffset = (absoluteTime - lastAbsoluteTime).Ticks + lastTimestamp - timestamp;
                    Global.Log.DebugFormat("Re-sync after disconnection: offset {0}, last absolute {1:yy-MM-dd HH:mm:ss.fff}, last timestamp {2}, cur absolute {3:yy-MM-dd HH:mm:ss.fff}, cur timestamp {4}", this.timestampOffset, lastAbsoluteTime, lastTimestamp, absoluteTime, timestamp);
                }

                this.synchronized = true;
            }

            long adjustedTimestamp = timestamp + this.timestampOffset;

            if (this.mediaDataPtrSize < length)
            {
                Marshal.FreeHGlobal(this.mediaDataPtr);
                this.mediaDataPtrSize = length * 3 / 2;
                this.mediaDataPtr = Marshal.AllocHGlobal(this.mediaDataPtrSize);
            }

            Marshal.Copy(buffer, offset, this.mediaDataPtr, length);

            int outputDataSize = 0;
            IntPtr outputDataPtr = IntPtr.Zero;
            //Global.Log.DebugFormat("Stream {0}, timestamp {1}, keyframe {2}", streamId, timestamp, keyFrame);
            int pushResult = SmoothStreamingSegmenter.MCSSF_PushMedia(muxId, this.publishStreamId2MuxerStreamId[publishStreamId], adjustedTimestamp, 0, keyFrame, length, mediaDataPtr, out outputDataSize, out outputDataPtr);

            if (pushResult < 0)
            {
                throw new CriticalStreamException(string.Format("MCSSF_PushMedia failed {0} (0x{1:x}), timestamp {2}", pushResult, outputDataSize, adjustedTimestamp));
            }

            if (outputDataSize > 0)
            {
                if (outputDataSize > Global.SegmentAllocator.BufferSize)
                {
                    Global.SegmentAllocator.Reallocate(outputDataSize * 3 / 2, Global.SegmentAllocator.BufferCount);
                }

                PacketBuffer segment = Global.SegmentAllocator.LockBuffer();
                Marshal.Copy(outputDataPtr, segment.Buffer, 0, outputDataSize);
                segment.ActualBufferSize = outputDataSize;

                try
                {
                    // TODO: start from key frame
                    publisher.PushData(publishStreamId, absoluteTime, adjustedTimestamp, segment.Buffer, 0, outputDataSize);
                }
                catch
                {
                    segment.Release();
                    throw;
                }

                segment.Release();
            }
        }

        /// <summary>
        /// Adjusts timestamp offset after timestamps were re-synchronized in RTMP message stream
        /// </summary>
        /// <param name="gap"></param>
        public void AdjustAbsoluteTime(long gap)
        {
            if (this.timestampOffset != 0)
            {
                this.timestampOffset += gap;
                Global.Log.DebugFormat("Re-sync after absolute time adjustment: new offset {0}", this.timestampOffset);
            }
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Corrects header data received from SSF SDK
        /// </summary>
        /// <param name="origHeader">Original header</param>
        /// <param name="properHeader">Proper header</param>
        /// <param name="streamId">Stream id</param>
        /// <param name="mediaType">Media type</param>
        private void CorrectHeader(PacketBuffer origHeader, PacketBuffer properHeader, int streamId, MediaType mediaType)
        {
            long nStreamSMILPos = 0;
            uint nStreamSMILSize = 0;

            long nMoovPos = 0;
            uint nMoovSize = 0;
            long nTrakPos = 0;
            uint nTrakSize = 0;
            long nMdiaPos = 0;
            uint nMdiaSize = 0;
            long nMinfPos = 0;
            uint nMinfSize = 0;
            long nStblPos = 0;
            uint nStblSize = 0;
            long nStsdPos = 0;
            uint nStsdSize = 0;
            long nAvc1Pos = 0;
            uint nAvc1Size = 0;
            long nAvccPos = 0;
            uint nAvccSize = 0;

            MemoryStream ms = new MemoryStream(origHeader.Buffer);

            // find start of StreamManifestBox
            using (EndianBinaryReader reader = new EndianBinaryReader(ms, true))
            {
                reader.Endiannes = Endianness.BigEndian;

                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    long nPos = reader.BaseStream.Position;
                    uint nBlockSize = reader.ReadUInt32();
                    uint nBlockType = reader.ReadUInt32();

                    if (nBlockType == 0x75756964) // uuid
                    {
                        byte[] gbyte = new byte[16];
                        reader.Read(gbyte, 0, 16);
                        Guid guid = new Guid(gbyte);
                        if (guid == new Guid("{1be52f3c-eee4-a340-ae81-5300199dc378}"))
                        {
                            reader.Seek(4, SeekOrigin.Current);
                            reader.Seek((int)(nBlockSize - 28), SeekOrigin.Current);
                        }
                        else if (guid == new Guid("{300bd4a5-14e8-dd11-ba2f-0800200c9a66}"))
                        {
                            nStreamSMILPos = nPos;
                            nStreamSMILSize = nBlockSize;
                            reader.Seek(4, SeekOrigin.Current);
                            reader.Seek((int)(nBlockSize - 28), SeekOrigin.Current);
                        }
                        else
                        {
                            reader.Seek((int)(nBlockSize - 24), SeekOrigin.Current);
                        }
                    }
                    else if (nBlockType == 0x6D6F6F76) // moov
                    {
                        nMoovPos = nPos;
                        nMoovSize = nBlockSize;

                        while (reader.BaseStream.Position < nMoovPos + nMoovSize)
                        {
                            nPos = reader.BaseStream.Position;
                            nBlockSize = reader.ReadUInt32();
                            nBlockType = reader.ReadUInt32();

                            if (nBlockType == 0x7472616B) // trak
                            {
                                nTrakPos = nPos;
                                nTrakSize = nBlockSize;

                                while (reader.BaseStream.Position < nTrakPos + nTrakSize)
                                {
                                    nPos = reader.BaseStream.Position;
                                    nBlockSize = reader.ReadUInt32();
                                    nBlockType = reader.ReadUInt32();

                                    if (nBlockType == 0x6D646961) // mdia
                                    {
                                        nMdiaPos = nPos;
                                        nMdiaSize = nBlockSize;

                                        while (reader.BaseStream.Position < nMdiaPos + nMdiaSize)
                                        {
                                            nPos = reader.BaseStream.Position;
                                            nBlockSize = reader.ReadUInt32();
                                            nBlockType = reader.ReadUInt32();

                                            if (nBlockType == 0x6D696E66) // minf
                                            {
                                                nMinfPos = nPos;
                                                nMinfSize = nBlockSize;

                                                while (reader.BaseStream.Position < nMinfPos + nMinfSize)
                                                {
                                                    nPos = reader.BaseStream.Position;
                                                    nBlockSize = reader.ReadUInt32();
                                                    nBlockType = reader.ReadUInt32();

                                                    if (nBlockType == 0x7374626C) // stbl
                                                    {
                                                        nStblPos = nPos;
                                                        nStblSize = nBlockSize;

                                                        while (reader.BaseStream.Position < nStblPos + nStblSize)
                                                        {
                                                            nPos = reader.BaseStream.Position;
                                                            nBlockSize = reader.ReadUInt32();
                                                            nBlockType = reader.ReadUInt32();

                                                            if (nBlockType == 0x73747364) // stsd
                                                            {
                                                                nStsdPos = nPos;
                                                                nStsdSize = nBlockSize;
                                                                reader.Seek(8, SeekOrigin.Current);

                                                                while (reader.BaseStream.Position < nStsdPos + nStsdSize)
                                                                {
                                                                    nPos = reader.BaseStream.Position;
                                                                    nBlockSize = reader.ReadUInt32();
                                                                    nBlockType = reader.ReadUInt32();

                                                                    if (nBlockType == 0x61766331) // avc1
                                                                    {
                                                                        nAvc1Pos = nPos;
                                                                        nAvc1Size = nBlockSize;

                                                                        reader.Seek(78, SeekOrigin.Current);
                                                                        nPos = reader.BaseStream.Position;
                                                                        nBlockSize = reader.ReadUInt32();
                                                                        nBlockType = reader.ReadUInt32();

                                                                        if (nBlockType == 0x61766343) // avcC
                                                                        {
                                                                            nAvccPos = nPos;
                                                                            nAvccSize = nBlockSize;
                                                                        }
                                                                        else
                                                                        {
                                                                            reader.Seek((int)(nBlockSize - 8 - 78 - 8), SeekOrigin.Current);
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        reader.Seek((int)(nBlockSize - 8), SeekOrigin.Current);
                                                                    }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                reader.Seek((int)(nBlockSize - 8), SeekOrigin.Current);
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        reader.Seek((int)(nBlockSize - 8), SeekOrigin.Current);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                reader.Seek((int)(nBlockSize - 8), SeekOrigin.Current);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        reader.Seek((int)(nBlockSize - 8), SeekOrigin.Current);
                                    }
                                }
                            }
                            else
                            {
                                reader.Seek((int)(nBlockSize - 8), SeekOrigin.Current);
                            }
                        }
                    }
                    else
                    {
                        reader.Seek((int)(nBlockSize - 8), SeekOrigin.Current);
                    }
                }
            }

            if (nStreamSMILPos > 0 && nAvccPos > 0)
            {
                StringBuilder sb = new StringBuilder();

                // SPS start code
                sb.Append("00000001");
                int numOfSps = mediaType.PrivateData[5] & 0x1F;
                int byteOffset = 6;
                for (int i = 0; i < numOfSps; ++i)
                {
                    int spsLength = (int)mediaType.PrivateData[byteOffset] << 8 | mediaType.PrivateData[byteOffset + 1];
                    byteOffset += 2;
                    for (int j = 0; i < spsLength; ++j)
                    {
                        sb.AppendFormat("{0:X2}", mediaType.PrivateData[byteOffset + j]);
                    }
                    byteOffset += spsLength;
                }

                // PPS start code
                sb.Append("00000001");
                int numOfPps = mediaType.PrivateData[byteOffset];
                byteOffset++;
                for (int i = 0; i < numOfPps; ++i)
                {
                    int ppsLength = (int)mediaType.PrivateData[byteOffset] << 8 | mediaType.PrivateData[byteOffset + 1];
                    byteOffset += 2;
                    for (int j = 0; i < ppsLength; ++j)
                    {
                        sb.AppendFormat("{0:X2}", mediaType.PrivateData[byteOffset + j]);
                    }
                    byteOffset += ppsLength;
                }

                string sSmil = string.Format
                (
                    @"<?xml version=""1.0"" encoding=""utf-8""?><smil xmlns=""http://www.w3.org/2001/SMIL20/Language""><head></head><body><switch><video src=""Streams"" systemBitrate=""{0}""><param name=""trackID"" value=""{1}"" valuetype=""data""/><param name=""FourCC"" value=""H264"" valuetype=""data""/><param name=""CodecPrivateData"" value=""{2}"" valuetype=""data""/><param name=""MaxWidth"" value=""{3}"" valuetype=""data""/><param name=""MaxHeight"" value=""{4}"" valuetype=""data""/><param name=""Subtype"" value=""H264"" valuetype=""data""/></video></switch></body></smil>",
                    mediaType.Bitrate,
                    streamId + 1,
                    sb.ToString(),
                    mediaType.Width,
                    mediaType.Height
                );
                byte[] bySmil = Encoding.UTF8.GetBytes(sSmil);

                ms.Position = 0;
                int nXmlGap = sSmil.Length + 28 - (int)nStreamSMILSize;
                int nGap = mediaType.PrivateData.Length + 8 - (int)nAvccSize;

                // adjust block sizes
                using (EndianBinaryWriter writer = new EndianBinaryWriter(ms, true))
                {

                    writer.Endiannes = Endianness.BigEndian;

                    writer.BaseStream.Seek(nStreamSMILPos, SeekOrigin.Begin);
                    writer.Write((uint)(nStreamSMILSize + nXmlGap));

                    writer.BaseStream.Seek(nMoovPos, SeekOrigin.Begin);
                    writer.Write((uint)(nMoovSize + nGap));
                    writer.BaseStream.Seek(nTrakPos, SeekOrigin.Begin);
                    writer.Write((uint)(nTrakSize + nGap));
                    writer.BaseStream.Seek(nMdiaPos, SeekOrigin.Begin);
                    writer.Write((uint)(nMdiaSize + nGap));
                    writer.BaseStream.Seek(nMinfPos, SeekOrigin.Begin);
                    writer.Write((uint)(nMinfSize + nGap));
                    writer.BaseStream.Seek(nStblPos, SeekOrigin.Begin);
                    writer.Write((uint)(nStblSize + nGap));
                    writer.BaseStream.Seek(nStsdPos, SeekOrigin.Begin);
                    writer.Write((uint)(nStsdSize + nGap));
                    writer.BaseStream.Seek(nAvc1Pos, SeekOrigin.Begin);
                    writer.Write((uint)(nAvc1Size + nGap));
                    writer.BaseStream.Seek(nAvccPos, SeekOrigin.Begin);
                    writer.Write((uint)(nAvccSize + nGap));

                }

                // copy data before stream SMIL
                Array.Copy(origHeader.Buffer, 0, properHeader.Buffer, 0, nStreamSMILPos + 28);

                // copy re-created SMIL
                Array.Copy(bySmil, 0, properHeader.Buffer, nStreamSMILPos + 28, bySmil.Length);

                // copy data between SMIL and avcC block
                Array.Copy(origHeader.Buffer, nStreamSMILPos + nStreamSMILSize, properHeader.Buffer, nStreamSMILPos + nStreamSMILSize + nXmlGap, nAvccPos - nStreamSMILPos - nStreamSMILSize + 8);
                long nDestPos = nXmlGap + nAvccPos + 8;

                // copy real private data
                Array.Copy(mediaType.PrivateData, 0, properHeader.Buffer, nDestPos, mediaType.PrivateData.Length);
                nDestPos += mediaType.PrivateData.Length;

                // copy remaining data after avcC block
                Array.Copy(origHeader.Buffer, nAvccPos + nAvccSize, properHeader.Buffer, nDestPos, nMoovSize - (nAvccPos + nAvccSize - nMoovPos));

                properHeader.ActualBufferSize = origHeader.ActualBufferSize + nXmlGap + nGap;

            }
        }

        #endregion

        #region Unmanaged interface to MCommsSSFSDK

        /// <summary>
        /// WAVEFORMATEX structure
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct WAVEFORMATEX
        {
            public ushort wFormatTag;
            public ushort nChannels;
            public uint nSamplesPerSec;
            public uint nAvgBytesPerSec;
            public ushort nBlockAlign;
            public ushort wBitsPerSample;
            public ushort cbSize;
        }

        /// <summary>
        /// RECT structure
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public Int32 left;
            public Int32 top;
            public Int32 right;
            public Int32 bottom;
        }

        /// <summary>
        /// BITMAPINFOHEADER structure
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFOHEADER
        {
            public UInt32 biSize;
            public Int32 biWidth;
            public Int32 biHeight;
            public UInt16 biPlanes;
            public UInt16 biBitCount;
            public UInt32 biCompression;
            public UInt32 biSizeImage;
            public Int32 biXPelsPerMeter;
            public Int32 biYPelsPerMeter;
            public UInt32 biClrUsed;
            public UInt32 biClrImportant;
        }

        /// <summary>
        /// VIDEOINFOHEADER2 structure
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct VIDEOINFOHEADER2
        {
            public RECT rcSource;
            public RECT rcTarget;
            public UInt32 dwBitRate;
            public UInt32 dwBitErrorRate;
            public Int64 AvgTimePerFrame;
            public UInt32 dwInterlaceFlags; // use AMINTERLACE_* defines. Reject connection if undefined bits are not 0
            public UInt32 dwCopyProtectFlags; // use AMCOPYPROTECT_* defines. Reject connection if undefined bits are not 0
            public UInt32 dwPictAspectRatioX; // X dimension of picture aspect ratio, e.g. 16 for 16x9 display
            public UInt32 dwPictAspectRatioY; // Y dimension of picture aspect ratio, e.g.  9 for 16x9 display
            public UInt32 dwControlFlags; // use AMCONTROL_* defines, use this from now on
            public UInt32 dwReserved2; // must be 0; reject connection otherwise
            public BITMAPINFOHEADER bmiHeader;
        }

        /// <summary>
        /// MPEG2VIDEOINFO structure
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct MPEG2VIDEOINFO
        {
            public VIDEOINFOHEADER2 hdr;
            public UInt32 dwStartTimeCode;
            public UInt32 cbSequenceHeader;
            public UInt32 dwProfile;
            public UInt32 dwLevel;
            public UInt32 dwFlags;
        }

        /// <summary>
        /// Initializes new muxer session
        /// </summary>
        /// <returns>Mux id</returns>
        [DllImport("MCommsSSFSDK.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int MCSSF_Initialize();

        /// <summary>
        /// Adds new stream to amux
        /// </summary>
        /// <param name="muxId">Mux id</param>
        /// <param name="streamType">Stream type</param>
        /// <param name="bitrate">Bitrate</param>
        /// <param name="language">Language</param>
        /// <param name="extraDataSize">Codec private data size</param>
        /// <param name="extraData">Codec private data</param>
        /// <returns>Stream id</returns>
        [DllImport("MCommsSSFSDK.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int MCSSF_AddStream(
            [In] Int32 muxId,
            [In] Int32 streamType,
            [In] Int32 bitrate,
            [In] UInt16 language,
            [In] Int32 extraDataSize,
            [In] IntPtr extraData);

        /// <summary>
        /// Gets stream header
        /// </summary>
        /// <param name="muxId">Mux id</param>
        /// <param name="streamId">Stream id</param>
        /// <param name="dataSize">Header data size</param>
        /// <param name="data">Header data</param>
        /// <returns>Less than zero if error, 1 if success</returns>
        [DllImport("MCommsSSFSDK.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int MCSSF_GetHeader(
            [In] Int32 muxId,
            [In] Int32 streamId,
            [Out] out Int32 dataSize,
            [Out] out IntPtr data);

        /// <summary>
        /// Adds new media data to segment
        /// </summary>
        /// <param name="muxId">Mux id</param>
        /// <param name="streamId">Stream id</param>
        /// <param name="startTime">Start time</param>
        /// <param name="stopTime">Stop time</param>
        /// <param name="keyFrame">Is key frame</param>
        /// <param name="sampleDataSize">Sample data size</param>
        /// <param name="sampleData">Sample data</param>
        /// <param name="outputDataSize">Segment data size, can be 0</param>
        /// <param name="outputData">Segment data, can be IntPtr.Zero</param>
        /// <returns>Less than zero if error, 0 if segment is not readyyet, 1 if segment is ready</returns>
        [DllImport("MCommsSSFSDK.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int MCSSF_PushMedia(
            [In] Int32 muxId,
            [In] Int32 streamId,
            [In] Int64 startTime,
            [In] Int64 stopTime,
            [In] bool keyFrame,
            [In] Int32 sampleDataSize,
            [In] IntPtr sampleData,
            [Out] out Int32 outputDataSize,
            [Out] out IntPtr outputData);

        /// <summary>
        /// Gets stream index data
        /// </summary>
        /// <param name="muxId">Mux id</param>
        /// <param name="streamId">Stream id</param>
        /// <param name="dataSize">Data size</param>
        /// <param name="data">Data</param>
        /// <returns>Less than zero if error, 1 if success</returns>
        [DllImport("MCommsSSFSDK.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int MCSSF_GetIndex(
            [In] Int32 muxId,
            [In] Int32 streamId,
            [Out] out Int32 dataSize,
            [Out] out IntPtr data);

        /// <summary>
        /// Releases all resources associated with specified mux id
        /// </summary>
        /// <param name="muxId">Mux id to release</param>
        /// <returns>1 if released, 0 if nothing to release (already released)</returns>
        [DllImport("MCommsSSFSDK.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int MCSSF_Uninitialize([In] Int32 muxId);

        #endregion
    }
}
