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
            this(endPoint, null, 0, 0)
        {
        }

        public TransportArgs(IPEndPoint endPoint, byte[] data, int dataOffset, int dataLength)
        {
            this.EndPoint = endPoint;
            this.Data = data;
            this.DataOffset = dataOffset;
            this.DataLength = dataLength;
            this.Packet = null;
        }

        public TransportArgs(IPEndPoint endPoint, PacketBuffer packet)
        {
            this.EndPoint = endPoint;
            this.Data = null;
            this.DataOffset = 0;
            this.DataLength = 0;
            this.Packet = packet;
        }

        public IPEndPoint EndPoint { get; set; }

        public byte[] Data { get; set; }

        public int DataOffset { get; set; }

        public int DataLength { get; set; }

        public PacketBuffer Packet { get; set; }

        public EventHandler<TransportArgs> ReceiveEventHandler { get; set; }
    }
}
