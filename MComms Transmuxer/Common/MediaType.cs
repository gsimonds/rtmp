namespace MComms_Transmuxer.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class MediaType
    {
        public MediaType()
        {
        }

        // common media type properties

        public MediaContentType ContentType { get; set; }

        public MediaCodec Codec { get; set; }

        public int Bitrate { get; set; }

        public byte[] ExtraData { get; set; }

        // video specific properties

        public int Width { get; set; }

        public int Height { get; set; }

        public Fraction Framerate { get; set; }

        public Fraction PAR { get; set; }

        // audio specific properties

        public int SampleRate { get; set; }

        public int Channels { get; set; }

        public int SampleSize { get; set; }
    }
}
