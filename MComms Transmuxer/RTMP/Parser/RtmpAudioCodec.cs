namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Audio codecs supported by RTMP
    /// </summary>
    public enum RtmpAudioCodec
    {
        PCM = 0,
        ADPCM = 1,
        MP3 = 2,
        PCMLE = 3,
        NellyMoser16khzMono = 4,
        NellyMoser8khzMono = 5,
        NellyMoser = 6,
        G711A = 7,
        G711MU = 8,
        Reserved = 9,
        AAC = 10,
        Speex = 11,
        Reserved2 = 12,
        Reserved3 = 13,
        MP38khz = 14,
        MP38DevSpec = 15,
    }
}
