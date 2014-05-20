namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;

    /// <summary>
    /// RTMP message "Aknowledgement"
    /// </summary>
    public class RtmpMessageAck : RtmpMessage
    {
        /// <summary>
        /// Creates new instance of RtmpMessageAck
        /// </summary>
        /// <param name="receivedBytes">Reported received bytes count</param>
        public RtmpMessageAck(uint receivedBytes)
        {
            this.ReceivedBytes = receivedBytes;
            this.OrigMessageType = RtmpMessageType.Aknowledgement;
            this.MessageType = RtmpIntMessageType.ProtoControlAknowledgement;
        }

        /// <summary>
        /// Gets or sets received bytes count as reported by peer
        /// </summary>
        public uint ReceivedBytes { get; set; }

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
                MessageType = RtmpMessageType.Aknowledgement
            };

            PacketBuffer packet = hdr.ToPacketBuffer();
            packet.ActualBufferSize += 4;

            using (EndianBinaryWriter writer = new EndianBinaryWriter(new PacketBufferStream(packet)))
            {
                writer.BaseStream.Seek(4, System.IO.SeekOrigin.End);
                writer.Write(this.ReceivedBytes);
            }

            return packet;
        }
    }
}
