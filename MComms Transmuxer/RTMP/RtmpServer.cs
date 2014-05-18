namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;
    using MComms_Transmuxer.SmoothStreaming;
    using MComms_Transmuxer.Transport;

    public class RtmpServer
    {
        private SocketTransport transport = null;
        private volatile bool isRunning = false;
        private Thread controlThread = null;
        private Hashtable sessions = new Hashtable();
        private long sessionCounter = 0;

        private Statistics stat = new Statistics();
        private DateTime lastStatCollected = DateTime.MinValue;
        private DateTime lastPublishingPointsChecked = DateTime.MinValue;
        private volatile int statNumberOfConnections = 0;
        private volatile int statTotalBandwidth = 0;

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

            this.transport.MaxConnections = Global.RtmpMaxConnections;
            this.transport.ReceiveContextPoolSize = Global.RtmpMaxConnections;
            this.transport.SendContextPoolSize = Global.RtmpMaxConnections;
            this.transport.ReceiveBufferSize = Global.TransportBufferSize;
            this.transport.SendBufferSize = Global.TransportBufferSize;

            this.stat.InitStats();

            this.controlThread = new Thread(this.ControlThreadProc);
        }

        public void Start()
        {
            this.isRunning = true;
            this.controlThread.Start();

            this.transport.Start(new IPEndPoint(IPAddress.Any, Properties.Settings.Default.RtmpPort), System.Net.Sockets.ProtocolType.Tcp);
        }

        public void Stop()
        {
            this.transport.Stop();

            this.isRunning = false;
            this.controlThread.Join();

            // clean up publishing points
            SmoothStreamingPublisher.DeleteAll();
        }

        private void ControlThreadProc()
        {
            Global.Log.Debug("RtmpServer main thread started");

            while (this.isRunning)
            {
                if ((DateTime.Now - this.lastStatCollected).TotalMilliseconds >= 1000)
                {
                    this.stat.CollectNetworkInfo(this.statNumberOfConnections, this.statTotalBandwidth * 8);
                    this.statTotalBandwidth = 0;
                    this.lastStatCollected = DateTime.Now;
                }

                if ((DateTime.Now - this.lastPublishingPointsChecked).TotalMilliseconds >= 1000)
                {
                    SmoothStreamingPublisher.DeleteExpired();
                    this.lastPublishingPointsChecked = DateTime.Now;
                }

                Thread.Sleep(1);
            }
        }

        void Transport_Connected(object sender, TransportArgs e)
        {
            lock (this)
            {
                if (sessions.ContainsKey(e.EndPoint))
                {
                    // session exists already, skipping it
                    Global.Log.DebugFormat("Session {0} exists already", e.EndPoint);
                    return;
                }

                RtmpSession session = new RtmpSession(++this.sessionCounter, this.transport, e.EndPoint);
                sessions.Add(e.EndPoint, session);
                this.statNumberOfConnections = sessions.Count;
            }
        }

        void Transport_Disconnected(object sender, TransportArgs e)
        {
            lock (this)
            {
                if (!sessions.ContainsKey(e.EndPoint))
                {
                    // session doens't exist, skipping it
                    Global.Log.DebugFormat("Session {0} doesn't exist", e.EndPoint);
                    return;
                }

                ((RtmpSession)sessions[e.EndPoint]).Dispose();
                sessions.Remove(e.EndPoint);
                this.statNumberOfConnections = sessions.Count;
            }
        }

        void Transport_Received(object sender, TransportArgs e)
        {
            lock (this)
            {
                if (!sessions.ContainsKey(e.EndPoint))
                {
                    // out of bound message, skipping it
                    Global.Log.DebugFormat("Out of bound message received from {0}, size {1}", e.EndPoint, e.Packet.ActualBufferSize);
                    return;
                }

                this.statTotalBandwidth += e.DataLength;
                ((RtmpSession)sessions[e.EndPoint]).OnReceive(sender, e);
            }
        }

        void Transport_Sent(object sender, TransportArgs e)
        {
            lock (this)
            {
                if (!sessions.ContainsKey(e.EndPoint))
                {
                    // out of bound message, skipping it
                    Global.Log.DebugFormat("Out of bound message sent to {0}, size {1}", e.EndPoint, e.Packet.ActualBufferSize);
                    return;
                }
            }
        }
    }
}
