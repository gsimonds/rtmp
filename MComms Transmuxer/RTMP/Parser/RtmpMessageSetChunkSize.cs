namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;

    class RtmpMessageSetChunkSize : RtmpMessage
    {
        private uint chunkSize = Global.RtmpDefaultChunkSize;

        public RtmpMessageSetChunkSize(uint chunkSize)
        {
            this.ChunkSize = chunkSize;
            this.MessageType = RtmpIntMessageType.ProtoControlSetChunkSize;
        }

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

        public override PacketBuffer ToPacketBuffer()
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
