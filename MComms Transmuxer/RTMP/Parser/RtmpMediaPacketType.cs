namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public enum RtmpMediaPacketType
    {
        Configuration = 0,
        Media = 1,
        Eos = 2,
    }
}
