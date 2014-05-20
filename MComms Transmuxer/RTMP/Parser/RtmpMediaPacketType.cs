namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// RTMP media packet type
    /// </summary>
    public enum RtmpMediaPacketType
    {
        /// <summary>
        /// Configuration packet, should be a type of the first packet in audio/video streams
        /// </summary>
        Configuration = 0,

        /// <summary>
        /// Normal media packet with media data
        /// </summary>
        Media = 1,

        /// <summary>
        /// Should be the a type of the last packet in audio/video streams
        /// </summary>
        Eos = 2,
    }
}
