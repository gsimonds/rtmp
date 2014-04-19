namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;

    public class RtmpMessageCommand : RtmpMessage
    {
        public RtmpMessageCommand(string commandName, int transactionId, List<object> parameters)
        {
            this.CommandName = commandName;
            this.TransactionId = transactionId;
            this.Parameters = parameters;

            switch (this.CommandName)
            {
                case "connect":
                    this.MessageType = RtmpIntMessageType.CommandNetConnectionConnect;
                    break;
                case "createStream":
                    this.MessageType = RtmpIntMessageType.CommandNetConnectionCreateStream;
                    break;
                case "releaseStream":
                    this.MessageType = RtmpIntMessageType.CommandNetConnectionReleaseStream;
                    break;
                case "FCPublish":
                    this.MessageType = RtmpIntMessageType.CommandNetConnectionFCPublish;
                    break;
                case "onFCPublish":
                    this.MessageType = RtmpIntMessageType.CommandNetConnectionOnFCPublish;
                    break;
                case "FCUnpublish":
                    this.MessageType = RtmpIntMessageType.CommandNetConnectionFCUnpublish;
                    break;
                case "deleteStream":
                    this.MessageType = RtmpIntMessageType.CommandNetStreamDeleteStream;
                    break;
                case "publish":
                    this.MessageType = RtmpIntMessageType.CommandNetStreamPublish;
                    break;
                case "onStatus":
                    this.MessageType = RtmpIntMessageType.CommandNetStreamOnStatus;
                    break;
                case "_result":
                    this.MessageType = RtmpIntMessageType.CommandResult;
                    break;
                case "_error":
                    this.MessageType = RtmpIntMessageType.CommandError;
                    break;
                default:
                    this.MessageType = RtmpIntMessageType.CommandUnsupported;
                    break;
            }
        }

        public string CommandName { get; set; }

        public int TransactionId { get; set; }

        public List<object> Parameters { get; set; }

        public override PacketBuffer ToPacketBuffer()
        {
            // TODO: add chunk size check
            RtmpChunkHeader hdr = new RtmpChunkHeader
            {
                Format = 0,
                Timestamp = this.Timestamp,
                ChunkStreamId = this.ChunkStreamId,
                MessageStreamId = this.MessageStreamId,
                MessageType = RtmpMessageType.CommandAmf0
            };

            int hdrSize = hdr.HeaderSize;
            int totalSize = 0;

            PacketBuffer packet = Global.Allocator.LockBuffer();
            // we'll set real value in the end
            packet.ActualBufferSize = packet.Size;

            using (EndianBinaryWriter writer = new EndianBinaryWriter(new PacketBufferStream(packet)))
            {
                // reserve space for chunk header
                writer.BaseStream.Seek(hdrSize, System.IO.SeekOrigin.Begin);

                // write AMF0 data
                this.WriteString(writer, this.CommandName);
                this.WriteNumber(writer, this.TransactionId);

                foreach (object obj in this.Parameters)
                {
                    Type objType = obj.GetType();
                    if (objType == typeof(double))
                    {
                        this.WriteNumber(writer, (double)obj);
                    }
                    else if (objType == typeof(bool))
                    {
                        this.WriteBoolean(writer, (bool)obj);
                    }
                    else if (objType == typeof(string))
                    {
                        this.WriteString(writer, (string)obj);
                    }
                    else if (objType == typeof(RtmpAmfNull))
                    {
                        this.WriteNull(writer);
                    }
                    else if (objType == typeof(RtmpAmfObject))
                    {
                        this.WriteObject(writer, (RtmpAmfObject)obj);
                    }
                }

                totalSize = (int)writer.BaseStream.Position;
            }

            hdr.MessageLength = totalSize - hdrSize;
            hdr.ToPacketBuffer(packet);
            packet.ActualBufferSize = totalSize;

            return packet;
        }

        private void WriteNumber(EndianBinaryWriter writer, double number)
        {
            writer.Write((byte)RtmpAmf0Types.Number);
            writer.Write(number);
        }

        private void WriteBoolean(EndianBinaryWriter writer, bool boolean)
        {
            writer.Write((byte)RtmpAmf0Types.Boolean);
            writer.Write(boolean);
        }

        private void WriteString(EndianBinaryWriter writer, string str, bool objectStart = false)
        {
            if (objectStart == false)
            {
                writer.Write((byte)RtmpAmf0Types.String);
            }

            byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(str);
            writer.Write((ushort)utf8.Length);
            writer.Write(utf8);
        }

        private void WriteNull(EndianBinaryWriter writer)
        {
            writer.Write((byte)RtmpAmf0Types.Null);
        }

        private void WriteObject(EndianBinaryWriter writer, RtmpAmfObject amfObject, bool isArray = false)
        {
            if (!isArray)
            {
                writer.Write((byte)RtmpAmf0Types.Object);
            }
            else
            {
                // TODO: test
                writer.Write((byte)RtmpAmf0Types.Array);
                writer.Write((int)(amfObject.Booleans.Count + amfObject.Numbers.Count + amfObject.Strings.Count + amfObject.Nulls));
            }

            foreach (var s in amfObject.Strings)
            {
                WriteString(writer, s.Key, true);
                WriteString(writer, s.Value);
            }

            foreach (var s in amfObject.Numbers)
            {
                WriteString(writer, s.Key, true);
                WriteNumber(writer, s.Value);
            }

            foreach (var s in amfObject.Booleans)
            {
                WriteString(writer, s.Key, true);
                WriteBoolean(writer, s.Value);
            }

            //objects end with 0x00,0x00, (oject end identifier [0x09 in this case])
            writer.Write((byte)0x00);
            writer.Write((byte)0x00);
            writer.Write((byte)RtmpAmf0Types.ObjectEnd);
        }
    }
}
