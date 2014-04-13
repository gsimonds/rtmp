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
        private int messageStreamId = -1;

        private Dictionary<long, PacketBufferStream> incompleteMessageStreams = new Dictionary<long,PacketBufferStream>();

        public RtmpChunkStream(uint chunkStreamId, int chunkSize)
        {
            this.chunkStreamId = chunkStreamId;
            this.ChunkSize = chunkSize;
        }

        public int ChunkSize { get; set; }

        public RtmpMessage Decode(RtmpChunkHeader hdr, PacketBufferStream dataStream)
        {
            // apply/save chunk stream context
            switch (hdr.Format)
            {
                case 0:
                    this.timestamp = hdr.Timestamp;
                    this.messageLength = hdr.MessageLength;
                    this.messageType = hdr.MessageType;
                    this.messageStreamId = hdr.MessageStreamId;
                    break;
                case 1:
                    this.timestampDelta = hdr.TimestampDelta;
                    this.timestamp += this.timestampDelta;
                    this.messageLength = hdr.MessageLength;
                    this.messageType = hdr.MessageType;
                    hdr.Timestamp = this.timestamp;
                    hdr.TimestampDelta = this.timestampDelta;
                    hdr.MessageStreamId = this.messageStreamId;
                    break;
                case 2:
                    this.timestampDelta = hdr.TimestampDelta;
                    this.timestamp += this.timestampDelta;
                    hdr.Timestamp = this.timestamp;
                    hdr.TimestampDelta = this.timestampDelta;
                    hdr.MessageLength = this.messageLength;
                    hdr.MessageType = this.messageType;
                    hdr.MessageStreamId = this.messageStreamId;
                    break;
                case 3:
                    if (this.timestampDelta > 0)
                    {
                        this.timestamp += this.timestampDelta;
                    }
                    hdr.Timestamp = this.timestamp;
                    hdr.TimestampDelta = this.timestampDelta;
                    hdr.MessageLength = this.messageLength;
                    hdr.MessageType = this.messageType;
                    hdr.MessageStreamId = this.messageStreamId;
                    break;
            }

            RtmpMessage msg = null;

            if (hdr.MessageLength <= this.ChunkSize)
            {
                // simplest case: the whole message in the chunk
                if (hdr.MessageLength > dataStream.Length - dataStream.Position)
                {
                    return null; // not enough data
                }

                Debug.WriteLine("Received message: {0} bytes, type {1}", hdr.MessageLength, hdr.MessageType);

                msg = RtmpMessage.Decode(hdr, dataStream);

                if (dataStream.Position <= 12)
                {
                    int n = 1;
                }

                // drop parsed data even if message decoding failed
                // (which means we don't support something in this message)
                dataStream.TrimBegin();
            }
            else
            {
                PacketBufferStream msgStream = null;

                if (incompleteMessageStreams.ContainsKey(hdr.Timestamp))
                {
                    msgStream = incompleteMessageStreams[hdr.Timestamp];
                }
                else
                {
                    // create new entry
                    msgStream = new PacketBufferStream();
                    incompleteMessageStreams.Add(hdr.Timestamp, msgStream);
                }

                int chunkLength = Math.Min((int)(hdr.MessageLength - msgStream.Length), this.ChunkSize);
                if (chunkLength > dataStream.Length - dataStream.Position)
                {
                    return null; // not enough data
                }

                Debug.WriteLine("Received chunk: {2} bytes, type {1}, message length {0}", hdr.MessageLength, hdr.MessageType, chunkLength);

                // append received chunk to previous ones
                dataStream.CopyTo(msgStream, chunkLength);
                dataStream.TrimBegin();

                if (msgStream.Length == hdr.MessageLength)
                {
                    Debug.WriteLine("Received message: {0} bytes (last chunk {2}), type {1}", hdr.MessageLength, hdr.MessageType, chunkLength);

                    // we've received complete message, parse it
                    incompleteMessageStreams.Remove(hdr.Timestamp);
                    msgStream.Seek(0, System.IO.SeekOrigin.Begin);
                    msg = RtmpMessage.Decode(hdr, msgStream);

                    // drop parsed data even if message decoding failed
                    // (which means we don't support something in this message)
                    msgStream.Dispose();
                }
            }

            return msg;
        }
    }
}
