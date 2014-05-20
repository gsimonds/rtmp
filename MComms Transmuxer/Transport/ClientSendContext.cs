namespace MComms_Transmuxer.Transport
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;

    /// <summary>
    /// Client send context, generated for each send operation
    /// </summary>
    public class ClientSendContext : ClientContext
    {
        /// <summary>
        /// Creates new empty instance of ClientSendContext
        /// </summary>
        public ClientSendContext()
        {
            this.Created = DateTime.Now;
        }

        /// <summary>
        /// Creates new instance of ClientSendContext
        /// </summary>
        /// <param name="obj">Client context object to take settings from</param>
        public ClientSendContext(ClientContext obj)
        {
            this.Socket = obj.Socket;
            this.RemoteEndPoint = obj.RemoteEndPoint;
            this.Created = DateTime.Now;
        }

        /// <summary>
        /// Packet with data to send
        /// </summary>
        public PacketBuffer Packet { get; set; }

        /// <summary>
        /// When object was created
        /// </summary>
        public DateTime Created { get; set; }
    }
}
