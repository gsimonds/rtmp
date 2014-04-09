namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;

    class RtmpProtocolParser
    {
        private PacketBufferStream dataStream = new PacketBufferStream();
        private RtmpHandshake handshake = new RtmpHandshake();
        private Dictionary<int, RtmpChunkStream> chunkStreams = new Dictionary<int, RtmpChunkStream>();

        public RtmpProtocolParser()
        {
            this.State = RtmpSessionState.Uninitialized;
        }

        /// <summary>
        /// We need session state in the parser to know what kind
        /// of data to expect. Makes sense for handshaking stage only.
        /// As soon as handshaking is finished, all data is coming
        /// in the same chunked format
        /// </summary>
        public RtmpSessionState State { get; set; }

        public RtmpMessage Decode(PacketBuffer dataPacket)
        {
            RtmpMessage msg = null;

            if (dataPacket != null)
            {
                // if we have the packet then add it to the end of the stream
                if (this.dataStream.Length > 0)
                {
                    this.dataStream.Seek(0, System.IO.SeekOrigin.End);
                }
                this.dataStream.Insert(dataPacket, 0, dataPacket.ActualBufferSize);
                this.dataStream.Seek(0, System.IO.SeekOrigin.Begin);
            }

            switch (this.State)
            {
                case RtmpSessionState.Uninitialized:
                    {
                        msg = RtmpHandshake.DecodeC0(this.dataStream);
                        break;
                    }

                case RtmpSessionState.HanshakeVersionSent:
                    {
                        msg = RtmpHandshake.DecodeC1(this.dataStream);
                        break;
                    }

                case RtmpSessionState.HanshakeAckSent:
                    {
                        msg = RtmpHandshake.DecodeC2(this.dataStream);
                        break;
                    }

                default:
                    {
                        // any other state means that handshake has been finished
                        // TODO: implement
                        break;
                    }
            }

            return msg;
        }

        public PacketBuffer Encode(RtmpMessage dataPacket)
        {
            throw new NotImplementedException();
        }
    }
}
