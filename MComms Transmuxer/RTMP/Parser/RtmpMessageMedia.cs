namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;

    /// <summary>
    /// RTMP message "Audio" or "Video". Used to store media data.
    /// </summary>
    public class RtmpMessageMedia : RtmpMessage
    {
        /// <summary>
        /// Creates new instance of video message
        /// </summary>
        /// <param name="videoCodec">Video codec</param>
        /// <param name="packetType">Media packet type</param>
        /// <param name="decoderDelay">Decoder delay (H.264 specific)</param>
        /// <param name="keyFrame">Whether it's a keyframe</param>
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

        /// <summary>
        /// Creates new instance of audio message
        /// </summary>
        /// <param name="audioCodec">Audio codec</param>
        /// <param name="packetType">Media packet type</param>
        /// <param name="sampleRate">Sampling rate</param>
        /// <param name="sampleSize">Sample size</param>
        /// <param name="channels">Number of channels</param>
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

        /// <summary>
        /// Gets or sets major content type
        /// </summary>
        public MediaContentType ContentType { get; set; }

        /// <summary>
        /// Gets or sets packet type
        /// </summary>
        public RtmpMediaPacketType PacketType { get; set; }

        /// <summary>
        /// Gets or sets offset of the beginning of raw bitstream in MediaData
        /// </summary>
        public int MediaDataOffset { get; set; }

        /// <summary>
        /// Gets or sets video codec
        /// </summary>
        public RtmpVideoCodec VideoCodec { get; set; }

        /// <summary>
        /// Gets or sets whether it is a keyframe
        /// </summary>
        public bool KeyFrame { get; set; }

        /// <summary>
        /// Gets or sets decoder delay (H.264 specific)
        /// </summary>
        public int DecoderDelay { get; set; }

        /// <summary>
        /// Gets or sets audio codec
        /// </summary>
        public RtmpAudioCodec AudioCodec { get; set; }

        /// <summary>
        /// Gets or sets sampling rate
        /// </summary>
        public int SampleRate { get; set; }

        /// <summary>
        /// Gets or sets number of channels
        /// </summary>
        public int Channels { get; set; }

        /// <summary>
        /// Gets or sets sample size
        /// </summary>
        public int SampleSize { get; set; }

        /// <summary>
        /// Gets or sets packet buffer containing media message header and raw media data.
        /// Media message header format is specific to media/codec types.
        /// Offset of the raw media data specified in MediaDataOffset
        /// </summary>
        public PacketBuffer MediaData { get; set; }
    }
}
