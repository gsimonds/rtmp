namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;

    public class RtmpMessageStream
    {
        public RtmpMessageStream(int messageStreamId)
        {
            this.FirstVideoFrame = true;
            this.FirstAudioFrame = true;
            this.MuxId = -1;
            this.VideoStreamId = -1;
            this.AudioStreamId = -1;
        }

        public int MessageStreamId { get; set; }

        public string PublishName { get; set; }

        public MediaType VideoMediaType { get; set; }

        public MediaType AudioMediaType { get; set; }

        public bool FirstVideoFrame { get; set; }

        public bool FirstAudioFrame { get; set; }

        public int MuxId { get; set; }

        public int VideoStreamId { get; set; }

        public int AudioStreamId { get; set; }

        public HttpWebRequest WebRequest { get; set; }

        public Stream WebRequestStream { get; set; }
    }
}
