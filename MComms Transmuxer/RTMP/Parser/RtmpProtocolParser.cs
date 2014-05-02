namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;

    public class RtmpProtocolParser
    {
        private int chunkSize = Global.RtmpDefaultChunkSize;
        private PacketBufferStream dataStream = new PacketBufferStream();
        private RtmpHandshake handshake = new RtmpHandshake();
        private Dictionary<uint, RtmpChunkStream> chunkStreams = new Dictionary<uint, RtmpChunkStream>();
        private List<int> registeredMessageStreams = new List<int>();
        private bool aligning = false;

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
                this.dataStream.Append(dataPacket, 0, dataPacket.ActualBufferSize);
            }

            if (this.dataStream.Length == 0)
            {
                return null;
            }

            this.dataStream.Seek(0, System.IO.SeekOrigin.Begin);

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
                        bool canContinue = true;
                        do
                        {
                            RtmpChunkHeader hdr = RtmpChunkHeader.Decode(this.dataStream);
                            // break the loop because of incomplete chunk
                            if (hdr == null) break;

                            RtmpChunkStream chunkStream = null;
                            if (this.chunkStreams.ContainsKey(hdr.ChunkStreamId))
                            {
                                chunkStream = this.chunkStreams[hdr.ChunkStreamId];
                            }
                            else
                            {
                                chunkStream = new RtmpChunkStream(hdr.ChunkStreamId, this.ChunkSize);
                                this.chunkStreams.Add(hdr.ChunkStreamId, chunkStream);
                            }

                            // check if message stream is registered
                            int messageStreamId = hdr.MessageStreamId;
                            if (messageStreamId < 0)
                            {
                                messageStreamId = chunkStream.MessageStreamId;
                            }

                            if (!this.registeredMessageStreams.Contains(messageStreamId))
                            {
                                // unregistered stream: most certainly we've lost chunk synchronization
                                // drop everything in current stream (trying to re-align to the next chunk)
                                if (!this.aligning)
                                {
                                    Global.Log.ErrorFormat("Received unknown message stream {0}", messageStreamId);
                                    Global.Log.ErrorFormat("Dropping {0} bytes and re-aligning to the next chunk...", dataStream.Length - dataStream.Position + hdr.HeaderSize);
                                    this.aligning = true;
                                }
                                dataStream.Seek(0, System.IO.SeekOrigin.End);
                                dataStream.TrimBegin();
                                break;
                            }

                            if (this.aligning)
                            {
                                Global.Log.ErrorFormat("Re-aligned to the chunk start");
                                this.aligning = false;
                            }

                            canContinue = true;
                            msg = this.chunkStreams[hdr.ChunkStreamId].Decode(hdr, this.dataStream, ref canContinue);

                            // break the loop if we've parsed complete message or have incomplete chunk
                            if (msg != null || !canContinue) break;
                        }
                        while (this.dataStream.Position < this.dataStream.Length);

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

        public void RegisterMessageStream(int messageStreamId)
        {
            if (!this.registeredMessageStreams.Contains(messageStreamId))
            {
                this.registeredMessageStreams.Add(messageStreamId);
            }
        }

        public void UnregisterMessageStream(int messageStreamId)
        {
            if (this.registeredMessageStreams.Contains(messageStreamId))
            {
                this.registeredMessageStreams.Remove(messageStreamId);
            }
        }

        public bool IsMessageStreamRegistered(int messageStreamId)
        {
            return this.registeredMessageStreams.Contains(messageStreamId);
        }

        public void Abort(uint chunkStreamId)
        {
            if (this.chunkStreams.ContainsKey(chunkStreamId))
            {
                this.chunkStreams[chunkStreamId].Abort();
            }
        }
    }
}
