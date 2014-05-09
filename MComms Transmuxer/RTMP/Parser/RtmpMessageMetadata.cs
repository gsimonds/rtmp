namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;

    public class RtmpMessageMetadata : RtmpMessage
    {
        public RtmpMessageMetadata(List<object> parameters)
        {
            this.Parameters = parameters;
            this.MessageType = RtmpIntMessageType.DataUnsupported;
            this.OrigMessageType = RtmpMessageType.DataAmf0;

            if (this.Parameters.Count > 0 && this.Parameters[0].GetType() == typeof(string))
            {
                switch ((string)this.Parameters[0])
                {
                    case "@setDataFrame":
                        {
                            if (this.Parameters.Count >= 3 && this.Parameters[1].GetType() == typeof(string) && (string)this.Parameters[1] == "onMetaData" && this.Parameters[2].GetType() == typeof(RtmpAmfObject))
                            {
                                this.FirstDataIndex = 2;
                                this.MessageType = RtmpIntMessageType.DataMetadata;
                            }
                            break;
                        }

                    case "onMetaData":
                        {
                            if (this.Parameters.Count >= 2 && this.Parameters[1].GetType() == typeof(RtmpAmfObject))
                            {
                                this.FirstDataIndex = 1;
                                this.MessageType = RtmpIntMessageType.DataMetadata;
                            }
                            break;
                        }

                    case "onFI":
                        {
                            if (this.Parameters.Count >= 2 && this.Parameters[1].GetType() == typeof(RtmpAmfObject))
                            {
                                this.FirstDataIndex = 1;
                                this.MessageType = RtmpIntMessageType.DataTimestamp;
                            }
                            break;
                        }
                }
            }
        }

        public List<object> Parameters { get; set; }

        public int FirstDataIndex { get; set; }

        public override PacketBuffer ToRtmpChunk()
        {
            RtmpChunkHeader hdr = new RtmpChunkHeader
            {
                Format = 0,
                Timestamp = this.Timestamp,
                ChunkStreamId = this.ChunkStreamId,
                MessageStreamId = this.MessageStreamId,
                MessageType = RtmpMessageType.DataAmf0
            };

            int hdrSize = hdr.HeaderSize;
            int totalSize = 0;

            PacketBuffer packet = this.createBody(hdrSize, ref totalSize);

            hdr.MessageLength = totalSize - hdrSize;
            hdr.ToPacketBuffer(packet);
            packet.ActualBufferSize = totalSize;

            return packet;
        }

        public override PacketBuffer ToFlvTag()
        {
            FlvTagHeader hdr = new FlvTagHeader
            {
                TagType = RtmpMessageType.DataAmf0,
                StreamId = 0,
                Timestamp = (uint)this.Timestamp
            };

            int hdrSize = hdr.HeaderSize;
            int totalSize = 0;

            PacketBuffer packet = this.createBody(hdrSize, ref totalSize);

            hdr.DataSize = (uint)(totalSize - hdrSize);
            hdr.ToPacketBuffer(packet);
            packet.ActualBufferSize = totalSize;

            return packet;
        }

        private PacketBuffer createBody(int hdrSize, ref int totalSize)
        {
            PacketBuffer packet = Global.Allocator.LockBuffer();
            // we'll set real value in the end
            packet.ActualBufferSize = packet.Size;

            using (EndianBinaryWriter writer = new EndianBinaryWriter(new PacketBufferStream(packet)))
            {
                // reserve space for chunk header
                writer.BaseStream.Seek(hdrSize, System.IO.SeekOrigin.Begin);

                // write AMF0 data
                int startIndex = 0;
                if (this.Parameters.Count > 0 && this.Parameters[0].GetType() == typeof(string) && (string)this.Parameters[0] == "@setDataFrame")
                {
                    startIndex = 1;
                }
                writer.WriteAmf0(this.Parameters, startIndex);

                totalSize = (int)writer.BaseStream.Position;
            }

            return packet;
        }
    }
}
