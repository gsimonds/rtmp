namespace MComms_Transmuxer.Transport
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;

    /// <summary>
    /// General socket transport class. Implements both server and client
    /// </summary>
    class SocketTransport
    {
        public SocketTransport()
        {
            throw new NotImplementedException();
        }

        public event EventHandler<TransportArgs> Connected;
        public event EventHandler<TransportArgs> Disconnected;
        public event EventHandler<TransportArgs> Received;
        public event EventHandler<TransportArgs> Sent;

        public int Start(IPEndPoint serverEndPoint = null, ProtocolType protocolType = ProtocolType.Unspecified)
        {
            throw new NotImplementedException();
        }

        public int Stop()
        {
            throw new NotImplementedException();
        }

        public int Send(IPEndPoint endPoint, PacketBuffer packet)
        {
            throw new NotImplementedException();
        }

        public int Connect(IPEndPoint endPoint, ProtocolType protocolType)
        {
            throw new NotImplementedException();
        }

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

        protected virtual void OnReceive(IPEndPoint endPoint, PacketBuffer packet)
        {
            if (this.Received != null)
            {
                this.Received(this, new TransportArgs(endPoint, packet));
            }
        }

        protected virtual void OnSent(IPEndPoint endPoint, PacketBuffer packet)
        {
            if (this.Sent != null)
            {
                this.Sent(this, new TransportArgs(endPoint, packet));
            }
        }
    }
}
