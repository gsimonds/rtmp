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

    /// <summary>
    /// RTMP server. Waiting for incoming connections, manages RTMP sessions.
    /// </summary>
    public class RtmpServer
    {
        #region Private constants and fields

        /// <summary>
        /// TCP transport
        /// </summary>
        private SocketTransport transport = null;

        /// <summary>
        /// Whether we've started
        /// </summary>
        private volatile bool isRunning = false;

        /// <summary>
        /// Control thread
        /// </summary>
        private Thread controlThread = null;

        /// <summary>
        /// RTMP sessions
        /// </summary>
        private Hashtable sessions = new Hashtable();

        /// <summary>
        /// Session counter
        /// </summary>
        private long sessionCounter = 0;

        /// <summary>
        /// Last time we checked publishing points
        /// </summary>
        private DateTime lastPublishingPointsChecked = DateTime.MinValue;

        /// <summary>
        /// Perf counters
        /// </summary>
        private Statistics stat = new Statistics();

        /// <summary>
        /// Last time we've collected the stat
        /// </summary>
        private DateTime lastStatCollected = DateTime.MinValue;

        /// <summary>
        /// Current number of connections
        /// </summary>
        private volatile int statNumberOfConnections = 0;

        /// <summary>
        /// Current total bandwidth
        /// </summary>
        private volatile int statTotalBandwidth = 0;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates new instance of RtmpServer
        /// </summary>
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

        #endregion

        #region Public methods

        /// <summary>
        /// Starts RTMP server
        /// </summary>
        public void Start()
        {
            this.isRunning = true;
            this.controlThread.Start();

            this.transport.Start(new IPEndPoint(IPAddress.Any, Properties.Settings.Default.RtmpPort), System.Net.Sockets.ProtocolType.Tcp);
        }

        /// <summary>
        /// Stops RTMP server
        /// </summary>
        public void Stop()
        {
            this.transport.Stop();

            this.isRunning = false;
            this.controlThread.Join();

            // clean up publishing points
            SmoothStreamingPublisher.DeleteAll();
        }

        #endregion

        #region Private methods and event handlers

        /// <summary>
        /// Main control thread
        /// </summary>
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

        /// <summary>
        /// Called when new connection established
        /// </summary>
        /// <param name="sender">Transport object</param>
        /// <param name="e">Connection parameters</param>
        private void Transport_Connected(object sender, TransportArgs e)
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

        /// <summary>
        /// Called when connection finished
        /// </summary>
        /// <param name="sender">Transport object</param>
        /// <param name="e">Connection parameters</param>
        private void Transport_Disconnected(object sender, TransportArgs e)
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

        /// <summary>
        /// Called when we received new data
        /// </summary>
        /// <param name="sender">Transport object</param>
        /// <param name="e">Connection parameters including received data</param>
        private void Transport_Received(object sender, TransportArgs e)
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

        /// <summary>
        /// Called when data has been sent
        /// </summary>
        /// <param name="sender">Transport object</param>
        /// <param name="e">Connection parameters including sent data</param>
        private void Transport_Sent(object sender, TransportArgs e)
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

        #endregion
    }
}
