namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;

    class RtmpMessageAbort : RtmpMessage
    {
        public RtmpMessageAbort(int targetChunkStreamId)
        {
            this.TargetChunkStreamId = targetChunkStreamId;
            this.MessageType = RtmpIntMessageType.ProtoControlAbort;
        }

        public int TargetChunkStreamId { get; set; }

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
                MessageType = RtmpMessageType.Abort
            };

            PacketBuffer packet = hdr.ToPacketBuffer();
            packet.ActualBufferSize += 4;

            using (EndianBinaryWriter writer = new EndianBinaryWriter(new PacketBufferStream(packet)))
            {
                writer.BaseStream.Seek(4, System.IO.SeekOrigin.End);
                writer.Write(this.TargetChunkStreamId);
            }

            return packet;
        }
    }
}
