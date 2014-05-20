namespace MComms_Transmuxer.Transport
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;

    /// <summary>
    /// General socket transport class. Implements both server and client
    /// </summary>
    public class SocketTransport
    {
        #region Constants

        /// <summary>
        /// Default maximum number of connections
        /// </summary>
        private const int DefaultMaxConnections = 1000;

        /// <summary>
        /// Default backlog size
        /// </summary>
        private const int DefaultBacklog = 100;

        /// <summary>
        /// Default accept's context pool size
        /// </summary>
        private const int DefaultAcceptContextPoolSize = 10;

        /// <summary>
        /// Default send's context pool size
        /// </summary>
        private const int DefaultSendContextPoolSize = SocketTransport.DefaultMaxConnections;

        /// <summary>
        /// Default receive buffer size
        /// </summary>
        private const int DefaultReceiveBufferSize = 8192;

        /// <summary>
        /// Default send buffer size
        /// </summary>
        private const int DefaultSendBufferSize = 8192;

        #endregion

        #region Private variables

        /// <summary>
        /// Server end point
        /// </summary>
        private IPEndPoint serverEndPoint = null;

        /// <summary>
        /// Protocol type
        /// </summary>
        private ProtocolType protocolType = ProtocolType.Unspecified;

        /// <summary>
        /// Whether transport is running
        /// </summary>
        private volatile bool isRunning = false;

        /// <summary>
        /// The maximum number of connections the object is able to handle simultaneously 
        /// </summary>
        private int maxConnections = SocketTransport.DefaultMaxConnections;

        /// <summary>
        /// Max number of pending connections the listener can hold in queue
        /// </summary>
        private int backlog = SocketTransport.DefaultBacklog;

        /// <summary>
        /// How many objects to put in pool for accept operations
        /// </summary>
        private int acceptContextPoolSize = SocketTransport.DefaultAcceptContextPoolSize;

        /// <summary>
        /// Maximum number of simultaneous receive operations. Normally it's equal to maxConnections
        /// </summary>
        private int receiveContextPoolSize = SocketTransport.DefaultMaxConnections;

        /// <summary>
        /// Maximum number of simultaneous send operations.
        /// </summary>
        private int sendContextPoolSize = SocketTransport.DefaultSendContextPoolSize;

        /// <summary>
        /// Buffer size to use for each socket receive operation
        /// </summary>
        private int receiveBufferSize = SocketTransport.DefaultReceiveBufferSize;

        /// <summary>
        /// Buffer size to use for each socket send operation
        /// </summary>
        private int sendBufferSize = SocketTransport.DefaultSendBufferSize;

        /// <summary>
        /// Listen socket
        /// </summary>
        private Socket listenSocket = null;

        /// <summary>
        /// Connected clients
        /// </summary>
        private Dictionary<IPEndPoint, ClientContext> clients = null;

        /// <summary>
        /// Receive buffer manager
        /// </summary>
        private SocketBufferManager receiveBufferManager = null;

        /// <summary>
        /// Send buffer manager
        /// </summary>
        private SocketBufferManager sendBufferManager = null;

        /// <summary>
        /// Accept SAEAs
        /// </summary>
        private List<SocketAsyncEventArgs> acceptAsyncContexts = null;

        /// <summary>
        /// Receive SAEAs
        /// </summary>
        private List<SocketAsyncEventArgs> receiveAsyncContexts = null;

        /// <summary>
        /// Send SAEAs
        /// </summary>
        private List<SocketAsyncEventArgs> sendAsyncContexts = null;

        /// <summary>
        /// Number of sent packets
        /// </summary>
        private int sentPackets = 0;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates new instance of SocketTransport
        /// </summary>
        public SocketTransport()
        {
            // we don't do any initialization here to allow user to customize settings
            // the real initialization will be done in Start() method
        }

        #endregion

        #region Events

        /// <summary>
        /// Fired when new client connects
        /// </summary>
        public event EventHandler<TransportArgs> Connected;

        /// <summary>
        /// Fired when connected client disconnects
        /// </summary>
        public event EventHandler<TransportArgs> Disconnected;

        /// <summary>
        /// Fired when new data received
        /// </summary>
        public event EventHandler<TransportArgs> Received;

        /// <summary>
        /// Fired when data is sent
        /// </summary>
        public event EventHandler<TransportArgs> Sent;

        #endregion

        #region Public properties and methods

        /// <summary>
        /// Gets or sets the maximum number of connections the object is able to handle simultaneously 
        /// </summary>
        public int MaxConnections
        {
            get
            {
                return this.maxConnections;
            }
            set
            {
                if (this.isRunning)
                {
                    throw new InvalidOperationException("Invalid while transport is running");
                }
                else
                {
                    this.maxConnections = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the max number of pending connections the listener can hold in queue
        /// </summary>
        public int Backlog
        {
            get
            {
                return this.backlog;
            }
            set
            {
                if (this.isRunning)
                {
                    throw new InvalidOperationException("Invalid while transport is running");
                }
                else
                {
                    this.backlog = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets how many objects to put in pool for accept operations
        /// </summary>
        public int AcceptContextPoolSize
        {
            get
            {
                return this.acceptContextPoolSize;
            }
            set
            {
                if (this.isRunning)
                {
                    throw new InvalidOperationException("Invalid while transport is running");
                }
                else
                {
                    this.acceptContextPoolSize = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets maximum number of simultaneous receive operations. Normally it's equal to maxConnections
        /// </summary>
        public int ReceiveContextPoolSize
        {
            get
            {
                return this.receiveContextPoolSize;
            }
            set
            {
                if (this.isRunning)
                {
                    throw new InvalidOperationException("Invalid while transport is running");
                }
                else
                {
                    this.receiveContextPoolSize = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets maximum number of simultaneous send operations.
        /// </summary>
        public int SendContextPoolSize
        {
            get
            {
                return this.sendContextPoolSize;
            }
            set
            {
                if (this.isRunning)
                {
                    throw new InvalidOperationException("Invalid while transport is running");
                }
                else
                {
                    this.sendContextPoolSize = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets buffer size to use for each socket receive operation
        /// </summary>
        public int ReceiveBufferSize
        {
            get
            {
                return this.backlog;
            }
            set
            {
                if (this.isRunning)
                {
                    throw new InvalidOperationException("Invalid while transport is running");
                }
                else
                {
                    this.receiveBufferSize = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets buffer size to use for each socket send operation
        /// </summary>
        public int SendBufferSize
        {
            get
            {
                return this.sendBufferSize;
            }
            set
            {
                if (this.isRunning)
                {
                    throw new InvalidOperationException("Invalid while transport is running");
                }
                else
                {
                    this.sendBufferSize = value;
                }
            }
        }

        /// <summary>
        /// Starts the transport
        /// </summary>
        /// <param name="serverEndPoint">Server end point, can be null for client mode</param>
        /// <param name="protocolType">Protocol type, can be null for client mode</param>
        public void Start(IPEndPoint serverEndPoint = null, ProtocolType protocolType = ProtocolType.Unspecified)
        {
            if (this.isRunning)
            {
                throw new InvalidOperationException("Transport is running already");
            }

            this.serverEndPoint = serverEndPoint;
            this.protocolType = protocolType;
            this.Initialize();
        }

        /// <summary>
        /// Stops the transport
        /// </summary>
        public void Stop()
        {
            this.Uninitialize();
        }

        /// <summary>
        /// Sends data to a specified end point. If specified end point is not found in
        /// the list of active connections then exception will be thrown.
        /// </summary>
        /// <param name="endPoint">End point to send data to</param>
        /// <param name="packet">Data to send</param>
        public void Send(IPEndPoint endPoint, PacketBuffer packet)
        {
            if (!this.isRunning)
            {
                throw new InvalidOperationException("Invalid while transport is not running");
            }

            ClientSendContext client = null;

            lock (clients)
            {
                if (!clients.ContainsKey(endPoint))
                {
                    throw new Exception("Connection not found");
                }

                client = new ClientSendContext(clients[endPoint]);
            }

            SocketAsyncEventArgs sendAsyncContext = null;

            lock (this.sendAsyncContexts)
            {
                if (this.sendAsyncContexts.Count > 0)
                {
                    sendAsyncContext = this.sendAsyncContexts[0];
                    this.sendAsyncContexts.RemoveAt(0);
                }
            }

            if (sendAsyncContext == null)
            {
                // we should not be here actually
                throw new Exception("No more send async context available");
            }

            client.Packet = packet;
            client.Packet.Position = 0;
            client.Packet.AddRef();

            sendAsyncContext.UserToken = client;
            this.StartSend(sendAsyncContext);
        }

        /// <summary>
        /// Connects to specified end point in client mode with specified protocol
        /// </summary>
        /// <param name="endPoint">End point to connect to</param>
        /// <param name="protocolType">Protocol to use</param>
        public void Connect(IPEndPoint endPoint, ProtocolType protocolType)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Disconnects specified end point
        /// </summary>
        /// <param name="endPoint">End point to disconnect</param>
        public void Disconnect(IPEndPoint endPoint)
        {
            if (!this.isRunning)
            {
                throw new InvalidOperationException("Invalid while transport is not running");
            }

            ClientSendContext client = null;

            lock (clients)
            {
                if (!clients.ContainsKey(endPoint))
                {
                    // already disconnected
                    return;
                }

                client = new ClientSendContext(clients[endPoint]);
            }

            try
            {
                client.Socket.Shutdown(SocketShutdown.Both);
            }
            catch
            {
            }

            try
            {
                client.Socket.Close();
            }
            catch
            {
            }
        }

        #endregion

        #region Virtual methods to be overridden in inherited class

        /// <summary>
        /// Calls Connected event handlers
        /// </summary>
        /// <param name="endPoint">Connected end point</param>
        protected virtual void OnConnect(IPEndPoint endPoint)
        {
            if (this.Connected != null)
            {
                this.Connected(this, new TransportArgs(endPoint));
            }
        }

        /// <summary>
        /// Calls Disconnected event handlers
        /// </summary>
        /// <param name="endPoint">Disconnected end point</param>
        protected virtual void OnDisconnect(IPEndPoint endPoint)
        {
            if (this.Disconnected != null)
            {
                this.Disconnected(this, new TransportArgs(endPoint));
            }
        }

        /// <summary>
        /// By default calls Received event handler.
        /// If client context specifies ReceiveEventHandler then it's called on priority basis.
        /// </summary>
        /// <param name="client">Client received data from</param>
        /// <param name="endPoint">Client's end point</param>
        /// <param name="data">Received data</param>
        /// <param name="dataOffset">Data offset</param>
        /// <param name="dataLength">Data length</param>
        protected virtual void OnReceive(ClientContext client, IPEndPoint endPoint, byte[] data, int dataOffset, int dataLength)
        {
            if (client != null && client.ReceiveEventHandler != null)
            {
                // direct call to RTMP session event handler
                client.ReceiveEventHandler(this, new TransportArgs(endPoint, data, dataOffset, dataLength));
            }
            else if (this.Received != null)
            {
                TransportArgs args = new TransportArgs(endPoint, data, dataOffset, dataLength);
                this.Received(this, args);
                if (args.ReceiveEventHandler != null && client != null)
                {
                    // we need to route further receive event to RTMP session directly
                    client.ReceiveEventHandler = args.ReceiveEventHandler;
                }
            }
        }

        /// <summary>
        /// Calls Sent event handlers
        /// </summary>
        /// <param name="endPoint">Client sent data to</param>
        /// <param name="packet">Packet sent</param>
        protected virtual void OnSent(IPEndPoint endPoint, PacketBuffer packet)
        {
            if (this.Sent != null)
            {
                this.Sent(this, new TransportArgs(endPoint, packet));
            }
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Initializes transport
        /// </summary>
        private void Initialize()
        {
            this.clients = new Dictionary<IPEndPoint, ClientContext>(this.maxConnections);

            if (this.serverEndPoint != null)
            {
                this.acceptAsyncContexts = new List<SocketAsyncEventArgs>(this.acceptContextPoolSize);
                for (int i = 0; i < this.acceptContextPoolSize; ++i)
                {
                    SocketAsyncEventArgs asyncContext = new SocketAsyncEventArgs();
                    asyncContext.Completed += Accept_Completed;
                    this.acceptAsyncContexts.Add(asyncContext);
                }
            }

            this.receiveBufferManager = new SocketBufferManager(this.receiveBufferSize * this.receiveContextPoolSize, this.receiveBufferSize);
            this.receiveAsyncContexts = new List<SocketAsyncEventArgs>(this.receiveContextPoolSize);
            for (int i = 0; i < this.receiveContextPoolSize; ++i)
            {
                SocketAsyncEventArgs asyncContext = new SocketAsyncEventArgs();
                asyncContext.Completed += Receive_Completed;
                this.receiveAsyncContexts.Add(asyncContext);
            }

            this.sendBufferManager = new SocketBufferManager(this.sendBufferSize * this.sendContextPoolSize, this.sendBufferSize);
            this.sendAsyncContexts = new List<SocketAsyncEventArgs>(this.sendContextPoolSize);
            for (int i = 0; i < this.sendContextPoolSize; ++i)
            {
                SocketAsyncEventArgs asyncContext = new SocketAsyncEventArgs();
                asyncContext.Completed += Send_Completed;
                this.sendAsyncContexts.Add(asyncContext);
            }

            if (this.serverEndPoint != null)
            {
                listenSocket = new Socket(this.serverEndPoint.AddressFamily, (protocolType == ProtocolType.Udp) ? SocketType.Dgram : SocketType.Stream, protocolType);
                listenSocket.Bind(this.serverEndPoint);
                listenSocket.Listen(this.backlog);
                this.StartAccept();
            }

            this.isRunning = true;
        }

        /// <summary>
        /// Uninitializes transport
        /// </summary>
        private void Uninitialize()
        {
            // this will prevent us from accepting of new connections
            this.isRunning = false;

            // close all sockets
            int clientCount = 0;

            lock (this.clients)
            {
                foreach (ClientContext client in this.clients.Values)
                {
                    try
                    {
                        client.Socket.Shutdown(SocketShutdown.Both);
                    }
                    catch
                    {
                    }

                    client.Socket.Close();
                }

                clientCount = this.clients.Count;
            }

            if (this.listenSocket != null)
            {
                this.listenSocket.Close();
            }

            // wait till all clients released
            while (clientCount > 0)
            {
                Thread.Sleep(100);
                lock (this.clients)
                {
                    clientCount = this.clients.Count;
                }
            }

            this.listenSocket = null;
            this.clients = null;

            this.receiveBufferManager = null;
            this.sendBufferManager = null;

            this.acceptAsyncContexts = null;
            this.receiveAsyncContexts = null;
            this.sendAsyncContexts = null;

            this.serverEndPoint = null;
            this.protocolType = ProtocolType.Unspecified;
        }

        /// <summary>
        /// Starts async accept
        /// </summary>
        private void StartAccept()
        {
            SocketAsyncEventArgs asyncContext = null;

            lock (this.acceptAsyncContexts)
            {
                if (this.acceptAsyncContexts.Count > 0)
                {
                    asyncContext = this.acceptAsyncContexts[0];
                    this.acceptAsyncContexts.RemoveAt(0);
                }
            }

            if (asyncContext == null)
            {
                return;
            }

            try
            {
                if (!listenSocket.AcceptAsync(asyncContext))
                {
                    this.ProcessAccept(asyncContext);
                }
            }
            catch
            {
                // push accept context back to pool
                if (asyncContext.AcceptSocket != null)
                {
                    try
                    {
                        asyncContext.AcceptSocket.Close();
                    }
                    catch
                    {
                    }

                    asyncContext.AcceptSocket = null;
                }

                lock (this.acceptAsyncContexts)
                {
                    this.acceptAsyncContexts.Add(asyncContext);
                }
            }
        }

        /// <summary>
        /// Processes async accept result
        /// </summary>
        /// <param name="asyncContext">Async context</param>
        private void ProcessAccept(SocketAsyncEventArgs asyncContext)
        {
            StartAccept(); // loopback accept

            if (!this.isRunning || asyncContext.SocketError != SocketError.Success)
            {
                // push accept context back to pool
                if (asyncContext.AcceptSocket != null)
                {
                    try
                    {
                        asyncContext.AcceptSocket.Close();
                    }
                    catch
                    {
                    }

                    asyncContext.AcceptSocket = null;
                }

                lock (this.acceptAsyncContexts)
                {
                    this.acceptAsyncContexts.Add(asyncContext);
                }

                return;
            }

            // create new client context
            ClientContext client = new ClientContext();
            client.Socket = asyncContext.AcceptSocket;
            client.RemoteEndPoint = client.Socket.RemoteEndPoint as IPEndPoint;

            // push accept context back to pool
            asyncContext.AcceptSocket = null;
            lock (this.acceptAsyncContexts)
            {
                this.acceptAsyncContexts.Add(asyncContext);
            }

            SocketAsyncEventArgs recvAsyncContext = null;

            lock (this.receiveAsyncContexts)
            {
                if (this.receiveAsyncContexts.Count > 0)
                {
                    recvAsyncContext = this.receiveAsyncContexts[0];
                    this.receiveAsyncContexts.RemoveAt(0);
                }
            }

            if (recvAsyncContext == null)
            {
                // we should not be here actually
                client.Socket.Close();
                return;
            }

            // store client
            lock (this.clients)
            {
                this.clients.Add(client.RemoteEndPoint, client);
            }

            // notify about new connection
            this.OnConnect(client.RemoteEndPoint);

            // start receive loop
            recvAsyncContext.UserToken = client;
            this.StartReceive(recvAsyncContext);
        }

        /// <summary>
        /// Starts async receive
        /// </summary>
        /// <param name="asyncContext">Async context</param>
        private void StartReceive(SocketAsyncEventArgs asyncContext)
        {
            ClientContext client = (ClientContext)asyncContext.UserToken;

            if (!this.receiveBufferManager.SetBuffer(asyncContext))
            {
                // no more buffer space
                // we should not be here actually
                Global.Log.Error("No more buffers available");
                this.Disconnect(asyncContext, true);
                return;
            }

            try
            {
                if (!client.Socket.ReceiveAsync(asyncContext))
                {
                    this.ProcessReceive(asyncContext);
                }
            }
            catch
            {
                this.Disconnect(asyncContext, true);
            }
        }

        /// <summary>
        /// Processes async receive result
        /// </summary>
        /// <param name="asyncContext">Async context</param>
        private void ProcessReceive(SocketAsyncEventArgs asyncContext)
        {
            if (!this.isRunning || asyncContext.SocketError != SocketError.Success || asyncContext.BytesTransferred == 0)
            {
                this.Disconnect(asyncContext, true);
                return;
            }

            try
            {
                ClientContext client = (ClientContext)asyncContext.UserToken;
                if (client.Socket.Connected)
                {
                    this.OnReceive(client, (IPEndPoint)client.RemoteEndPoint, asyncContext.Buffer, asyncContext.Offset, asyncContext.BytesTransferred);
                }
            }
            catch
            {
                this.Disconnect(asyncContext, true);
                return;
            }

            // back to receiving loop
            this.StartReceive(asyncContext);
        }

        /// <summary>
        /// Starts async send
        /// </summary>
        /// <param name="asyncContext">Async context</param>
        private void StartSend(SocketAsyncEventArgs asyncContext)
        {
            ClientSendContext client = (ClientSendContext)asyncContext.UserToken;
            int bytesToSend = Math.Min(this.sendBufferSize, client.Packet.ActualBufferSize - client.Packet.Position);

            if (!this.sendBufferManager.SetBuffer(asyncContext, bytesToSend))
            {
                // no more buffer space
                // we should not be here actually
                Global.Log.Error("No more buffers available");

                // release everything
                client.Packet.Release();
                asyncContext.UserToken = null;
                lock (this.sendAsyncContexts)
                {
                    this.sendAsyncContexts.Add(asyncContext);
                }

                return;
            }

            Array.Copy(client.Packet.Buffer, client.Packet.Position, asyncContext.Buffer, asyncContext.Offset, bytesToSend);

            try
            {
                if (!client.Socket.SendAsync(asyncContext))
                {
                    this.ProcessSend(asyncContext);
                }
            }
            catch
            {
                this.Disconnect(asyncContext, false);
            }
        }

        /// <summary>
        /// Processes async send result
        /// </summary>
        /// <param name="asyncContext"></param>
        private void ProcessSend(SocketAsyncEventArgs asyncContext)
        {
            ClientSendContext client = (ClientSendContext)asyncContext.UserToken;

            if (!this.isRunning || asyncContext.SocketError != SocketError.Success || asyncContext.BytesTransferred == 0)
            {
                this.Disconnect(asyncContext, false);
                return;
            }

            client.Packet.Position += asyncContext.BytesTransferred;

            this.sendBufferManager.FreeBuffer(asyncContext);

            if (client.Packet.Position < client.Packet.ActualBufferSize)
            {
                // there is remaining data to send
                this.StartSend(asyncContext);
            }
            else
            {
                // notify about completely sent packet
                this.OnSent((IPEndPoint)client.RemoteEndPoint, client.Packet);

                // release everything
                client.Packet.Release();
                asyncContext.UserToken = null;
                int sendAsyncContextsCount = 0;
                lock (this.sendAsyncContexts)
                {
                    this.sendAsyncContexts.Add(asyncContext);
                    sendAsyncContextsCount = this.sendAsyncContexts.Count;
                }

                //Global.Log.DebugFormat("Releasing context {0}, packet {1}, size {2}, sendAsyncContexts size {3}, sent {4}", client.RemoteEndPoint, client.Packet.Id, client.Packet.ActualBufferSize, sendAsyncContextsCount, ++sentPackets);
            }
        }

        /// <summary>
        /// Disconnects specified async context
        /// </summary>
        /// <param name="asyncContext">Async context to disconnect</param>
        /// <param name="receiveContext">True if it's a receive context, false if it's a send context</param>
        private void Disconnect(SocketAsyncEventArgs asyncContext, bool receiveContext)
        {
            ClientContext client = (ClientContext)asyncContext.UserToken;

            try
            {
                client.Socket.Shutdown(SocketShutdown.Both);
            }
            catch
            {
            }

            try
            {
                client.Socket.Close();
            }
            catch
            {
            }

            // remove client
            lock (this.clients)
            {
                this.clients.Remove(client.RemoteEndPoint);
            }

            if (receiveContext)
            {
                this.receiveBufferManager.FreeBuffer(asyncContext);

                lock (this.receiveAsyncContexts)
                {
                    this.receiveAsyncContexts.Add(asyncContext);
                }
            }
            else
            {
                ClientSendContext clientSend = (ClientSendContext)asyncContext.UserToken;
                if (clientSend.Packet != null)
                {
                    clientSend.Packet.Release();
                }

                this.sendBufferManager.FreeBuffer(asyncContext);

                lock (this.sendAsyncContexts)
                {
                    this.sendAsyncContexts.Add(asyncContext);
                }
            }

            // notify about disconnection
            this.OnDisconnect(client.RemoteEndPoint);
        }

        #endregion

        #region Event handlers

        /// <summary>
        /// Called when async accept completed
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Async context</param>
        void Accept_Completed(object sender, SocketAsyncEventArgs e)
        {
            this.ProcessAccept(e);
        }

        /// <summary>
        /// Called when async receive completed
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Async context</param>
        void Receive_Completed(object sender, SocketAsyncEventArgs e)
        {
            this.ProcessReceive(e);
        }

        /// <summary>
        /// Called when async send completed
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Async context</param>
        void Send_Completed(object sender, SocketAsyncEventArgs e)
        {
            this.ProcessSend(e);
        }

        #endregion
    }
}
