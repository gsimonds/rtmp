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

    class RtmpServer
    {
        private SocketTransport transport = null;
        private volatile bool isRunning = false;
        private Thread controlThread = null;
        private Dictionary<IPEndPoint, RtmpSession> sessions = new Dictionary<IPEndPoint, RtmpSession>();

        public RtmpServer()
        {
            // initialize default endianness
            EndianBinaryReader.GlobalEndiannes = Endianness.BigEndian;
            EndianBinaryWriter.GlobalEndiannes = Endianness.BigEndian;

            this.transport = new SocketTransport();
            this.transport.Connected += Transport_Connected;
            this.transport.Disconnected += Transport_Disconnected;
            this.transport.Received += Transport_Received;
#if DEBUG
            // use sent event in debug only, we don't need it in production version
            this.transport.Sent += Transport_Sent;
#endif

            // TODO: adjust transport parameters

            this.controlThread = new Thread(this.ControlThreadProc);
        }

        public void Start()
        {
            this.isRunning = true;
            this.controlThread.Start();

            this.transport.Start(new IPEndPoint(IPAddress.Any, 1935), System.Net.Sockets.ProtocolType.Tcp);
        }

        public void Stop()
        {
            this.transport.Stop();

            this.isRunning = false;
            this.controlThread.Join();
        }

        private void ControlThreadProc()
        {
            while (this.isRunning)
            {
                // TODO: implement general server control logic
            }
        }

        void Transport_Connected(object sender, TransportArgs e)
        {
            lock (this)
            {
                if (sessions.ContainsKey(e.EndPoint))
                {
                    // session exists already, skipping it
                    return;
                }

                RtmpSession session = new RtmpSession(this.transport, e.EndPoint);
                sessions.Add(e.EndPoint, session);
            }
        }

        void Transport_Disconnected(object sender, TransportArgs e)
        {
            lock (this)
            {
                if (!sessions.ContainsKey(e.EndPoint))
                {
                    // session doens't exist, skipping it
                    return;
                }

                sessions[e.EndPoint].Dispose();
                sessions.Remove(e.EndPoint);
            }
        }

        void Transport_Received(object sender, TransportArgs e)
        {
            lock (this)
            {
                if (!sessions.ContainsKey(e.EndPoint))
                {
                    // out of bound message, skipping it
                    return;
                }

                sessions[e.EndPoint].OnReceive(e.Packet);
            }
        }

        void Transport_Sent(object sender, TransportArgs e)
        {
            lock (this)
            {
                if (!sessions.ContainsKey(e.EndPoint))
                {
                    // out of bound message, skipping it
                    return;
                }

                // TODO: log
            }
        }

        // other public properties/methods TBD
    }
}
