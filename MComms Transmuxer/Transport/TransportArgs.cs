namespace MComms_Transmuxer.Transport
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;

    public class TransportArgs : EventArgs
    {
        public TransportArgs(IPEndPoint endPoint) :
            this(endPoint, null)
        {
        }

        public TransportArgs(IPEndPoint endPoint, PacketBuffer packet)
        {
            this.EndPoint = endPoint;
            this.Packet = packet;
        }

        public IPEndPoint EndPoint { get; set; }

        public PacketBuffer Packet { get; set; }
    }
}
