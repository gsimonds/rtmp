namespace MComms_Transmuxer.Transport
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Connected client context
    /// </summary>
    public class ClientContext
    {
        /// <summary>
        /// Associated socket
        /// </summary>
        public Socket Socket { get; set; }

        /// <summary>
        /// Remote IP end point
        /// </summary>
        public IPEndPoint RemoteEndPoint { get; set; }

        /// <summary>
        /// Receive event handler. Used by RTMP session to re-route receive event directly to RTMP session
        /// </summary>
        public EventHandler<TransportArgs> ReceiveEventHandler { get; set; }
    }
}
