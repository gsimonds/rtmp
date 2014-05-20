namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;

    /// <summary>
    /// Lowest level RTMP protocol decoder/encoder.
    /// Decoder receives PacketBuffer and outputs parsed RtmpMessage objects.
    /// Encoder receives RtmpMessage objects and generates PacketBuffer with ready to send data.
    /// </summary>
    public class RtmpProtocolParser
    {
        #region Private constants and fields

        /// <summary>
        /// Current chunk size. Takes RTMP standard chunk size as default, can be adjusted during a session.
        /// </summary>
        private int chunkSize = Global.RtmpDefaultChunkSize;

        /// <summary>
        /// Abstract stream build on top of underlying packet buffers
        /// </summary>
        private PacketBufferStream dataStream = new PacketBufferStream();

        /// <summary>
        /// Chunks streams
        /// </summary>
        private Dictionary<uint, RtmpChunkStream> chunkStreams = new Dictionary<uint, RtmpChunkStream>();

        /// <summary>
        /// List of registered message streams, used to detect input stream synchronization
        /// </summary>
        private List<int> registeredMessageStreams = new List<int>();

        /// <summary>
        /// Flag indicating that we're in the process of (re)aligning after we've lost synchronization
        /// </summary>
        private bool aligning = false;

        /// <summary>
        /// Output packet queue. We're using List instead of Queue to allow insertion
        /// of high priority packets to the beginning of the queue
        /// </summary>
        private List<PacketBuffer> outputQueue = new List<PacketBuffer>();

#if DEBUG_RTMP_CORRUPTION
        // used to resolve data synchronization issues
        private List<PacketBuffer> history = new List<PacketBuffer>();
        private RtmpMessage lastMessage = null;
#endif

        #endregion

        #region Constructor

        /// <summary>
        /// Creates new instance of RtmpProtocolParser
        /// </summary>
        public RtmpProtocolParser()
        {
            this.State = RtmpSessionState.Uninitialized;

            // allocate data for chunk control stream
            this.chunkStreams.Add(2, new RtmpChunkStream(2, this.ChunkSize));
        }

        #endregion

        #region Public properties and methods

        /// <summary>
        /// We need session state in the parser to know what kind
        /// of data to expect. Makes sense for handshaking stage only.
        /// As soon as handshaking is done, all data is coming
        /// in the same chunked format
        /// </summary>
        public RtmpSessionState State { get; set; }

        /// <summary>
        /// Current chunk size. Takes RTMP standard chunk size as default, can be adjusted during a session.
        /// </summary>
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

        /// <summary>
        /// Decodes RTMP message from the provided packet buffer.
        /// If data is not enough to parse the message then null will be returned.
        /// </summary>
        /// <param name="dataPacket">
        /// Packet buffer with new data. It can be null which means to use already
        /// cached data from dataStream to decode next message.
        /// </param>
        /// <returns>
        /// New RTMP message if parsing was successful, null otherwise.
        /// If null returned then it means we need more input data.
        /// </returns>
        public RtmpMessage Decode(PacketBuffer dataPacket)
        {
            RtmpMessage msg = null;

            if (dataPacket != null)
            {
                // if we have the packet then add it to the end of the stream
                this.dataStream.Append(dataPacket, 0, dataPacket.ActualBufferSize);
#if DEBUG_RTMP_CORRUPTION
                {
                    this.history.Add(dataPacket);
                    dataPacket.AddRef();
                    if (this.history.Count > 3)
                    {
                        this.history[0].Release();
                        this.history.RemoveAt(0);
                    }
                }
#endif
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
                            if (msg != null || !canContinue)
                            {
                                break;
                            }
                        }
                        while (this.dataStream.Position < this.dataStream.Length);

                        // trim everything we've parsed or dropped till current position
                        dataStream.TrimBegin();
                        break;
                    }
            }

#if DEBUG_RTMP_CORRUPTION
            if (msg != null)
            {
                this.lastMessage = msg;
            }
#endif

            return msg;
        }

        /// <summary>
        /// Encodes provided message into RTMP chunk(s) and stores it in internal queue.
        /// The consequent one or more calls to GetSendPacket() have to be used to
        /// return packet buffers with encoded data
        /// </summary>
        /// <param name="msg">RTMP message to encode</param>
        public void Encode(RtmpMessage msg)
        {
            this.outputQueue.Add(msg.ToRtmpChunk());
        }

        /// <summary>
        /// Gets next packet buffer from the output queue
        /// </summary>
        /// <returns>Next packet buffer from the output queue</returns>
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

        /// <summary>
        /// Registers message stream id. We're using message stream ids to detect
        /// lost data synchronization in the input stream
        /// </summary>
        /// <param name="messageStreamId">Message stream id to register</param>
        public void RegisterMessageStream(int messageStreamId)
        {
            if (!this.registeredMessageStreams.Contains(messageStreamId))
            {
                this.registeredMessageStreams.Add(messageStreamId);
            }
        }

        /// <summary>
        /// Unregisters message stream id
        /// </summary>
        /// <param name="messageStreamId">Message stream id to unregister</param>
        public void UnregisterMessageStream(int messageStreamId)
        {
            if (this.registeredMessageStreams.Contains(messageStreamId))
            {
                this.registeredMessageStreams.Remove(messageStreamId);
            }
        }

        /// <summary>
        /// Whether message stream id registered or not
        /// </summary>
        /// <param name="messageStreamId">Message stream id to check</param>
        /// <returns>True if message stream id registered, false otherwise.</returns>
        public bool IsMessageStreamRegistered(int messageStreamId)
        {
            return this.registeredMessageStreams.Contains(messageStreamId);
        }

        /// <summary>
        /// Aborts specified chunk stream
        /// </summary>
        /// <param name="chunkStreamId">Chunk stream to abort</param>
        public void Abort(uint chunkStreamId)
        {
            if (this.chunkStreams.ContainsKey(chunkStreamId))
            {
                this.chunkStreams[chunkStreamId].Abort();
            }
        }

        #endregion
    }
}
