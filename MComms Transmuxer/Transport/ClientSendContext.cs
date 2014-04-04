namespace MComms_Transmuxer.Transport
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;

    class ClientSendContext : ClientContext
    {
        public ClientSendContext()
        {
        }

        public ClientSendContext(ClientContext obj)
        {
            this.Socket = obj.Socket;
        }

        public PacketBuffer Packet { get; set; }
    }
}
