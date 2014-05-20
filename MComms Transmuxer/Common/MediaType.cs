namespace MComms_Transmuxer.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Type used to describe media parameters of the given stream
    /// </summary>
    public class MediaType
    {
        /// <summary>
        /// Constructs empty media type
        /// </summary>
        public MediaType()
        {
        }

        // Common media type properties

        /// <summary>
        /// Content type
        /// </summary>
        public MediaContentType ContentType { get; set; }

        /// <summary>
        /// Codec
        /// </summary>
        public MediaCodec Codec { get; set; }

        /// <summary>
        /// Bitrate
        /// </summary>
        public int Bitrate { get; set; }

        /// <summary>
        /// Codec private data
        /// </summary>
        public byte[] PrivateData { get; set; }

        /// <summary>
        /// Codec private data converted to IIS compatible string
        /// </summary>
        public string PrivateDataIisString { get; set; }

        // video specific properties

        /// <summary>
        /// Video width
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Video height
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Video framerate
        /// </summary>
        public Fraction Framerate { get; set; }

        /// <summary>
        /// Video pixel aspect ratio
        /// </summary>
        public Fraction PAR { get; set; }

        // audio specific properties

        /// <summary>
        /// Sampling rate
        /// </summary>
        public int SampleRate { get; set; }

        /// <summary>
        /// Channel number
        /// </summary>
        public int Channels { get; set; }

        /// <summary>
        /// Sample size, i.e. bit depth
        /// </summary>
        public int SampleSize { get; set; }

        /// <summary>
        /// Compares object's codec private data with the given one
        /// </summary>
        /// <param name="privateData">Codec private data to compare to</param>
        /// <returns>True if private data arrays are equal, false otherwise</returns>
        public bool IsPrivateDataEqual(byte[] privateData)
        {
            if (this.PrivateData == null && privateData == null) return true;
            if (this.PrivateData == null || privateData == null) return false;
            if (this.PrivateData.Length != privateData.Length) return false;
            for (int i = 0; i < this.PrivateData.Length; ++i)
            {
                if (this.PrivateData[i] != privateData[i]) return false;
            }
            return true;
        }
    }
}
