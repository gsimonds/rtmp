namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;

    class RtmpHandshake : RtmpMessage
    {
        public RtmpHandshake()
        {
        }

        public byte Version { get; set; }

        public int Time { get; set; }

        public int Time2 { get; set; }

        public byte[] RandomBytes { get; set; }

        public static RtmpHandshake DecodeC0(PacketBufferStream dataStream)
        {
            if (dataStream.Length == 0)
            {
                // not enough data
                return null;
            }
            else
            {
                RtmpHandshake handshake = new RtmpHandshake();
                handshake.MessageType = RtmpMessageType.HandshakeC0;
                handshake.Version = (byte)dataStream.ReadByte();

                // drop processed data from the stream
                dataStream.TrimBegin();

                return handshake;
            }
        }

        public static RtmpHandshake DecodeC1(PacketBufferStream dataStream)
        {
            if (dataStream.Length < 1536)
            {
                // not enough data
                return null;
            }
            else
            {
                RtmpHandshake handshake = new RtmpHandshake();
                handshake.MessageType = RtmpMessageType.HandshakeC1;
                handshake.Time = dataStream.ReadInt32();
                handshake.Time2 = dataStream.ReadInt32();
                handshake.RandomBytes = new byte[1528];
                dataStream.Read(handshake.RandomBytes, 0, 1528);

                // drop processed data from the stream
                dataStream.TrimBegin();

                return handshake;
            }
        }

        public static RtmpHandshake DecodeC2(PacketBufferStream dataStream)
        {
            if (dataStream.Length < 1536)
            {
                // not enough data
                return null;
            }
            else
            {
                RtmpHandshake handshake = new RtmpHandshake();
                handshake.MessageType = RtmpMessageType.HandshakeC2;
                handshake.Time = dataStream.ReadInt32();
                handshake.Time2 = dataStream.ReadInt32();
                handshake.RandomBytes = new byte[1528];
                dataStream.Read(handshake.RandomBytes, 0, 1528);

                // drop processed data from the stream
                dataStream.TrimBegin();

                return handshake;
            }
        }
    }
}
