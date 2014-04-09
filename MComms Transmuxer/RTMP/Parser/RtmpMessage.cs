namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;

    class RtmpMessage
    {
        public RtmpMessage()
        {
        }

        public RtmpMessageType MessageType { get; set; }

        public int MessageStreamId { get; set; }

        public int Timestamp { get; set; }

        // other public properties/methods TBD
    }
}
