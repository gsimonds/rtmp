namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;

    class RtmpPacket
    {
        private PacketBuffer dataPacket = null;

        public RtmpPacket(PacketBuffer dataPacket)
        {
            this.dataPacket = dataPacket;
            throw new NotImplementedException();
        }

        // other public properties/methods TBD
    }
}
