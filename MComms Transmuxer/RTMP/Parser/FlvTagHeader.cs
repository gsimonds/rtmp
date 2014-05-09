namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using MComms_Transmuxer.Common;

    public class FlvTagHeader
    {
        public FlvTagHeader()
        {
            this.TagType = RtmpMessageType.Undefined;
            this.DataSize = 0;
            this.Timestamp = 0;
            this.StreamId = 0;
        }

        public RtmpMessageType TagType { get; set; }

        public uint DataSize { get; set; }

        public uint Timestamp { get; set; }

        public int StreamId { get; set; }

        public int HeaderSize
        {
            get
            {
                return 11;
            }
        }

        public PacketBuffer ToPacketBuffer()
        {
            PacketBuffer packet = Global.Allocator.LockBuffer();
            return ToPacketBuffer(packet);
        }

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
