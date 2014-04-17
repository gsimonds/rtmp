[assembly: log4net.Config.XmlConfigurator(Watch = false)]
namespace MComms_Transmuxer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;

    class Global
    {
        public const byte RtmpVersion = 3;
        public const int RtmpHandshakeSize = 1536;
        public const int RtmpHandshakeRandomBytesSize = 1528;
        public const int RtmpDefaultChunkSize = 128;
        public const int RtmpDefaultAckWindowSize = 2500000;

        public static PacketBufferAllocator Allocator { get; set; }

        public static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(Program));
    }
}
