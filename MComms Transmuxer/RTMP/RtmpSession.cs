namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;
    using MComms_Transmuxer.Transport;

    class RtmpSession : IDisposable
    {
        private SocketTransport transport = null;
        private IPEndPoint sessionEndPoint = null;
        private volatile bool isRunning = true;
        private RtmpSessionState state = RtmpSessionState.Uninitialized;
        private RtmpProtocolParser parser = new RtmpProtocolParser();
        private Thread sessionThread = null;
        private Queue<PacketBuffer> receivedPackets = new Queue<PacketBuffer>();

        public RtmpSession(SocketTransport transport, IPEndPoint sessionEndPoint)
        {
            this.transport = transport;
            this.sessionEndPoint = sessionEndPoint;
            this.sessionThread = new Thread(this.SessionThread);
            this.sessionThread.Start();
        }

        public void Dispose()
        {
            this.isRunning = false;
            this.sessionThread.Join();
        }

        public void OnReceive(PacketBuffer packet)
        {
            lock (this.receivedPackets)
            {
                packet.AddRef();
                this.receivedPackets.Enqueue(packet);
            }
        }

        private void SessionThread()
        {
            while (this.isRunning)
            {
                PacketBuffer packet = null;

                lock (this.receivedPackets)
                {
                    if (this.receivedPackets.Count > 0)
                    {
                        packet = this.receivedPackets.Dequeue();
                    }
                }

                if (packet != null)
                {
                    RtmpMessage msg = null;
                    while ((msg = parser.Decode(packet)) != null)
                    {
                        this.ProcessMessage(msg);
                        packet = null;
                    }
                }
                else
                {
                    // sleep only if don't have anything to do
                    Thread.Sleep(1);
                }
            }
        }

        private void ProcessMessage(RtmpMessage msg)
        {
            switch (msg.MessageType)
            {
                case RtmpMessageType.HandshakeC0:
                    {
                        RtmpHandshake hadshake = (RtmpHandshake)msg;
                        if (hadshake.Version == 3)
                        {
                            // TODO: send reply
                            this.state = RtmpSessionState.HanshakeVersionSent;
                            this.parser.State = RtmpSessionState.HanshakeVersionSent;
                        }
                        else
                        {
                            // TODO: handle it
                        }

                        break;
                    }

                case RtmpMessageType.HandshakeC1:
                    {
                        break;
                    }

                case RtmpMessageType.HandshakeC2:
                    {
                        break;
                    }

                // TODO: process other messages
            }
        }

        // other public properties/methods TBD
    }
}
