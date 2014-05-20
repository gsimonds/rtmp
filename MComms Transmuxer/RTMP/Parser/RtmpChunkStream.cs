namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;

    /// <summary>
    /// RTMP chunk stream. Contains context of the current chunk stream
    /// to fill up the omitted data in compressed chunk header
    /// </summary>
    public class RtmpChunkStream
    {
        #region Private constants and fields

        /// <summary>
        /// Chunk stream id
        /// </summary>
        private uint chunkStreamId;

        /// <summary>
        /// Current timestamp
        /// </summary>
        private long timestamp = -1;

        /// <summary>
        /// Current timestamp delta
        /// </summary>
        private long timestampDelta = -1;

        /// <summary>
        /// Current message length
        /// </summary>
        private int messageLength = -1;

        /// <summary>
        /// Current message type
        /// </summary>
        private RtmpMessageType messageType = RtmpMessageType.Undefined;

        /// <summary>
        /// Chunk header of currently assembling RTMP message
        /// </summary>
        private RtmpChunkHeader incompleteMessageChunkHeader = null;

        /// <summary>
        /// Stream containing currently assembling RTMP message
        /// </summary>
        private PacketBufferStream incompleteMessageStream = null;

        /// <summary>
        /// Underlying packet buffer containing currently assembling RTMP message
        /// </summary>
        private PacketBuffer incompletePacketBuffer = null;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates new instance of RtmpChunkStream with specified chunk stream id and chunk size
        /// </summary>
        /// <param name="chunkStreamId">Chunk stream id</param>
        /// <param name="chunkSize">Chunk size</param>
        public RtmpChunkStream(uint chunkStreamId, int chunkSize)
        {
            this.chunkStreamId = chunkStreamId;
            this.ChunkSize = chunkSize;
            this.MessageStreamId = -1;
        }

        #endregion

        #region Public properties and methods

        /// <summary>
        /// Gets or sets chunk size of the current chunk stream.
        /// It can be changed dynamically by the streamer
        /// </summary>
        public int ChunkSize { get; set; }

        /// <summary>
        /// Gets or sets message stream id associated with current chunk stream
        /// </summary>
        public int MessageStreamId { get; set; }

        /// <summary>
        /// Decodes RTMP message from specified stream using provided chunk header
        /// </summary>
        /// <param name="hdr">Current chunk header</param>
        /// <param name="dataStream">Stream to read data from</param>
        /// <param name="canContinue">Can we continue parsing, i.e. do we have enough data in the stream to parse at least one more chunk</param>
        /// <returns>
        /// New RTMP message if parsing was successful, null otherwise.
        /// If null returned then it means we need more input data.
        /// </returns>
        public RtmpMessage Decode(RtmpChunkHeader hdr, PacketBufferStream dataStream, ref bool canContinue)
        {
            // apply/save chunk stream context
            switch (hdr.Format)
            {
                case 0:
                    this.timestamp = hdr.Timestamp;
                    this.messageLength = hdr.MessageLength;
                    this.messageType = hdr.MessageType;
                    this.MessageStreamId = hdr.MessageStreamId;
                    break;

                case 1:
                    this.timestampDelta = hdr.TimestampDelta;
                    this.messageLength = hdr.MessageLength;
                    this.messageType = hdr.MessageType;
                    hdr.Timestamp = this.timestamp;
                    hdr.MessageStreamId = this.MessageStreamId;
                    break;

                case 2:
                    this.timestampDelta = hdr.TimestampDelta;
                    hdr.Timestamp = this.timestamp;
                    hdr.MessageLength = this.messageLength;
                    hdr.MessageType = this.messageType;
                    hdr.MessageStreamId = this.MessageStreamId;
                    break;

                case 3:
                    hdr.Timestamp = this.timestamp;
                    hdr.TimestampDelta = this.timestampDelta;
                    hdr.MessageLength = this.messageLength;
                    hdr.MessageType = this.messageType;
                    hdr.MessageStreamId = this.MessageStreamId;
                    break;
            }

            // validate header
            bool bValid = false;
            if (hdr.Timestamp >= 0 && hdr.MessageLength >= 0 && hdr.MessageType != RtmpMessageType.Undefined)
            {
                // check if size is reasonable
                switch (hdr.MessageType)
                {
                    case RtmpMessageType.Video:
                        if (hdr.MessageLength < 10 * 1024 * 1024)
                        {
                            bValid = true;
                        }
                        break;
                    case RtmpMessageType.Audio:
                        if (hdr.MessageLength < 1024 * 1024)
                        {
                            bValid = true;
                        }
                        break;
                    default:
                        if (hdr.MessageLength < 10240)
                        {
                            bValid = true;
                        }
                        break;
                }
            }

            if (!bValid)
            {
                // drop everything in current stream (trying to re-align to the next chunk)
                Global.Log.ErrorFormat("Received corrupted chunk header");
                Global.Log.ErrorFormat("Dropping {0} bytes and re-aligning to the next chunk...", dataStream.Length - dataStream.Position + hdr.HeaderSize);
                dataStream.Seek(0, System.IO.SeekOrigin.End);
                dataStream.TrimBegin();
                return null;
            }

            RtmpMessage msg = null;

            bool assemblingMessage = false;
            if (this.incompleteMessageChunkHeader != null)
            {
                if (this.incompleteMessageChunkHeader.MessageType == hdr.MessageType &&
                    this.incompleteMessageChunkHeader.MessageStreamId == hdr.MessageStreamId &&
                    this.incompleteMessageChunkHeader.MessageLength == hdr.MessageLength)
                {
                    // we need this to process partially received message
                    // after increase of chunk size
                    assemblingMessage = true;
                }
            }

            if (hdr.MessageLength <= this.ChunkSize && !assemblingMessage)
            {
                // simplest case: the whole message in the chunk

                // this is the whole message, add delta to timestamp if it was specified
                if (this.timestampDelta > 0)
                {
                    this.timestamp += this.timestampDelta;
                    hdr.Timestamp = this.timestamp;
                }

                if (hdr.MessageLength > dataStream.Length - dataStream.Position)
                {
                    // can't continue parsing, need to receive more data
                    canContinue = false;
                    dataStream.Seek(-hdr.HeaderSize, System.IO.SeekOrigin.Current);
                    return null;
                }

                //Global.Log.DebugFormat("Received message: {0} bytes, type {1}", hdr.MessageLength, hdr.MessageType);

                msg = RtmpMessage.Decode(hdr, dataStream);
            }
            else
            {
                if (this.incompleteMessageStream == null)
                {
                    if (hdr.MessageLength > Global.MediaAllocator.BufferSize)
                    {
                        // increase buffer sizes
                        Global.MediaAllocator.Reallocate(hdr.MessageLength * 3 / 2, Global.MediaAllocator.BufferCount);
                    }

                    this.incompletePacketBuffer = Global.MediaAllocator.LockBuffer();
                    this.incompletePacketBuffer.ActualBufferSize = this.incompletePacketBuffer.Size;
                    this.incompleteMessageStream = new PacketBufferStream(this.incompletePacketBuffer);
                    this.incompleteMessageStream.OneMessage = true;
                    this.incompleteMessageChunkHeader = hdr;

                    // this is the first chunk of the message, add delta to timestamp if it was specified
                    if (this.timestampDelta > 0)
                    {
                        this.timestamp += this.timestampDelta;
                        hdr.Timestamp = this.timestamp;
                    }
                }

                int chunkLength = Math.Min((int)(hdr.MessageLength - this.incompleteMessageStream.Position), this.ChunkSize);
                if (chunkLength > dataStream.Length - dataStream.Position)
                {
                    // can't continue parsing, need to receive more data
                    canContinue = false;
                    dataStream.Seek(-hdr.HeaderSize, System.IO.SeekOrigin.Current);
                    return null;
                }

                //Global.Log.DebugFormat("Received chunk: {2} bytes, type {1}, message length {0}", hdr.MessageLength, hdr.MessageType, chunkLength);

                // append received chunk to previous ones
                dataStream.CopyTo(this.incompleteMessageStream, chunkLength);

                if (this.incompleteMessageStream.Position >= hdr.MessageLength)
                {
                    //Global.Log.DebugFormat("Received message: {0} bytes (last chunk {2}), type {1}", hdr.MessageLength, hdr.MessageType, chunkLength);

                    // we've received complete message, parse it
                    this.incompletePacketBuffer.ActualBufferSize = (int)this.incompleteMessageStream.Position;
                    this.incompleteMessageStream.Seek(0, System.IO.SeekOrigin.Begin);
                    msg = RtmpMessage.Decode(hdr, this.incompleteMessageStream);

                    // drop parsed data even if message decoding failed
                    // (which means we don't support something in this message)
                    this.incompletePacketBuffer.Release();
                    this.incompleteMessageStream.Dispose();
                    this.incompleteMessageStream = null;
                    this.incompleteMessageChunkHeader = null;
                }
            }

            return msg;
        }

        /// <summary>
        /// Abort current chunk stream, i.e. reset currently receiving RTMP message
        /// </summary>
        public void Abort()
        {
            if (this.incompleteMessageStream != null)
            {
                this.incompleteMessageStream.Dispose();
                this.incompleteMessageStream = null;
                this.incompleteMessageChunkHeader = null;
            }
        }

        #endregion
    }
}
