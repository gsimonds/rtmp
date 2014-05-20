namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;

    /// <summary>
    /// RTMP message "Abort"
    /// </summary>
    public class RtmpMessageAbort : RtmpMessage
    {
        /// <summary>
        /// Creates new instance of RtmpMessageAbort
        /// </summary>
        /// <param name="targetChunkStreamId">Target chunk stream id which has to be aborted</param>
        public RtmpMessageAbort(int targetChunkStreamId)
        {
            this.TargetChunkStreamId = targetChunkStreamId;
            this.OrigMessageType = RtmpMessageType.Abort;
            this.MessageType = RtmpIntMessageType.ProtoControlAbort;
        }

        /// <summary>
        /// Gets or sets chunk stream id which has to be aborted
        /// </summary>
        public int TargetChunkStreamId { get; set; }

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
