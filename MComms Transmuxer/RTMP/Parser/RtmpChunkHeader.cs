﻿namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;

    class RtmpChunkHeader
    {
        public RtmpChunkHeader()
        {
            this.Timestamp = -1;
            this.MessageLength = -1;
            this.MessageType = RtmpMessageType.Undefined;
            this.MessageStreamId = -1;
        }

        public byte Format { get; set; }

        public uint ChunkStreamId { get; set; }

        /// <summary>
        /// Timestamp or timestamp delta depending on chunk format
        /// </summary>
        public long Timestamp { get; set; }

        public int MessageLength { get; set; }

        public RtmpMessageType MessageType { get; set; }

        public int MessageStreamId { get; set; }

        public static RtmpChunkHeader Decode(PacketBufferStream dataStream)
        {
            if (dataStream.Length < 1)
            {
                return null; // not enough data
            }

            byte byte0 = (byte)dataStream.ReadByte();

            RtmpChunkHeader hdr = new RtmpChunkHeader();
            hdr.Format = (byte)((byte0 & 0xC0) >> 6);
            hdr.ChunkStreamId = (uint)(byte0 & 0x3F);

            switch (hdr.ChunkStreamId)
            {
                case 0:
                    {
                        if (dataStream.Length < 2)
                        {
                            return null; // not enough data
                        }

                        byte byte1 = (byte)dataStream.ReadByte();
                        hdr.ChunkStreamId = (uint)byte1 + 64;

                        break;
                    }

                case 1:
                    {
                        if (dataStream.Length < 3)
                        {
                            return null; // not enough data
                        }

                        byte byte1 = (byte)dataStream.ReadByte();
                        byte byte2 = (byte)dataStream.ReadByte();
                        hdr.ChunkStreamId = 64 + (uint)byte1 + (uint)byte2 * 256;

                        break;
                    }
            }

            int requiredSize = 0;
            switch (hdr.Format)
            {
                case 0:
                    requiredSize = 11;
                    break;
                case 1:
                    requiredSize = 7;
                    break;
                case 2:
                    requiredSize = 3;
                    break;
                case 3:
                    requiredSize = 0;
                    break;
            }

            if (requiredSize > dataStream.Length - dataStream.Position)
            {
                return null; // not enough data
            }

            using (EndianBinaryReader reader = new EndianBinaryReader(dataStream, true))
            {
                if (hdr.Format <= 2)
                {
                    hdr.Timestamp = reader.ReadInt32(3);
                    if (hdr.Format <= 1)
                    {
                        hdr.MessageLength = reader.ReadInt32(3);
                        hdr.MessageType = (RtmpMessageType)reader.ReadByte();
                        if (hdr.Format == 0)
                        {
                            hdr.MessageStreamId = reader.ReadInt32(4, Endianness.LittleEndian);
                        }
                    }

                    if (hdr.Timestamp == 0xFFFFFF)
                    {
                        // need to read 4-byte extended timestamp
                        hdr.Timestamp = reader.ReadInt32();
                    }
                }
            }

            return hdr;
        }
    }
}
