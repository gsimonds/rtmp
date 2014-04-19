namespace MComms_Transmuxer.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class MediaPacket
    {
        public MediaPacket()
        {
        }

        public long Pts { get; set; }

        public long Dts { get; set; }

        public bool IsKeyFrame { get; set; }

        public PacketBuffer MediaData { get; set; }

        public MediaType MediaType { get; set; }
    }
}
