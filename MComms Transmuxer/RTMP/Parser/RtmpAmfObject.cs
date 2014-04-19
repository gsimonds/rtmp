namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class RtmpAmfObject
    {
        public string CurrentProperty { get; set; }

        public Dictionary<string, double> Numbers { get; set; }

        public Dictionary<string, string> Strings { get; set; }

        public Dictionary<string, bool> Booleans { get; set; }

        public Dictionary<string, RtmpAmfObject> Objects { get; set; }

        public uint Nulls { get; set; }

        public RtmpAmfObject()
        {
            CurrentProperty = string.Empty;
            Numbers = new Dictionary<string, double>();
            Strings = new Dictionary<string, string>();
            Booleans = new Dictionary<string, bool>();
            Objects = new Dictionary<string, RtmpAmfObject>();
            Nulls = 0;
        }
    }
}
