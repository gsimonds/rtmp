namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using MComms_Transmuxer.Common;

    /// <summary>
    /// FLV tag (i.e. packet, message) header
    /// </summary>
    public class FlvTagHeader
    {
        /// <summary>
        /// Creates new instance of FLV tag header
        /// </summary>
        public FlvTagHeader()
        {
            this.TagType = RtmpMessageType.Undefined;
            this.DataSize = 0;
            this.Timestamp = 0;
            this.StreamId = 0;
        }

        /// <summary>
        /// Gets or set tag type (i.e. RTMP message type).
        /// For FLV must be Audio and Video only
        /// </summary>
        public RtmpMessageType TagType { get; set; }

        /// <summary>
        /// Gets or sets media data size. Media data is following the tag header
        /// </summary>
        public uint DataSize { get; set; }

        /// <summary>
        /// Gets or sets current timestamp
        /// </summary>
        public uint Timestamp { get; set; }

        /// <summary>
        /// Gets or sets stream id. Must be 0 for FLV.
        /// </summary>
        public int StreamId { get; set; }

        /// <summary>
        /// Gets tag header size
        /// </summary>
        public int HeaderSize
        {
            get
            {
                return 11;
            }
        }

        /// <summary>
        /// Converts current object to a byte array and returns packet buffer containing it
        /// </summary>
        /// <returns>Packet buffer containing the converted byte array</returns>
        public PacketBuffer ToPacketBuffer()
        {
            PacketBuffer packet = Global.Allocator.LockBuffer();
            return ToPacketBuffer(packet);
        }

        /// <summary>
        /// Converts current object to a byte array and returns packet buffer containing it
        /// </summary>
        /// <param name="packet">Packet buffer to write byte data to</param>
        /// <returns>Packet buffer (the same as specified in parameter packet) containing the converted byte array</returns>
        public PacketBuffer ToPacketBuffer(PacketBuffer packet)
        {
            packet.ActualBufferSize = this.HeaderSize;

            using (EndianBinaryWriter writer = new EndianBinaryWriter(new PacketBufferStream(packet)))
            {
                writer.Write((byte)this.TagType);
                writer.Write(this.DataSize, 3);
                writer.Write((uint)(this.Timestamp & 0x00FFFFFF), 3);
                writer.Write((byte)((this.Timestamp & 0xFF000000) >> 24));
                writer.Write(this.StreamId, 3);
            }

            return packet;
        }
    }
}
