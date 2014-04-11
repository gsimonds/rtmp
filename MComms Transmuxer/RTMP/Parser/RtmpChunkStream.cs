namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;

    class RtmpChunkStream
    {
        private uint chunkStreamId;
        private int chunkSize = Global.RtmpDefaultChunkSize;
        private long timestamp = -1;
        private long timestampDelta = -1;
        private int messageLength = -1;
        private RtmpMessageType messageType = RtmpMessageType.Undefined;
        private int messageStreamId = -1;

        private Dictionary<long, PacketBufferStream> incompleteMessageStreams = new Dictionary<long,PacketBufferStream>();

        public RtmpChunkStream(uint chunkStreamId)
        {
            this.chunkStreamId = chunkStreamId;
        }

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
                    this.timestampDelta = hdr.Timestamp;
                    this.timestamp += this.timestampDelta;
                    this.messageLength = hdr.MessageLength;
                    this.messageType = hdr.MessageType;
                    hdr.Timestamp = this.timestamp;
                    hdr.MessageStreamId = this.messageStreamId;
                    break;
                case 2:
                    this.timestampDelta = hdr.Timestamp;
                    this.timestamp += this.timestampDelta;
                    hdr.Timestamp = this.timestamp;
                    hdr.MessageLength = this.messageLength;
                    hdr.MessageType = this.messageType;
                    hdr.MessageStreamId = this.messageStreamId;
                    break;
                case 3:
                    this.timestamp += this.timestampDelta;
                    hdr.Timestamp = this.timestamp;
                    hdr.MessageLength = this.messageLength;
                    hdr.MessageType = this.messageType;
                    hdr.MessageStreamId = this.messageStreamId;
                    break;
            }

            RtmpMessage msg = null;

            if (hdr.MessageLength <= this.chunkSize)
            {
                // simplest case: the whole message in the chunk
                if (hdr.MessageLength > dataStream.Length - dataStream.Position)
                {
                    return null; // not enough data
                }

                msg = RtmpMessage.Decode(hdr, dataStream);
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

                int chunkLength = Math.Min((int)(hdr.MessageLength - msgStream.Length), this.chunkSize);
                if (chunkLength > dataStream.Length - dataStream.Position)
                {
                    return null; // not enough data
                }

                // append received chunk to previous ones
                dataStream.CopyTo(msgStream, chunkLength);

                if (msgStream.Length == hdr.MessageLength)
                {
                    // we've received complete message, parse it
                    incompleteMessageStreams.Remove(hdr.Timestamp);
                    msg = RtmpMessage.Decode(hdr, msgStream);
                }
            }

            if (msg != null)
            {
                // drop parsed data
                dataStream.TrimBegin();
            }

            return msg;
        }
    }
}
