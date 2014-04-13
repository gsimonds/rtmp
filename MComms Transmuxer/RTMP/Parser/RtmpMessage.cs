namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;

    class RtmpMessage
    {
        public RtmpMessage()
        {
        }

        public uint ChunkStreamId { get; set; }

        public RtmpIntMessageType MessageType { get; set; }

        public int MessageStreamId { get; set; }

        public long Timestamp { get; set; }

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

                        using (EndianBinaryReader reader = new EndianBinaryReader(dataStream, true))
                        {
                            msg = new RtmpMessageSetChunkSize(reader.ReadUInt32());
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

                        using (EndianBinaryReader reader = new EndianBinaryReader(dataStream, true))
                        {
                            msg = new RtmpMessageAbort(reader.ReadInt32());
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

                        using (EndianBinaryReader reader = new EndianBinaryReader(dataStream, true))
                        {
                            msg = new RtmpMessageAck(reader.ReadUInt32());
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

                        msg = RtmpMessage.DecodeUserControl(dataStream);
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

                        using (EndianBinaryReader reader = new EndianBinaryReader(dataStream, true))
                        {
                            msg = new RtmpMessageWindowAckSize(reader.ReadUInt32());
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

                        using (EndianBinaryReader reader = new EndianBinaryReader(dataStream, true))
                        {
                            msg = new RtmpMessageSetPeerBandwidth(reader.ReadUInt32(), (RtmpMessageSetPeerBandwidth.LimitTypes)reader.ReadByte());
                        }

                        break;
                    }

                case RtmpMessageType.Audio:
                    {
                        if (hdr.MessageStreamId == 0)
                        {
                            // TODO: log error
                            break;
                        }

                        // TODO: implement
                        Debug.WriteLine("Received audio: {0} bytes, timestamp {1}", hdr.MessageLength, hdr.Timestamp);
                        dataStream.Seek(hdr.MessageLength, System.IO.SeekOrigin.Current);
                        break;
                    }

                case RtmpMessageType.Video:
                    {
                        if (hdr.MessageStreamId == 0)
                        {
                            // TODO: log error
                            break;
                        }

                        // TODO: implement
                        Debug.WriteLine("Received video: {0} bytes, timestamp {1}", hdr.MessageLength, hdr.Timestamp);
                        dataStream.Seek(hdr.MessageLength, System.IO.SeekOrigin.Current);
                        break;
                    }

                case RtmpMessageType.Data:
                    {
                        if (hdr.MessageStreamId == 0)
                        {
                            // TODO: log error
                            break;
                        }

                        // TODO: implement
                        Debug.WriteLine("Received metadata: {0} bytes, timestamp {1}", hdr.MessageLength, hdr.Timestamp);
                        dataStream.Seek(hdr.MessageLength, System.IO.SeekOrigin.Current);
                        break;
                    }

                case RtmpMessageType.SharedObject:
                    {
                        // TODO: log warning
                        int n = 1;
                        break;
                    }

                case RtmpMessageType.Command:
                    {
                        msg = RtmpMessage.DecodeCommand(hdr, dataStream);
                        break;
                    }

                case RtmpMessageType.Aggregate:
                    {
                        // TODO: log warning
                        int n = 1;
                        break;
                    }

                default:
                    {
                        // TODO: log warning
                        int n = 1;
                        break;
                    }
            }

            if (msg != null)
            {
                msg.ChunkStreamId = hdr.ChunkStreamId;
                msg.MessageStreamId = hdr.MessageStreamId;
                msg.Timestamp = hdr.Timestamp;
            }

            return msg;
        }

        private static RtmpMessageCommand DecodeCommand(RtmpChunkHeader hdr, PacketBufferStream dataStream)
        {
            List<object> pars = new List<object>();
            long startPosition = dataStream.Position;

            using (EndianBinaryReader reader = new EndianBinaryReader(dataStream, true))
            {
                try
                {
                    Stack<RtmpAmfObject> objectStack = new Stack<RtmpAmfObject>();

                    while (dataStream.Position < startPosition + hdr.MessageLength)
                    {
                        if (objectStack.Count != 0)
                        {
                            var count = reader.ReadUInt16();
                            var propString = "";
                            for (var i = 0; i < count; i++)
                            {
                                propString += (char)reader.ReadByte();
                            }
                            objectStack.Peek().CurrentProperty = propString;
                        }

                        var type = (RtmpAmf0Types)reader.ReadByte();

                        switch (type)
                        {
                            case RtmpAmf0Types.Number:
                                {
                                    double value = reader.ReadDouble();
                                    if (objectStack.Count != 0)
                                    {
                                        objectStack.Peek().Numbers.Add(objectStack.Peek().CurrentProperty, value);
                                    }
                                    else
                                    {
                                        pars.Add(value);
                                    }

                                    break;
                                }

                            case RtmpAmf0Types.Boolean:
                                {
                                    bool value = reader.ReadBoolean();
                                    if (objectStack.Count != 0)
                                    {
                                        objectStack.Peek().Booleans.Add(objectStack.Peek().CurrentProperty, value);
                                    }
                                    else
                                    {
                                        pars.Add(value);
                                    }

                                    break;
                                }

                            case RtmpAmf0Types.String:
                                {
                                    ushort count = reader.ReadUInt16();
                                    byte[] utf8 = reader.ReadBytes(count);
                                    string pushString = System.Text.Encoding.UTF8.GetString(utf8);

                                    if (objectStack.Count != 0)
                                    {
                                        objectStack.Peek().Strings.Add(objectStack.Peek().CurrentProperty, pushString);
                                    }
                                    else
                                    {
                                        pars.Add(pushString);
                                    }

                                    break;
                                }

                            case RtmpAmf0Types.Null:
                                {
                                    if (objectStack.Count != 0)
                                    {
                                        objectStack.Peek().Nulls++;
                                    }
                                    else
                                    {
                                        pars.Add(new RtmpAmfNull());
                                    }

                                    break;
                                }

                            case RtmpAmf0Types.Object:
                            case RtmpAmf0Types.Array:
                                {
                                    if (type == RtmpAmf0Types.Array)
                                    {
                                        var arrayLength = reader.ReadInt32();
                                    }

                                    var objectAdd = new RtmpAmfObject();
                                    objectStack.Push(objectAdd);

                                    break;
                                }

                            case RtmpAmf0Types.ObjectEnd:
                                {
                                    if (objectStack.Count == 1)
                                    {
                                        pars.Add(objectStack.Pop());
                                    }
                                    else if (objectStack.Count > 1)
                                    {
                                        var mostRecentObject = objectStack.Pop();
                                        objectStack.Peek().Objects.Add(objectStack.Peek().CurrentProperty, mostRecentObject);
                                    }

                                    break;
                                }

                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }
                catch (Exception ex)
                {
                    // unsupported format
                    // TODO: log error
                    dataStream.Seek(startPosition + hdr.MessageLength, System.IO.SeekOrigin.Begin);
                    return null;
                }
            }

            if (pars.Count < 2 || pars[0].GetType() != typeof(string) || pars[1].GetType() != typeof(double))
            {
                // TODO: unexpected, log error
                return null;
            }

            RtmpMessageCommand msg = new RtmpMessageCommand((string)pars[0], (int)(double)pars[1], pars.GetRange(2, pars.Count - 2));
            Debug.WriteLine("Received command: " + msg.CommandName);
            return msg;
        }

        private static RtmpMessageUserControl DecodeUserControl(PacketBufferStream dataStream)
        {
            RtmpMessageUserControl msg = null;

            using (EndianBinaryReader reader = new EndianBinaryReader(dataStream, true))
            {
                RtmpMessageUserControl.EventTypes eventType = (RtmpMessageUserControl.EventTypes)reader.ReadUInt16();

                switch (eventType)
                {
                    case RtmpMessageUserControl.EventTypes.StreamBegin:
                    case RtmpMessageUserControl.EventTypes.StreamEOF:
                    case RtmpMessageUserControl.EventTypes.StreamDry:
                    case RtmpMessageUserControl.EventTypes.StreamIsRecorded:
                        {
                            int targetMessageStreamId = reader.ReadInt32();
                            msg = new RtmpMessageUserControl(eventType, targetMessageStreamId);
                            break;
                        }

                    case RtmpMessageUserControl.EventTypes.SetBufferLength:
                        {
                            int targetMessageStreamId = reader.ReadInt32();
                            uint bufferLength = reader.ReadUInt32();
                            msg = new RtmpMessageUserControl(eventType, targetMessageStreamId, bufferLength);
                            break;
                        }

                    case RtmpMessageUserControl.EventTypes.PingRequest:
                    case RtmpMessageUserControl.EventTypes.PingResponse:
                        {
                            uint timestamp = reader.ReadUInt32();
                            msg = new RtmpMessageUserControl(eventType, timestamp);
                            break;
                        }
                }
            }

            return msg;
        }
    }
}
