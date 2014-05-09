namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using MComms_Transmuxer.Common;

    public class FlvFileHeader
    {
        public FlvFileHeader(bool haveAudio, bool haveVideo)
        {
            this.FileVersion = 1;
            this.HaveAudio = haveAudio;
            this.HaveVideo = haveVideo;
        }

        public byte FileVersion { get; set; }

        public bool HaveAudio { get; set; }

        public bool HaveVideo { get; set; }

        public int HeaderSize
        {
            get
            {
                return 9;
            }
        }

        public PacketBuffer ToPacketBuffer()
        {
            PacketBuffer packet = Global.Allocator.LockBuffer();
            packet.ActualBufferSize = this.HeaderSize;

            using (EndianBinaryWriter writer = new EndianBinaryWriter(new PacketBufferStream(packet)))
            {
                writer.Write((byte)'F');
                writer.Write((byte)'L');
                writer.Write((byte)'V');
                writer.Write(this.FileVersion);
                byte flags = (byte)((this.HaveAudio ? 0x04 : 0) | (this.HaveVideo ? 0x01 : 0));
                writer.Write(flags);
                writer.Write(this.HeaderSize);
            }

            return packet;
        }
    }
}
