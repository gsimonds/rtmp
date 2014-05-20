namespace MComms_Transmuxer.Transport
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;

    /// <summary>
    /// Transport event handler's argument
    /// </summary>
    public class TransportArgs : EventArgs
    {
        /// <summary>
        /// Creates new instance of TransportArgs
        /// </summary>
        /// <param name="endPoint">End point responsible for this event</param>
        public TransportArgs(IPEndPoint endPoint) :
            this(endPoint, null, 0, 0)
        {
        }

        /// <summary>
        /// Creates new instance of TransportArgs
        /// </summary>
        /// <param name="endPoint">End point responsible for this event</param>
        /// <param name="data">Data for this event</param>
        /// <param name="dataOffset">Data offset</param>
        /// <param name="dataLength">Data length</param>
        public TransportArgs(IPEndPoint endPoint, byte[] data, int dataOffset, int dataLength)
        {
            this.EndPoint = endPoint;
            this.Data = data;
            this.DataOffset = dataOffset;
            this.DataLength = dataLength;
            this.Packet = null;
        }

        /// <summary>
        /// Creates new instance of TransportArgs
        /// </summary>
        /// <param name="endPoint">End point responsible for this event</param>
        /// <param name="packet">Packet buffer for this event</param>
        public TransportArgs(IPEndPoint endPoint, PacketBuffer packet)
        {
            this.EndPoint = endPoint;
            this.Data = null;
            this.DataOffset = 0;
            this.DataLength = 0;
            this.Packet = packet;
        }

        /// <summary>
        /// Gets or sets end point responsible for this event
        /// </summary>
        public IPEndPoint EndPoint { get; set; }

        /// <summary>
        /// Gets or sets data for this event
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// Gets or sets data offset
        /// </summary>
        public int DataOffset { get; set; }

        /// <summary>
        /// Gets or sets data length
        /// </summary>
        public int DataLength { get; set; }

        /// <summary>
        /// Gets or sets packet buffer for this event
        /// </summary>
        public PacketBuffer Packet { get; set; }

        /// <summary>
        /// Gets or sets receive event handler
        /// </summary>
        public EventHandler<TransportArgs> ReceiveEventHandler { get; set; }
    }
}
