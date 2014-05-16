﻿namespace MComms_Transmuxer.Transport
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;

    public class ClientSendContext : ClientContext
    {
        public ClientSendContext()
        {
        }

        public ClientSendContext(ClientContext obj)
        {
            this.Socket = obj.Socket;
            this.RemoteEndPoint = obj.RemoteEndPoint;
        }

        public PacketBuffer Packet { get; set; }
    }
}
