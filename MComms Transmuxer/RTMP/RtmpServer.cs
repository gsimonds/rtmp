namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Transport;

    class RtmpServer
    {
        private SocketTransport transport = new SocketTransport();

        public RtmpServer()
        {
            throw new NotImplementedException();
        }

        public int Start()
        {
            transport.Start(new IPEndPoint(IPAddress.Any, 1935), System.Net.Sockets.ProtocolType.Tcp);
            throw new NotImplementedException();
        }

        public int Stop()
        {
            transport.Stop();
            throw new NotImplementedException();
        }

        // other public properties/methods TBD
    }
}
