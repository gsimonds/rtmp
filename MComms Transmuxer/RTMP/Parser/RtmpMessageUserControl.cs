namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;

    /// <summary>
    /// RTMP message "UserControl"
    /// </summary>
    public class RtmpMessageUserControl : RtmpMessage
    {
        /// <summary>
        /// Event types
        /// </summary>
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

        /// <summary>
        /// Creates new instance of RtmpMessageUserControl
        /// </summary>
        /// <param name="eventType">Specified event type</param>
        /// <param name="targetMessageStreamId">Specified target message stream id</param>
        public RtmpMessageUserControl(EventTypes eventType, int targetMessageStreamId)
        {
            this.EventType = eventType;
            this.TargetMessageStreamId = targetMessageStreamId;
            this.MessageType = RtmpIntMessageType.ProtoControlUserControl;
            this.OrigMessageType = RtmpMessageType.UserControl;
        }

        /// <summary>
        /// Creates new instance of RtmpMessageUserControl
        /// </summary>
        /// <param name="eventType">Specified event type</param>
        /// <param name="targetMessageStreamId">Specified target message stream id</param>
        /// <param name="bufferLength">Specified buffer length</param>
        public RtmpMessageUserControl(EventTypes eventType, int targetMessageStreamId, uint bufferLength)
        {
            this.EventType = eventType;
            this.TargetMessageStreamId = targetMessageStreamId;
            this.BufferLength = bufferLength;
            this.MessageType = RtmpIntMessageType.ProtoControlUserControl;
            this.OrigMessageType = RtmpMessageType.UserControl;
        }

        /// <summary>
        /// Creates new instance of RtmpMessageUserControl
        /// </summary>
        /// <param name="eventType">Specified event type</param>
        /// <param name="timestamp">Specified timestamp</param>
        public RtmpMessageUserControl(EventTypes eventType, uint timestamp)
        {
            this.EventType = eventType;
            this.PingTimestamp = timestamp;
            this.MessageType = RtmpIntMessageType.ProtoControlUserControl;
            this.OrigMessageType = RtmpMessageType.UserControl;
        }

        /// <summary>
        /// Gets or sets event type
        /// </summary>
        public EventTypes EventType { get; set; }

        /// <summary>
        /// Gets or sets target message stream id
        /// </summary>
        public int TargetMessageStreamId { get; set; }

        /// <summary>
        /// Gets or sets buffer length
        /// </summary>
        public uint BufferLength { get; set; }

        /// <summary>
        /// Gets or sets ping timestamp
        /// </summary>
        public uint PingTimestamp { get; set; }

        /// <summary>
        /// Converts current object to RTMP chunk and returns packet buffer containing it
        /// </summary>
        /// <returns>Packet buffer containing the converted RTMP chunk</returns>
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
