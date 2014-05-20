namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// RTMP session state
    /// </summary>
    public enum RtmpSessionState
    {
        /// <summary>
        /// Session uninitialized
        /// </summary>
        Uninitialized = 0,

        /// <summary>
        /// We've sent handhsake version
        /// </summary>
        HanshakeVersionSent,

        /// <summary>
        /// We've sent handshake ack
        /// </summary>
        HanshakeAckSent,

        /// <summary>
        /// Handshake has been successful, we're receiving RTMP data
        /// </summary>
        Receiving,

        /// <summary>
        /// Session stopped
        /// </summary>
        Stopped,
    }
}
