namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;

    public class RtmpMessageMedia : RtmpMessage
    {
        public RtmpMessageMedia(RtmpVideoCodec videoCodec, bool keyFrame)
        {
            this.MessageType = RtmpIntMessageType.Video;
            this.ContentType = MediaContentType.Video;
            this.VideoCodec = videoCodec;
            this.KeyFrame = keyFrame;
        }

        public RtmpMessageMedia(RtmpAudioCodec audioCodec, int sampleRate, int sampleSize, int channels)
        {
            this.MessageType = RtmpIntMessageType.Audio;
            this.ContentType = MediaContentType.Audio;
            this.AudioCodec = audioCodec;
            this.SampleRate = sampleRate;
            this.SampleSize = sampleSize;
            this.Channels = channels;
        }

        public MediaContentType ContentType { get; set; }

        public RtmpVideoCodec VideoCodec { get; set; }

        public bool KeyFrame { get; set; }

        public RtmpAudioCodec AudioCodec { get; set; }

        public int SampleRate { get; set; }

        public int Channels { get; set; }

        public int SampleSize { get; set; }

        public PacketBuffer MediaData { get; set; }
    }
}
