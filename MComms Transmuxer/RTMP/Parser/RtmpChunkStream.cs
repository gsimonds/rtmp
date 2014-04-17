namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;

    class RtmpChunkStream
    {
        private uint chunkStreamId;
        private long timestamp = -1;
        private long timestampDelta = -1;
        private int messageLength = -1;
        private RtmpMessageType messageType = RtmpMessageType.Undefined;

        private PacketBufferStream incompleteMessageStream = null;
        private RtmpChunkHeader incompleteMessageChunkHeader = null;

        public RtmpChunkStream(uint chunkStreamId, int chunkSize)
        {
            this.chunkStreamId = chunkStreamId;
            this.ChunkSize = chunkSize;
            this.MessageStreamId = -1;
        }

        public int ChunkSize { get; set; }

        public int MessageStreamId { get; set; }

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
                bValid = true;
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
                    return null;
                }

                //Global.Log.DebugFormat("Received message: {0} bytes, type {1}", hdr.MessageLength, hdr.MessageType);

                msg = RtmpMessage.Decode(hdr, dataStream);

                // drop parsed data even if message decoding failed
                // (which means we don't support something in this message)
                dataStream.TrimBegin();
            }
            else
            {
                if (this.incompleteMessageStream == null)
                {
                    this.incompleteMessageStream = new PacketBufferStream();
                    this.incompleteMessageChunkHeader = hdr;

                    // this is the first chunk of the message, add delta to timestamp if it was specified
                    if (this.timestampDelta > 0)
                    {
                        this.timestamp += this.timestampDelta;
                        hdr.Timestamp = this.timestamp;
                    }
                }

                int chunkLength = Math.Min((int)(hdr.MessageLength - this.incompleteMessageStream.Length), this.ChunkSize);
                if (chunkLength > dataStream.Length - dataStream.Position)
                {
                    // can't continue parsing, need to receive more data
                    canContinue = false;
                    return null;
                }

                //Global.Log.DebugFormat("Received chunk: {2} bytes, type {1}, message length {0}", hdr.MessageLength, hdr.MessageType, chunkLength);

                // append received chunk to previous ones
                dataStream.CopyTo(this.incompleteMessageStream, chunkLength);
                dataStream.TrimBegin();

                if (this.incompleteMessageStream.Length >= hdr.MessageLength)
                {
                    //Global.Log.DebugFormat("Received message: {0} bytes (last chunk {2}), type {1}", hdr.MessageLength, hdr.MessageType, chunkLength);

                    // we've received complete message, parse it
                    this.incompleteMessageStream.Seek(0, System.IO.SeekOrigin.Begin);
                    msg = RtmpMessage.Decode(hdr, this.incompleteMessageStream);

                    // drop parsed data even if message decoding failed
                    // (which means we don't support something in this message)
                    this.incompleteMessageStream.Dispose();
                    this.incompleteMessageStream = null;
                    this.incompleteMessageChunkHeader = null;
                }
            }

            return msg;
        }

        public void Abort()
        {
            if (this.incompleteMessageStream != null)
            {
                this.incompleteMessageStream.Dispose();
                this.incompleteMessageStream = null;
                this.incompleteMessageChunkHeader = null;
            }
        }
    }
}
