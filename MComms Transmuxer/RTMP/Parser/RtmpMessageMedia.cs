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
        public RtmpMessageMedia(RtmpVideoCodec videoCodec, RtmpMediaPacketType packetType, int decoderDelay, bool keyFrame)
        {
            this.MessageType = RtmpIntMessageType.Video;
            this.OrigMessageType = RtmpMessageType.Video;
            this.ContentType = MediaContentType.Video;
            this.PacketType = packetType;
            this.DecoderDelay = decoderDelay;
            this.MediaDataOffset = 5; // RTMP media packet header: 1 byte, packet type: 1 byte, decoder delay: 3 bytes
            this.VideoCodec = videoCodec;
            this.KeyFrame = keyFrame;
        }

        public RtmpMessageMedia(RtmpAudioCodec audioCodec, RtmpMediaPacketType packetType, int sampleRate, int sampleSize, int channels)
        {
            this.MessageType = RtmpIntMessageType.Audio;
            this.OrigMessageType = RtmpMessageType.Audio;
            this.ContentType = MediaContentType.Audio;
            this.PacketType = packetType;
            this.MediaDataOffset = 2; // RTMP media packet header: 1 byte, packet type: 1 byte
            this.AudioCodec = audioCodec;
            this.SampleRate = sampleRate;
            this.SampleSize = sampleSize;
            this.Channels = channels;
        }

        public MediaContentType ContentType { get; set; }

        public RtmpMediaPacketType PacketType { get; set; }

        public int MediaDataOffset { get; set; }

        public RtmpVideoCodec VideoCodec { get; set; }

        public bool KeyFrame { get; set; }

        public int DecoderDelay { get; set; }

        public RtmpAudioCodec AudioCodec { get; set; }

        public int SampleRate { get; set; }

        public int Channels { get; set; }

        public int SampleSize { get; set; }

        public PacketBuffer MediaData { get; set; }
    }
}
