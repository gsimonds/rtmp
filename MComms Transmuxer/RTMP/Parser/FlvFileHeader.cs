namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using MComms_Transmuxer.Common;

    /// <summary>
    /// FLV file header
    /// </summary>
    public class FlvFileHeader
    {
        /// <summary>
        /// Creates new instance of FLV file header with specified stream flags
        /// </summary>
        /// <param name="haveAudio">Whether file contains audio</param>
        /// <param name="haveVideo">Whether file contains video</param>
        public FlvFileHeader(bool haveAudio, bool haveVideo)
        {
            this.FileVersion = 1;
            this.HaveAudio = haveAudio;
            this.HaveVideo = haveVideo;
        }

        /// <summary>
        /// Gets or sets FLV file version. Has to be 1.
        /// </summary>
        public byte FileVersion { get; set; }

        /// <summary>
        /// Gets or sets whether the file contains audio
        /// </summary>
        public bool HaveAudio { get; set; }

        /// <summary>
        /// Gets or sets whether the file contains video
        /// </summary>
        public bool HaveVideo { get; set; }

        /// <summary>
        /// Gets header size
        /// </summary>
        public int HeaderSize
        {
            get
            {
                return 9;
            }
        }

        /// <summary>
        /// Converts current object to a byte array and returns packet buffer containing it
        /// </summary>
        /// <returns>Packet buffer containing the converted byte array</returns>
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
