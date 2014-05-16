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

        private const int DefaultMaxConnections = 1000;
        private const int DefaultBacklog = 100;
        private const int DefaultAcceptContextPoolSize = 10;
        private const int DefaultSendContextPoolSize = SocketTransport.DefaultMaxConnections;
        private const int DefaultReceiveBufferSize = 10240;
        private const int DefaultSendBufferSize = 10240;

        #endregion

        #region Private variables

        private IPEndPoint serverEndPoint = null;
        private ProtocolType protocolType = ProtocolType.Unspecified;

        private volatile bool isRunning = false;

        /// <summary>
        /// The maximum number of connections the sample is designed to handle simultaneously 
        /// </summary>
        private int maxConnections = SocketTransport.DefaultMaxConnections;

        /// <summary>
        /// Max # of pending connections the listener can hold in queue
        /// </summary>
        private int backlog = SocketTransport.DefaultBacklog;

        /// <summary>
        /// Tells us how many objects to put in pool for accept operations
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

        private Socket listenSocket = null;
        private Dictionary<IPEndPoint, ClientContext> clients = null;

        private SocketBufferManager receiveBufferManager = null;
        private SocketBufferManager sendBufferManager = null;

        private List<SocketAsyncEventArgs> acceptAsyncContexts = null;
        private List<SocketAsyncEventArgs> receiveAsyncContexts = null;
        private List<SocketAsyncEventArgs> sendAsyncContexts = null;

        private Semaphore maxConnectionsEnforcer = null;

        #endregion

        #region Constructor

        public SocketTransport()
        {
            // we don't do any initialization here to allow user to customize settings
            // the real initialization will be done in Start() method
        }

        #endregion

        #region Events

        public event EventHandler<TransportArgs> Connected;
        public event EventHandler<TransportArgs> Disconnected;
        public event EventHandler<TransportArgs> Received;
        public event EventHandler<TransportArgs> Sent;

        #endregion

        #region Public properties and methods

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

        public void Stop()
        {
            this.Uninitialize();
        }

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

            sendAsyncContext.AcceptSocket = client.Socket;
            sendAsyncContext.UserToken = client;
            this.StartSend(sendAsyncContext);
        }

        public void Connect(IPEndPoint endPoint, ProtocolType protocolType)
        {
            throw new NotImplementedException();
        }

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

        protected virtual void OnConnect(IPEndPoint endPoint)
        {
            if (this.Connected != null)
            {
                this.Connected(this, new TransportArgs(endPoint));
            }
        }

        protected virtual void OnDisconnect(IPEndPoint endPoint)
        {
            if (this.Disconnected != null)
            {
                this.Disconnected(this, new TransportArgs(endPoint));
            }
        }

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

        protected virtual void OnSent(IPEndPoint endPoint, PacketBuffer packet)
        {
            if (this.Sent != null)
            {
                this.Sent(this, new TransportArgs(endPoint, packet));
            }
        }

        #endregion

        #region Private methods

        private void Initialize()
        {
            this.maxConnectionsEnforcer = new Semaphore(this.maxConnections, this.maxConnections);
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
            this.maxConnectionsEnforcer = null;

            this.receiveBufferManager = null;
            this.sendBufferManager = null;

            this.acceptAsyncContexts = null;
            this.receiveAsyncContexts = null;
            this.sendAsyncContexts = null;

            this.serverEndPoint = null;
            this.protocolType = ProtocolType.Unspecified;
        }

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

            // limit number of connections
            this.maxConnectionsEnforcer.WaitOne();

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
            recvAsyncContext.AcceptSocket = client.Socket;
            recvAsyncContext.UserToken = client;
            this.StartReceive(recvAsyncContext);
        }

        private void StartReceive(SocketAsyncEventArgs asyncContext)
        {
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
                if (!asyncContext.AcceptSocket.ReceiveAsync(asyncContext))
                {
                    this.ProcessReceive(asyncContext);
                }
            }
            catch
            {
                this.Disconnect(asyncContext, true);
            }
        }

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
                asyncContext.AcceptSocket = null;
                lock (this.sendAsyncContexts)
                {
                    this.sendAsyncContexts.Add(asyncContext);
                }

                return;
            }

            Array.Copy(client.Packet.Buffer, client.Packet.Position, asyncContext.Buffer, asyncContext.Offset, bytesToSend);

            try
            {
                if (!asyncContext.AcceptSocket.SendAsync(asyncContext))
                {
                    this.ProcessSend(asyncContext);
                }
            }
            catch
            {
                this.Disconnect(asyncContext, false);
            }
        }

        private void ProcessSend(SocketAsyncEventArgs asyncContext)
        {
            if (!this.isRunning || asyncContext.SocketError != SocketError.Success || asyncContext.BytesTransferred == 0)
            {
                this.Disconnect(asyncContext, false);
                return;
            }

            ClientSendContext client = (ClientSendContext)asyncContext.UserToken;
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
                asyncContext.AcceptSocket = null;
                lock (this.sendAsyncContexts)
                {
                    this.sendAsyncContexts.Add(asyncContext);
                }
            }
        }

        private void Disconnect(SocketAsyncEventArgs asyncContext, bool receiveContext)
        {
            ClientContext client = (ClientContext)asyncContext.UserToken;

            try
            {
                asyncContext.AcceptSocket.Shutdown(SocketShutdown.Both);
            }
            catch
            {
            }

            try
            {
                asyncContext.AcceptSocket.Close();
            }
            catch
            {
            }

            asyncContext.AcceptSocket = null;

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
                this.sendBufferManager.FreeBuffer(asyncContext);

                lock (this.sendAsyncContexts)
                {
                    this.sendAsyncContexts.Add(asyncContext);
                }
            }

            this.maxConnectionsEnforcer.Release();

            // notify about disconnection
            this.OnDisconnect(client.RemoteEndPoint);
        }

        #endregion

        #region Event handlers

        void Accept_Completed(object sender, SocketAsyncEventArgs e)
        {
            this.ProcessAccept(e);
        }

        void Receive_Completed(object sender, SocketAsyncEventArgs e)
        {
            this.ProcessReceive(e);
        }

        void Send_Completed(object sender, SocketAsyncEventArgs e)
        {
            this.ProcessSend(e);
        }

        #endregion
    }
}
