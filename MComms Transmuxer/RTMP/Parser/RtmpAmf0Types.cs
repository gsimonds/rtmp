namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// AMF0 types
    /// </summary>
    public enum RtmpAmf0Types : byte
    {
        /// <summary>
        /// Number (always double)
        /// </summary>
        Number = 0x00,

        /// <summary>
        /// Boolean
        /// </summary>
        Boolean = 0x01,

        /// <summary>
        /// String
        /// </summary>
        String = 0x02,

        /// <summary>
        /// Object, contains other types
        /// </summary>
        Object = 0x03,

        /// <summary>
        /// Null
        /// </summary>
        Null = 0x05,

        /// <summary>
        /// Array, contains the list of values of the same type
        /// </summary>
        Array = 0x08,

        /// <summary>
        /// Object end indicator
        /// </summary>
        ObjectEnd = 0x09,
    }
}
