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
        private int chunkSize = Global.RtmpDefaultChunkSize;
        private PacketBufferStream dataStream = new PacketBufferStream();
        private RtmpHandshake handshake = new RtmpHandshake();
        private Dictionary<uint, RtmpChunkStream> chunkStreams = new Dictionary<uint, RtmpChunkStream>();

        /// <summary>
        /// Output packet queue. We're using List instead of Queue to allow insertion
        /// of high priority packets to the beginning of the queue
        /// </summary>
        private List<PacketBuffer> outputQueue = new List<PacketBuffer>();

        public RtmpProtocolParser()
        {
            this.State = RtmpSessionState.Uninitialized;

            // allocate data for chunk control stream
            this.chunkStreams.Add(2, new RtmpChunkStream(2, this.ChunkSize));
        }

        /// <summary>
        /// We need session state in the parser to know what kind
        /// of data to expect. Makes sense for handshaking stage only.
        /// As soon as handshaking is done, all data is coming
        /// in the same chunked format
        /// </summary>
        public RtmpSessionState State { get; set; }

        public int ChunkSize
        {
            get { return this.chunkSize; }
            set
            {
                this.chunkSize = value;
                foreach (RtmpChunkStream chunkStream in this.chunkStreams.Values)
                {
                    chunkStream.ChunkSize = value;
                }
            }
        }

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

            if (this.dataStream.Length == 0)
            {
                return null;
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
                        RtmpChunkHeader hdr = RtmpChunkHeader.Decode(this.dataStream);

                        if (hdr != null)
                        {
                            if (!this.chunkStreams.ContainsKey(hdr.ChunkStreamId))
                            {
                                this.chunkStreams.Add(hdr.ChunkStreamId, new RtmpChunkStream(hdr.ChunkStreamId, this.ChunkSize));
                            }

                            msg = this.chunkStreams[hdr.ChunkStreamId].Decode(hdr, this.dataStream);
                        }

                        break;
                    }
            }

            return msg;
        }

        public void Encode(RtmpMessage msg)
        {
            this.outputQueue.Add(msg.ToPacketBuffer());
        }

        public PacketBuffer GetSendPacket()
        {
            if (this.outputQueue.Count > 0)
            {
                PacketBuffer packet = this.outputQueue[0];
                this.outputQueue.RemoveAt(0);
                return packet;
            }
            else
            {
                return null;
            }
        }
    }
}
