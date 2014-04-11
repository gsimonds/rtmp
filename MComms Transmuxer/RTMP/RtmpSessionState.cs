namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    enum RtmpSessionState
    {
        Uninitialized = 0,
        HanshakeVersionSent,
        HanshakeAckSent,
        Receiving,
        Stopped,
    }
}
