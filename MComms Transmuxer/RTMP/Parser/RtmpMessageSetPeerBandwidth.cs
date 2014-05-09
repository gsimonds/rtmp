namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;

    public class RtmpMessageSetPeerBandwidth : RtmpMessage
    {
        public enum LimitTypes : byte
        {
            Hard = 0x00,
            Soft = 0x01,
            Dynamic = 0x02,
        }

        public RtmpMessageSetPeerBandwidth(uint ackSize, LimitTypes limitType)
        {
            this.AckSize = ackSize;
            this.LimitType = limitType;
            this.MessageType = RtmpIntMessageType.ProtoControlSetPeerBandwidth;
            this.OrigMessageType = RtmpMessageType.SetPeerBandwidth;
        }

        public uint AckSize { get; set; }

        public LimitTypes LimitType { get; set; }

        public override PacketBuffer ToRtmpChunk()
        {
            // we need only one chunk for this message
            RtmpChunkHeader hdr = new RtmpChunkHeader
            {
                Format = 0,
                Timestamp = 0,
                ChunkStreamId = 2,
                MessageStreamId = 0,
                MessageLength = 5,
                MessageType = RtmpMessageType.SetPeerBandwidth
            };

            PacketBuffer packet = hdr.ToPacketBuffer();
            packet.ActualBufferSize += 5;

            using (EndianBinaryWriter writer = new EndianBinaryWriter(new PacketBufferStream(packet)))
            {
                writer.BaseStream.Seek(5, System.IO.SeekOrigin.End);
                writer.Write(this.AckSize);
                writer.Write((byte)this.LimitType);
            }

            return packet;
        }
    }
}
