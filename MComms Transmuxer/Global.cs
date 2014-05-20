[assembly: log4net.Config.XmlConfigurator(Watch = false)]
namespace MComms_Transmuxer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;

    /// <summary>
    /// Global static data common for the whole application
    /// </summary>
    class Global
    {
        /// <summary>
        /// Supported RTMP version
        /// </summary>
        public const byte RtmpVersion = 3;

        /// <summary>
        /// RTMP handshake size
        /// </summary>
        public const int RtmpHandshakeSize = 1536;

        /// <summary>
        /// RTMP handshake random bytes size
        /// </summary>
        public const int RtmpHandshakeRandomBytesSize = 1528;

        /// <summary>
        /// RTMP default chunk size
        /// </summary>
        public const int RtmpDefaultChunkSize = 128;

        /// <summary>
        /// RTMP default ack window size
        /// </summary>
        public const int RtmpDefaultAckWindowSize = 2500000;

        /// <summary>
        /// Maximum number of simultaneous RTMP connections.
        /// </summary>
        public const int RtmpMaxConnections = 150;

        /// <summary>
        /// Chunk size of our messages
        /// </summary>
        public const int RtmpOurChunkSize = 1024;

        /// <summary>
        /// Inactivity timeout after which RTMP session will be disposed
        /// </summary>
        public const int RtmpSessionInactivityTimeoutMs = 30000;

        /// <summary>
        /// Transport buffer size used to accumulate received data.
        /// This must be equal or bigger than RtmpOurChunkSize
        /// </summary>
        public const int TransportBufferSize = 8192;

        /// <summary>
        /// Buffer size to store one whole media frame, including I-frame in full HD resolution
        /// </summary>
        public const int OneMediaBufferSize = 1024 * 1024;

        /// <summary>
        /// Buffer size to store one segment data.
        /// </summary>
        public const int SegmentBufferSize = 10 * 1024 * 1024;

        /// <summary>
        /// Timescale of the smooth streaming timstamps
        /// </summary>
        public const long SmoothStreamingTimescale = 10000000;

        /// <summary>
        /// Allocator is used for transport purpose. Buffer size is relatively small
        /// (should not be too small though because it reduces socket transport performance).
        /// Number of buffers should be big
        /// </summary>
        public static PacketBufferAllocator Allocator { get; set; }

        /// <summary>
        /// Allocator is used to accumulate media packet data. Buffer size should be
        /// big enough to store one I-frame in full HD resolution. Number of buffers should be
        /// equal to a maximum number of simultaneous message streams with media.
        /// </summary>
        public static PacketBufferAllocator MediaAllocator { get; set; }

        /// <summary>
        /// Allocator is used to store the whole segment. Buffer size should be big enough
        /// to store 2 seconds of video data in full HD resolution. Number of buffers should be relatively small
        /// because this buffer is used for a very short time every 2-5 seconds for every connection.
        /// </summary>
        public static PacketBufferAllocator SegmentAllocator { get; set; }

        /// <summary>
        /// Logger
        /// </summary>
        public static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(Program));
    }
}
