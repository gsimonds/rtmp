namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// AMF object
    /// </summary>
    public class RtmpAmfObject
    {
        /// <summary>
        /// Creates new instance of RtmpAmfObject
        /// </summary>
        public RtmpAmfObject()
        {
            CurrentProperty = string.Empty;
            Numbers = new Dictionary<string, double>();
            Strings = new Dictionary<string, string>();
            Booleans = new Dictionary<string, bool>();
            Objects = new Dictionary<string, RtmpAmfObject>();
            Nulls = 0;
        }

        /// <summary>
        /// Gets or sets the list of numbers of current AMF object
        /// </summary>
        public Dictionary<string, double> Numbers { get; set; }

        /// <summary>
        /// Gets or sets the list of strings of current AMF object
        /// </summary>
        public Dictionary<string, string> Strings { get; set; }

        /// <summary>
        /// Gets or sets the list of booleans of current AMF object
        /// </summary>
        public Dictionary<string, bool> Booleans { get; set; }

        /// <summary>
        /// Gets or sets the list of sub-objects of current AMF object
        /// </summary>
        public Dictionary<string, RtmpAmfObject> Objects { get; set; }

        /// <summary>
        /// Gets or sets the number of nulls of current AMF object
        /// </summary>
        public uint Nulls { get; set; }

        /// <summary>
        /// Gets of sets currently parsing property. Used by AMF object parser only
        /// </summary>
        public string CurrentProperty { get; set; }
    }
}
