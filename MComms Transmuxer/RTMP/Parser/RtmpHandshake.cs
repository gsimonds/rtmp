namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;

    public class RtmpHandshake : RtmpMessage
    {
        public RtmpHandshake()
        {
        }

        public byte Version { get; set; }

        public uint Time { get; set; }

        public uint Time2 { get; set; }

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
                handshake.MessageType = RtmpIntMessageType.HandshakeC0;
                handshake.Version = (byte)dataStream.ReadByte();

                // drop processed data from the stream
                dataStream.TrimBegin();

                return handshake;
            }
        }

        public static RtmpHandshake DecodeC1(PacketBufferStream dataStream)
        {
            if (dataStream.Length < Global.RtmpHandshakeSize)
            {
                // not enough data
                return null;
            }
            else
            {
                RtmpHandshake handshake = new RtmpHandshake();
                handshake.MessageType = RtmpIntMessageType.HandshakeC1;

                using (EndianBinaryReader reader = new EndianBinaryReader(dataStream, true))
                {
                    handshake.Time = reader.ReadUInt32();
                    handshake.Time2 = reader.ReadUInt32();
                }

                handshake.RandomBytes = new byte[Global.RtmpHandshakeRandomBytesSize];
                dataStream.Read(handshake.RandomBytes, 0, Global.RtmpHandshakeRandomBytesSize);

                // drop processed data from the stream
                dataStream.TrimBegin();

                return handshake;
            }
        }

        public static RtmpHandshake DecodeC2(PacketBufferStream dataStream)
        {
            if (dataStream.Length < Global.RtmpHandshakeSize)
            {
                // not enough data
                return null;
            }
            else
            {
                RtmpHandshake handshake = new RtmpHandshake();
                handshake.MessageType = RtmpIntMessageType.HandshakeC2;

                using (EndianBinaryReader reader = new EndianBinaryReader(dataStream, true))
                {
                    handshake.Time = reader.ReadUInt32();
                    handshake.Time2 = reader.ReadUInt32();
                }

                handshake.RandomBytes = new byte[Global.RtmpHandshakeRandomBytesSize];
                dataStream.Read(handshake.RandomBytes, 0, Global.RtmpHandshakeRandomBytesSize);

                // drop processed data from the stream
                dataStream.TrimBegin();

                return handshake;
            }
        }

        public static RtmpHandshake GenerateS0()
        {
            RtmpHandshake handshake = new RtmpHandshake();
            handshake.MessageType = RtmpIntMessageType.HandshakeS0;
            handshake.Version = Global.RtmpVersion;
            return handshake;
        }

        public static RtmpHandshake GenerateS1()
        {
            RtmpHandshake handshake = new RtmpHandshake();
            handshake.MessageType = RtmpIntMessageType.HandshakeS1;
            handshake.Time = 0;
            handshake.Time2 = 0;

            Random rnd = new Random();
            handshake.RandomBytes = new byte[Global.RtmpHandshakeRandomBytesSize];
            rnd.NextBytes(handshake.RandomBytes);

            return handshake;
        }

        public RtmpHandshake GenerateS2()
        {
            RtmpHandshake handshake = new RtmpHandshake();
            handshake.MessageType = RtmpIntMessageType.HandshakeS2;
            handshake.Time = this.Time;
            handshake.Time2 = 0;
            handshake.RandomBytes = this.RandomBytes;
            return handshake;
        }

        public bool ValidateC2(RtmpHandshake handshakeS1)
        {
            if (this.Time != handshakeS1.Time)
            {
                return false;
            }

            for (int i = 0; i < Global.RtmpHandshakeRandomBytesSize; i++)
            {
                if (this.RandomBytes[i] != handshakeS1.RandomBytes[i])
                {
                    return false;
                }
            }

            return true;
        }

        public override PacketBuffer ToPacketBuffer()
        {
            PacketBuffer packet = null;

            switch (this.MessageType)
            {
                case RtmpIntMessageType.HandshakeS0:
                    {
                        packet = Global.Allocator.LockBuffer();
                        packet.Buffer[0] = this.Version;
                        packet.ActualBufferSize = 1;
                        break;
                    }

                case RtmpIntMessageType.HandshakeS1:
                    {
                        packet = Global.Allocator.LockBuffer();
                        for (int i = 0; i < 8; i++)
                        {
                            packet.Buffer[i] = 0;
                        }
                        Array.Copy(this.RandomBytes, 0, packet.Buffer, 8, this.RandomBytes.Length);
                        packet.ActualBufferSize = Global.RtmpHandshakeSize;
                        break;
                    }

                case RtmpIntMessageType.HandshakeS2:
                    {
                        packet = Global.Allocator.LockBuffer();
                        packet.ActualBufferSize = Global.RtmpHandshakeSize;
                        using (EndianBinaryWriter writer = new EndianBinaryWriter(new PacketBufferStream(packet)))
                        {
                            writer.Write(this.Time);
                            writer.Write(this.Time2);
                        }
                        Array.Copy(this.RandomBytes, 0, packet.Buffer, 8, this.RandomBytes.Length);
                        break;
                    }
            }

            return packet;
        }
    }
}
