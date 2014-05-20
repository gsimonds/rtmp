namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;

    /// <summary>
    /// RTMP chunk header. Used to parse and generate RTMP chunk header
    /// </summary>
    public class RtmpChunkHeader
    {
        #region Constructor

        /// <summary>
        /// Creates new instance of RtmpChunkHeader
        /// </summary>
        public RtmpChunkHeader()
        {
            this.Timestamp = -1;
            this.TimestampDelta = -1;
            this.MessageLength = -1;
            this.MessageType = RtmpMessageType.Undefined;
            this.MessageStreamId = -1;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets or sets chunk format. Can be 0, 1, 2 or 3.
        /// </summary>
        public byte Format { get; set; }

        /// <summary>
        /// Gets or sets chunk stream id
        /// </summary>
        public uint ChunkStreamId { get; set; }

        /// <summary>
        /// Gets or sets chunk timestamp
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        /// Gets or sets chunk timestamp delta
        /// </summary>
        public long TimestampDelta { get; set; }

        /// <summary>
        /// Gets or sets total length of the message current chunk belongs to
        /// </summary>
        public int MessageLength { get; set; }

        /// <summary>
        /// Gets or sets type of the message current chunk belongs to
        /// </summary>
        public RtmpMessageType MessageType { get; set; }

        /// <summary>
        /// Gets or sets message stream id current chunk belongs to
        /// </summary>
        public int MessageStreamId { get; set; }

        /// <summary>
        /// Gets chunk header size
        /// </summary>
        public int HeaderSize
        {
            get
            {
                int headerSize = 1;
                if (this.ChunkStreamId > 319)
                {
                    headerSize += 2;
                }
                else if (this.ChunkStreamId > 64)
                {
                    headerSize++;
                }

                if (this.Format <= 2)
                {
                    int timestamp = (int)this.TimestampDelta;
                    if (this.Format == 0)
                    {
                        timestamp = (int)this.Timestamp;
                    }

                    if (timestamp >= 0xFFFFFF)
                    {
                        headerSize += 4;
                    }

                    switch (this.Format)
                    {
                        case 0:
                            headerSize += 11;
                            break;
                        case 1:
                            headerSize += 7;
                            break;
                        case 2:
                            headerSize += 3;
                            break;
                    }
                }

                return headerSize;
            }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Decodes chunk header from the specified stream
        /// </summary>
        /// <param name="dataStream">Stream to read data from</param>
        /// <returns>New chunk header if it was decoded, null otherwise</returns>
        public static RtmpChunkHeader Decode(PacketBufferStream dataStream)
        {
            if (dataStream.Length < 1)
            {
                return null; // not enough data
            }

            int totalRead = 0;

            byte byte0 = (byte)dataStream.ReadByte();
            totalRead++;

            RtmpChunkHeader hdr = new RtmpChunkHeader();
            hdr.Format = (byte)((byte0 & 0xC0) >> 6);
            hdr.ChunkStreamId = (uint)(byte0 & 0x3F);

            switch (hdr.ChunkStreamId)
            {
                case 0:
                    {
                        if (dataStream.Length < 2)
                        {
                            dataStream.Seek(-totalRead, System.IO.SeekOrigin.Current);
                            return null; // not enough data
                        }

                        byte byte1 = (byte)dataStream.ReadByte();
                        totalRead++;
                        hdr.ChunkStreamId = (uint)byte1 + 64;

                        break;
                    }

                case 1:
                    {
                        if (dataStream.Length < 3)
                        {
                            dataStream.Seek(-totalRead, System.IO.SeekOrigin.Current);
                            return null; // not enough data
                        }

                        byte byte1 = (byte)dataStream.ReadByte();
                        byte byte2 = (byte)dataStream.ReadByte();
                        totalRead += 2;
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
                dataStream.Seek(-totalRead, System.IO.SeekOrigin.Current);
                return null; // not enough data
            }

            using (EndianBinaryReader reader = new EndianBinaryReader(dataStream, true))
            {
                if (hdr.Format <= 2)
                {
                    int timestamp = reader.ReadInt32(3);
                    totalRead += 3;
                    if (hdr.Format <= 1)
                    {
                        hdr.MessageLength = reader.ReadInt32(3);
                        hdr.MessageType = (RtmpMessageType)reader.ReadByte();
                        totalRead += 4;
                        if (hdr.Format == 0)
                        {
                            hdr.MessageStreamId = reader.ReadInt32(4, Endianness.LittleEndian);
                            totalRead += 4;
                        }
                    }

                    if (timestamp == 0xFFFFFF)
                    {
                        // need to read 4-byte extended timestamp
                        timestamp = reader.ReadInt32();
                        totalRead += 4;
                    }

                    if (hdr.Format == 0)
                    {
                        hdr.Timestamp = timestamp;
                    }
                    else
                    {
                        hdr.TimestampDelta = timestamp;
                    }
                }
            }

            return hdr;
        }

        /// <summary>
        /// Converts current object to a byte array and returns packet buffer containing it
        /// </summary>
        /// <returns>Packet buffer containing the converted byte array</returns>
        public PacketBuffer ToPacketBuffer()
        {
            PacketBuffer packet = Global.Allocator.LockBuffer();
            return ToPacketBuffer(packet);
        }

        /// <summary>
        /// Converts current object to a byte array and returns packet buffer containing it
        /// </summary>
        /// <param name="packet">Packet buffer to write byte data to</param>
        /// <returns>Packet buffer (the same as specified in parameter packet) containing the converted byte array</returns>
        public PacketBuffer ToPacketBuffer(PacketBuffer packet)
        {
            packet.ActualBufferSize = this.HeaderSize;

            using (EndianBinaryWriter writer = new EndianBinaryWriter(new PacketBufferStream(packet)))
            {
                // chunk basic header
                if (this.ChunkStreamId > 319)
                {
                    writer.Write((byte)((this.Format << 6) | 1));
                    writer.Write((byte)((this.ChunkStreamId - 64) % 256));
                    writer.Write((byte)((this.ChunkStreamId - 64) / 256));
                }
                else if (this.ChunkStreamId > 64)
                {
                    writer.Write((byte)(this.Format << 6));
                    writer.Write((byte)(this.ChunkStreamId - 64));
                }
                else
                {
                    writer.Write((byte)((this.Format << 6) | (byte)this.ChunkStreamId));
                }

                // chunk message header
                if (this.Format <= 2)
                {
                    int timestamp = (int)this.TimestampDelta;
                    if (this.Format == 0)
                    {
                        timestamp = (int)this.Timestamp;
                    }

                    int timestampFull = 0;
                    if (timestamp >= 0xFFFFFF)
                    {
                        timestampFull = timestamp;
                        timestamp = 0xFFFFFF;
                    }

                    writer.Write(timestamp, 3);

                    if (this.Format <= 1)
                    {
                        writer.Write(this.MessageLength, 3);
                        writer.Write((byte)this.MessageType);
                        if (this.Format == 0)
                        {
                            writer.Write(this.MessageStreamId, 4, Endianness.LittleEndian);
                        }
                    }

                    if (timestamp == 0xFFFFFF)
                    {
                        // need to read 4-byte extended timestamp
                        writer.Write(timestampFull);
                    }
                }
            }

            return packet;
        }

        #endregion
    }
}
