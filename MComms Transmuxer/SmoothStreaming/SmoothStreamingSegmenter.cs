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

    // TODO: 32/64bit dlls
    public class SmoothStreamingSegmenter : IDisposable
    {
        private int muxId = -1;
        private IntPtr mediaDataPtr = IntPtr.Zero;
        private int mediaDataPtrSize = 0;
        private string publishUri = null;
        private SmoothStreamingPublisher publisher = null;

        public SmoothStreamingSegmenter(string publishUri)
        {
            this.muxId = SmoothStreamingSegmenter.MCSSF_Initialize();
            this.mediaDataPtrSize = Global.MediaAllocator.BufferSize;
            this.mediaDataPtr = Marshal.AllocHGlobal(this.mediaDataPtrSize);
            this.publishUri = publishUri;
        }

        #region IDisposable

        /// <summary>
        /// Releases resources.
        /// </summary>
        public void Dispose()
        {
            if (this.publisher != null)
            {
                this.publisher.Dispose();
                this.publisher = null;
            }

            if (this.muxId >= 0)
            {
                SmoothStreamingSegmenter.MCSSF_Uninitialize(this.muxId);
                this.muxId = -1;
            }

            if (this.mediaDataPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(this.mediaDataPtr);
                this.mediaDataPtr = IntPtr.Zero;
            }
        }

        #endregion

        public int RegisterStream(MediaType mediaType)
        {
            int streamId = -1;

            if (mediaType.ContentType == MediaContentType.Video)
            {
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

                // copy SPS
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
                    }
                    byteOffset += 2 + spsLength;
                }

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
                        break;
                    }
                }

                int totalDataSize = Marshal.SizeOf(mvih) + privateDataSize;
                Marshal.StructureToPtr(mvih, this.mediaDataPtr, true);
                IntPtr extraDataPtr = IntPtr.Add(this.mediaDataPtr, Marshal.SizeOf(mvih) - 4); // TODO: research this 4 byte diff
                Marshal.Copy(privateData, 0, extraDataPtr, privateDataSize);

                streamId = SmoothStreamingSegmenter.MCSSF_AddStream(this.muxId, 2 /* video */, mediaType.Bitrate, 0, totalDataSize - 4, this.mediaDataPtr);
            }
            else if (mediaType.ContentType == MediaContentType.Audio)
            {
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

                streamId = SmoothStreamingSegmenter.MCSSF_AddStream(this.muxId, 1 /* audio */, mediaType.Bitrate, 0, totalDataSize - 2, this.mediaDataPtr);
            }
            else
            {
                throw new CriticalStreamException(string.Format("Unsupported content type {0}", mediaType.ContentType));
            }

            if (streamId < 0)
            {
                throw new CriticalStreamException(string.Format("MCSSF_AddStream failed {0}", streamId));
            }

            return streamId;
        }

        public void PushHeader(int streamId)
        {
            int headerSize = 0;
            IntPtr headerPtr = IntPtr.Zero;
            int res = SmoothStreamingSegmenter.MCSSF_GetHeader(this.muxId, streamId, out headerSize, out headerPtr);
            if (res < 0)
            {
                throw new CriticalStreamException(string.Format("MCSSF_GetHeader failed {0}", res));
            }

            if (headerSize > Global.MediaAllocator.BufferSize)
            {
                // increase buffer sizes
                Global.MediaAllocator.Reallocate(headerSize * 3 / 2, Global.MediaAllocator.BufferCount);
            }

            PacketBuffer header = Global.MediaAllocator.LockBuffer();
            Marshal.Copy(headerPtr, header.Buffer, 0, headerSize);
            header.ActualBufferSize = headerSize;

            //PacketBuffer tmp = Global.MediaAllocator.LockBuffer();
            //Marshal.Copy(headerPtr, tmp.Buffer, 0, headerSize);

            try
            {
                //this.CorrectHeader(tmp, header, streamId, mediaType);
                if (header.ActualBufferSize > 0)
                {
                    if (this.publisher == null)
                    {
                        this.publisher = new SmoothStreamingPublisher(this.publishUri);
                    }

                    publisher.PushData(streamId, header.Buffer, 0, header.ActualBufferSize);
                }
            }
            catch
            {
                //tmp.Release();
                header.Release();
                throw;
            }

            //tmp.Release();
            header.Release();
        }

        public void PushMediaData(int streamId, long timestamp, bool keyFrame, byte[] buffer, int offset, int length)
        {
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
            int pushResult = SmoothStreamingSegmenter.MCSSF_PushMedia(this.muxId, streamId, timestamp, 0, keyFrame, length, mediaDataPtr, out outputDataSize, out outputDataPtr);

            if (pushResult < 0)
            {
                throw new CriticalStreamException(string.Format("MCSSF_PushMedia failed {0}", pushResult));
            }

            if (outputDataSize > 0)
            {
                if (outputDataSize > Global.SegmentAllocator.BufferSize)
                {
                    Global.SegmentAllocator.Reallocate(outputDataSize * 3 / 2, Global.SegmentAllocator.BufferCount);
                }

                PacketBuffer segment = Global.SegmentAllocator.LockBuffer();
                Marshal.Copy(outputDataPtr, segment.Buffer, 0, outputDataSize);

                try
                {
                    publisher.PushData(streamId, segment.Buffer, 0, outputDataSize);
                }
                catch
                {
                    segment.Release();
                    throw;
                }

                segment.Release();
            }
        }

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

        #region Unmanaged interface to MCommsSSFSDK

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

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public Int32 left;
            public Int32 top;
            public Int32 right;
            public Int32 bottom;
        }

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

        [DllImport("MCommsSSFSDK.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int MCSSF_Initialize();

        [DllImport("MCommsSSFSDK.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int MCSSF_AddStream(
            [In] Int32 muxId,
            [In] Int32 streamType,
            [In] Int32 bitrate,
            [In] UInt16 language,
            [In] Int32 extraDataSize,
            [In] IntPtr extraData);

        [DllImport("MCommsSSFSDK.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int MCSSF_GetHeader(
            [In] Int32 muxId,
            [In] Int32 streamId,
            [Out] out Int32 dataSize,
            [Out] out IntPtr data);

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

        [DllImport("MCommsSSFSDK.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int MCSSF_GetIndex(
            [In] Int32 muxId,
            [In] Int32 streamId,
            [Out] out Int32 dataSize,
            [Out] out IntPtr data);

        [DllImport("MCommsSSFSDK.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int MCSSF_Uninitialize([In] Int32 muxId);

        #endregion
    }
}
