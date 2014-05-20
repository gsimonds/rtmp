namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;

    /// <summary>
    /// RTMP handshake. Used to parse, generate and validate RTMP handshake messages
    /// </summary>
    public class RtmpHandshake : RtmpMessage
    {
        #region Constructor

        /// <summary>
        /// Creates new instance of RtmpHandshake
        /// </summary>
        public RtmpHandshake()
        {
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets or sets RTMP version
        /// </summary>
        public byte Version { get; set; }

        /// <summary>
        /// Gets or sets time
        /// </summary>
        public uint Time { get; set; }

        /// <summary>
        /// Gets or sets time2
        /// </summary>
        public uint Time2 { get; set; }

        /// <summary>
        /// Gets or sets random bytes
        /// </summary>
        public byte[] RandomBytes { get; set; }

        #endregion

        #region Public static methods

        /// <summary>
        /// Decodes C0 message from specified stream
        /// </summary>
        /// <param name="dataStream">Stream to read data from</param>
        /// <returns>Decoded C0 message</returns>
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

        /// <summary>
        /// Decodes C1 message from specified stream
        /// </summary>
        /// <param name="dataStream">Stream to read data from</param>
        /// <returns>Decoded C1 message</returns>
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

        /// <summary>
        /// Decodes C2 message from specified stream
        /// </summary>
        /// <param name="dataStream">Stream to read data from</param>
        /// <returns>Decoded C2 message</returns>
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

        /// <summary>
        /// Generates S0 message
        /// </summary>
        /// <returns>Generated S0 message</returns>
        public static RtmpHandshake GenerateS0()
        {
            RtmpHandshake handshake = new RtmpHandshake();
            handshake.MessageType = RtmpIntMessageType.HandshakeS0;
            handshake.Version = Global.RtmpVersion;
            return handshake;
        }

        /// <summary>
        /// Generates S1 message
        /// </summary>
        /// <returns>Generated S1 message</returns>
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

        #endregion

        #region Public methods

        /// <summary>
        /// Validates current C2 message based on previously sent S1 message
        /// </summary>
        /// <param name="handshakeS1">S1 message to use for validation</param>
        /// <returns>True if C2 message is valid, false otherwise</returns>
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

        /// <summary>
        /// Generates S2 message from current C2 message
        /// </summary>
        /// <returns>Generated S2 message</returns>
        public RtmpHandshake GenerateS2()
        {
            RtmpHandshake handshake = new RtmpHandshake();
            handshake.MessageType = RtmpIntMessageType.HandshakeS2;
            handshake.Time = this.Time;
            handshake.Time2 = 0;
            handshake.RandomBytes = this.RandomBytes;
            return handshake;
        }

        /// <summary>
        /// Converts current object to RTMP chunk and returns packet buffer containing it
        /// </summary>
        /// <returns>Packet buffer containing the converted RTMP chunk</returns>
        public override PacketBuffer ToRtmpChunk()
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

        #endregion
    }
}
