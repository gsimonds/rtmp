namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class RtmpMessageMetadata : RtmpMessage
    {
        public RtmpMessageMetadata(List<object> parameters)
        {
            this.Parameters = parameters;
            this.MessageType = RtmpIntMessageType.Data;
        }

        public List<object> Parameters { get; set; }
    }
}
