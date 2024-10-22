﻿namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;

    /// <summary>
    /// RTMP message "WindowAknowledgementSize"
    /// </summary>
    public class RtmpMessageWindowAckSize : RtmpMessage
    {
        /// <summary>
        /// Creates new instance of RtmpMessageWindowAckSize
        /// </summary>
        /// <param name="ackSize">Aknowledgement size</param>
        public RtmpMessageWindowAckSize(uint ackSize)
        {
            this.AckSize = ackSize;
            this.MessageType = RtmpIntMessageType.ProtoControlWindowAknowledgementSize;
            this.OrigMessageType = RtmpMessageType.WindowAknowledgementSize;
        }

        /// <summary>
        /// Gets or sets aknowledgement size
        /// </summary>
        public uint AckSize { get; set; }

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
