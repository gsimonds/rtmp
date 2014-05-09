namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;

    public class RtmpMessageUserControl : RtmpMessage
    {
        public enum EventTypes : ushort
        {
            StreamBegin = 0x00,
            StreamEOF = 0x01,
            StreamDry = 0x02,
            SetBufferLength = 0x03,
            StreamIsRecorded = 0x04,
            PingRequest = 0x06,
            PingResponse = 0x07,
        }

        public RtmpMessageUserControl(EventTypes eventType, int targetMessageStreamId)
        {
            this.EventType = eventType;
            this.TargetMessageStreamId = targetMessageStreamId;
            this.MessageType = RtmpIntMessageType.ProtoControlUserControl;
            this.OrigMessageType = RtmpMessageType.UserControl;
        }

        public RtmpMessageUserControl(EventTypes eventType, int targetMessageStreamId, uint bufferLength)
        {
            this.EventType = eventType;
            this.TargetMessageStreamId = targetMessageStreamId;
            this.BufferLength = bufferLength;
            this.MessageType = RtmpIntMessageType.ProtoControlUserControl;
            this.OrigMessageType = RtmpMessageType.UserControl;
        }

        public RtmpMessageUserControl(EventTypes eventType, uint timestamp)
        {
            this.EventType = eventType;
            this.PingTimestamp = timestamp;
            this.MessageType = RtmpIntMessageType.ProtoControlUserControl;
            this.OrigMessageType = RtmpMessageType.UserControl;
        }

        public EventTypes EventType { get; set; }

        public int TargetMessageStreamId { get; set; }

        public uint BufferLength { get; set; }

        public uint PingTimestamp { get; set; }

        public override PacketBuffer ToRtmpChunk()
        {
            // we need only one chunk for this message
            int messageLength = 6;
            switch (this.EventType)
            {
                case RtmpMessageUserControl.EventTypes.SetBufferLength:
                    messageLength = 10;
                    break;
            }

            RtmpChunkHeader hdr = new RtmpChunkHeader
            {
                Format = 0,
                Timestamp = this.PingTimestamp,
                ChunkStreamId = 2,
                MessageStreamId = 0,
                MessageLength = messageLength,
                MessageType = RtmpMessageType.UserControl
            };

            PacketBuffer packet = hdr.ToPacketBuffer();
            packet.ActualBufferSize += messageLength;

            using (EndianBinaryWriter writer = new EndianBinaryWriter(new PacketBufferStream(packet)))
            {
                writer.BaseStream.Seek(messageLength, System.IO.SeekOrigin.End);
                writer.Write((ushort)this.EventType);

                switch (this.EventType)
                {
                    case RtmpMessageUserControl.EventTypes.StreamBegin:
                    case RtmpMessageUserControl.EventTypes.StreamEOF:
                    case RtmpMessageUserControl.EventTypes.StreamDry:
                    case RtmpMessageUserControl.EventTypes.StreamIsRecorded:
                        {
                            writer.Write(this.TargetMessageStreamId);
                            break;
                        }

                    case RtmpMessageUserControl.EventTypes.SetBufferLength:
                        {
                            writer.Write(this.TargetMessageStreamId);
                            writer.Write(this.BufferLength);
                            break;
                        }

                    case RtmpMessageUserControl.EventTypes.PingRequest:
                    case RtmpMessageUserControl.EventTypes.PingResponse:
                        {
                            writer.Write(this.PingTimestamp);
                            break;
                        }
                }
            }

            return packet;
        }
    }
}
