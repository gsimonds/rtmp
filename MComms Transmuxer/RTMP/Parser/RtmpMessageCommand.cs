namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;

    /// <summary>
    /// RTMP message "AMF0 encoded command". Used to store & generate various command messages.
    /// </summary>
    public class RtmpMessageCommand : RtmpMessage
    {
        /// <summary>
        /// Creates new instance of RtmpMessageCommand
        /// </summary>
        /// <param name="commandName">Command name</param>
        /// <param name="transactionId">Transaction Id</param>
        /// <param name="parameters">Other parameters</param>
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

        /// <summary>
        /// Gets or sets command name
        /// </summary>
        public string CommandName { get; set; }

        /// <summary>
        /// Gets or sets transaction id
        /// </summary>
        public int TransactionId { get; set; }

        /// <summary>
        /// Gets or sets command parameters
        /// </summary>
        public List<object> Parameters { get; set; }

        /// <summary>
        /// Converts current object to RTMP chunk and returns packet buffer containing it
        /// </summary>
        /// <returns>Packet buffer containing the converted RTMP chunk</returns>
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

        /// <summary>
        /// Creates message body
        /// </summary>
        /// <param name="hdrSize">Header size</param>
        /// <param name="totalSize">Total message size</param>
        /// <returns>Packet buffer containing the message body and reserved space for a header</returns>
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
