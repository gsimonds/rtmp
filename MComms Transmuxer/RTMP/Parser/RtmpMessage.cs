namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;

    class RtmpMessage
    {
        public RtmpMessage()
        {
        }

        public RtmpIntMessageType MessageType { get; set; }

        public int MessageStreamId { get; set; }

        public int Timestamp { get; set; }

        public virtual PacketBuffer ToPacketBuffer()
        {
            return null;
        }

        public static RtmpMessage Decode(RtmpChunkHeader hdr, PacketBufferStream dataStream)
        {
            RtmpMessage msg = null;

            switch (hdr.MessageType)
            {
                case RtmpMessageType.SetChunkSize:
                    {
                        if (hdr.ChunkStreamId == 2)
                        {
                            // TODO: log warning
                        }

                        if (hdr.MessageStreamId != 0)
                        {
                            // TODO: log warning
                        }

                        break;
                    }

                case RtmpMessageType.Abort:
                    {
                        if (hdr.ChunkStreamId == 2)
                        {
                            // TODO: log warning
                        }

                        if (hdr.MessageStreamId != 0)
                        {
                            // TODO: log warning
                        }

                        break;
                    }

                case RtmpMessageType.Aknowledgement:
                    {
                        if (hdr.ChunkStreamId == 2)
                        {
                            // TODO: log warning
                        }

                        if (hdr.MessageStreamId != 0)
                        {
                            // TODO: log warning
                        }

                        break;
                    }

                case RtmpMessageType.UserControl:
                    {
                        if (hdr.ChunkStreamId == 2)
                        {
                            // TODO: log warning
                        }

                        if (hdr.MessageStreamId != 0)
                        {
                            // TODO: log warning
                        }

                        break;
                    }

                case RtmpMessageType.WindowAknowledgementSize:
                    {
                        if (hdr.ChunkStreamId == 2)
                        {
                            // TODO: log warning
                        }

                        if (hdr.MessageStreamId != 0)
                        {
                            // TODO: log warning
                        }

                        break;
                    }

                case RtmpMessageType.SetPeerBandwidth:
                    {
                        if (hdr.ChunkStreamId == 2)
                        {
                            // TODO: log warning
                        }

                        if (hdr.MessageStreamId != 0)
                        {
                            // TODO: log warning
                        }

                        break;
                    }

                case RtmpMessageType.Audio:
                    {
                        break;
                    }

                case RtmpMessageType.Video:
                    {
                        break;
                    }

                case RtmpMessageType.Data:
                    {
                        break;
                    }

                case RtmpMessageType.SharedObject:
                    {
                        break;
                    }

                case RtmpMessageType.Command:
                    {
                        break;
                    }

                case RtmpMessageType.Aggregate:
                    {
                        break;
                    }

                default:
                    // TODO: what to do?
                    break;
            }

            return msg;
        }
    }
}
