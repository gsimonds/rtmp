namespace MComms_Transmuxer.Transport
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading.Tasks;

    public class ClientContext
    {
        public Socket Socket { get; set; }
        public IPEndPoint RemoteEndPoint { get; set; }
        public EventHandler<TransportArgs> ReceiveEventHandler { get; set; }
    }
}
