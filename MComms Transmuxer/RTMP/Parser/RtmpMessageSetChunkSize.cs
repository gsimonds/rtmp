namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;

    /// <summary>
    /// RTMP message "SetChunkSize"
    /// </summary>
    public class RtmpMessageSetChunkSize : RtmpMessage
    {
        /// <summary>
        /// Chunk size to be used
        /// </summary>
        private uint chunkSize = Global.RtmpDefaultChunkSize;

        /// <summary>
        /// Creates new instance of RtmpMessageSetChunkSize
        /// </summary>
        /// <param name="chunkSize">Chunk size to be used</param>
        public RtmpMessageSetChunkSize(uint chunkSize)
        {
            this.ChunkSize = chunkSize;
            this.MessageType = RtmpIntMessageType.ProtoControlSetChunkSize;
            this.OrigMessageType = RtmpMessageType.SetChunkSize;
        }

        /// <summary>
        /// Gets or sets chunk size to be used
        /// </summary>
        public uint ChunkSize
        {
            get { return this.chunkSize; }
            set
            {
                this.chunkSize = value & 0x7FFFFFFF;
                if (this.chunkSize > 0xFFFFFF)
                {
                    this.chunkSize = 0xFFFFFF;
                }
            }
        }

        /// <summary>
        /// Converts current object to RTMP chunk and returns packet buffer containing it
        /// </summary>
        /// <returns>Packet buffer containing the converted RTMP chunk</returns>
        public override PacketBuffer ToRtmpChunk()
        {
            // we need only one chunk for this message
            RtmpChunkHeader hdr = new RtmpChunkHeader
            {
                Format = 0,
                Timestamp = 0,
                ChunkStreamId = 2,
                MessageStreamId = 0,
                MessageLength = 4,
                MessageType = RtmpMessageType.SetChunkSize
            };

            PacketBuffer packet = hdr.ToPacketBuffer();
            packet.ActualBufferSize += 4;

            using (EndianBinaryWriter writer = new EndianBinaryWriter(new PacketBufferStream(packet)))
            {
                writer.BaseStream.Seek(4, System.IO.SeekOrigin.End);
                writer.Write(this.ChunkSize);
            }

            return packet;
        }
    }
}
