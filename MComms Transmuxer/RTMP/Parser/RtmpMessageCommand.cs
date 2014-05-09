namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;

    public class RtmpMessageCommand : RtmpMessage
    {
        public RtmpMessageCommand(string commandName, int transactionId, List<object> parameters)
        {
            this.OrigMessageType = RtmpMessageType.CommandAmf0;
            this.CommandName = commandName;
            this.TransactionId = transactionId;
            this.Parameters = parameters;

            switch (this.CommandName)
            {
                case "connect":
                    this.MessageType = RtmpIntMessageType.CommandNetConnectionConnect;
                    break;
                case "createStream":
                    this.MessageType = RtmpIntMessageType.CommandNetConnectionCreateStream;
                    break;
                case "releaseStream":
                    this.MessageType = RtmpIntMessageType.CommandNetConnectionReleaseStream;
                    break;
                case "FCPublish":
                    this.MessageType = RtmpIntMessageType.CommandNetConnectionFCPublish;
                    break;
                case "onFCPublish":
                    this.MessageType = RtmpIntMessageType.CommandNetConnectionOnFCPublish;
                    break;
                case "FCUnpublish":
                    this.MessageType = RtmpIntMessageType.CommandNetConnectionFCUnpublish;
                    break;
                case "onFCUnpublish":
                    this.MessageType = RtmpIntMessageType.CommandNetConnectionFCUnpublish;
                    break;
                case "closeStream":
                    this.MessageType = RtmpIntMessageType.CommandNetStreamCloseStream;
                    break;
                case "deleteStream":
                    this.MessageType = RtmpIntMessageType.CommandNetStreamDeleteStream;
                    break;
                case "publish":
                    this.MessageType = RtmpIntMessageType.CommandNetStreamPublish;
                    break;
                case "onStatus":
                    this.MessageType = RtmpIntMessageType.CommandNetStreamOnStatus;
                    break;
                case "_result":
                    this.MessageType = RtmpIntMessageType.CommandResult;
                    break;
                case "_error":
                    this.MessageType = RtmpIntMessageType.CommandError;
                    break;
                default:
                    this.MessageType = RtmpIntMessageType.CommandUnsupported;
                    break;
            }
        }

        public string CommandName { get; set; }

        public int TransactionId { get; set; }

        public List<object> Parameters { get; set; }

        public override PacketBuffer ToRtmpChunk()
        {
            // TODO: add chunk size check
            RtmpChunkHeader hdr = new RtmpChunkHeader
            {
                Format = 0,
                Timestamp = this.Timestamp,
                ChunkStreamId = this.ChunkStreamId,
                MessageStreamId = this.MessageStreamId,
                MessageType = RtmpMessageType.CommandAmf0
            };

            int hdrSize = hdr.HeaderSize;
            int totalSize = 0;

            PacketBuffer packet = this.createBody(hdrSize, ref totalSize);

            hdr.MessageLength = totalSize - hdrSize;
            hdr.ToPacketBuffer(packet);
            packet.ActualBufferSize = totalSize;

            return packet;
        }

        private PacketBuffer createBody(int hdrSize, ref int totalSize)
        {
            PacketBuffer packet = Global.Allocator.LockBuffer();
            // we'll set real value in the end
            packet.ActualBufferSize = packet.Size;

            using (EndianBinaryWriter writer = new EndianBinaryWriter(new PacketBufferStream(packet)))
            {
                // reserve space for chunk header
                writer.BaseStream.Seek(hdrSize, System.IO.SeekOrigin.Begin);

                // write AMF0 data
                writer.WriteAmf0(this.CommandName);
                writer.WriteAmf0(this.TransactionId);
                writer.WriteAmf0(this.Parameters);

                totalSize = (int)writer.BaseStream.Position;
            }

            return packet;
        }
    }
}
