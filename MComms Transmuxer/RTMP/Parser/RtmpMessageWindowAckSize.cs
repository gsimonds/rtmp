namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;

    public class RtmpMessageWindowAckSize : RtmpMessage
    {
        public RtmpMessageWindowAckSize(uint ackSize)
        {
            this.AckSize = ackSize;
            this.MessageType = RtmpIntMessageType.ProtoControlWindowAknowledgementSize;
            this.OrigMessageType = RtmpMessageType.WindowAknowledgementSize;
        }

        public uint AckSize { get; set; }

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
                MessageType = RtmpMessageType.WindowAknowledgementSize
            };

            PacketBuffer packet = hdr.ToPacketBuffer();
            packet.ActualBufferSize += 4;

            using (EndianBinaryWriter writer = new EndianBinaryWriter(new PacketBufferStream(packet)))
            {
                writer.BaseStream.Seek(4, System.IO.SeekOrigin.End);
                writer.Write(this.AckSize);
            }

            return packet;
        }
    }
}
