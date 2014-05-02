namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;

    public class RtmpMessage
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
            long messageStart = dataStream.Position;

            switch (hdr.MessageType)
            {
                case RtmpMessageType.SetChunkSize:
                    {
                        if (hdr.ChunkStreamId != 2)
                        {
                            Global.Log.WarnFormat("Control message {0} received on wrong chunk stream {1}", hdr.MessageType, hdr.ChunkStreamId);
                        }

                        if (hdr.MessageStreamId != 0)
                        {
                            Global.Log.WarnFormat("Control message {0} received on wrong message stream {1}", hdr.MessageType, hdr.MessageStreamId);
                        }

                        using (EndianBinaryReader reader = new EndianBinaryReader(dataStream, true))
                        {
                            msg = new RtmpMessageSetChunkSize(reader.ReadUInt32());
                        }

                        break;
                    }

                case RtmpMessageType.Abort:
                    {
                        if (hdr.ChunkStreamId != 2)
                        {
                            Global.Log.WarnFormat("Control message {0} received on wrong chunk stream {1}", hdr.MessageType, hdr.ChunkStreamId);
                        }

                        if (hdr.MessageStreamId != 0)
                        {
                            Global.Log.WarnFormat("Control message {0} received on wrong message stream {1}", hdr.MessageType, hdr.MessageStreamId);
                        }

                        using (EndianBinaryReader reader = new EndianBinaryReader(dataStream, true))
                        {
                            msg = new RtmpMessageAbort(reader.ReadInt32());
                        }

                        break;
                    }

                case RtmpMessageType.Aknowledgement:
                    {
                        if (hdr.ChunkStreamId != 2)
                        {
                            Global.Log.WarnFormat("Control message {0} received on wrong chunk stream {1}", hdr.MessageType, hdr.ChunkStreamId);
                        }

                        if (hdr.MessageStreamId != 0)
                        {
                            Global.Log.WarnFormat("Control message {0} received on wrong message stream {1}", hdr.MessageType, hdr.MessageStreamId);
                        }

                        using (EndianBinaryReader reader = new EndianBinaryReader(dataStream, true))
                        {
                            msg = new RtmpMessageAck(reader.ReadUInt32());
                        }

                        break;
                    }

                case RtmpMessageType.UserControl:
                    {
                        if (hdr.ChunkStreamId != 2)
                        {
                            Global.Log.WarnFormat("Control message {0} received on wrong chunk stream {1}", hdr.MessageType, hdr.ChunkStreamId);
                        }

                        if (hdr.MessageStreamId != 0)
                        {
                            Global.Log.WarnFormat("Control message {0} received on wrong message stream {1}", hdr.MessageType, hdr.MessageStreamId);
                        }

                        msg = RtmpMessage.DecodeUserControl(dataStream);
                        break;
                    }

                case RtmpMessageType.WindowAknowledgementSize:
                    {
                        if (hdr.ChunkStreamId != 2)
                        {
                            Global.Log.WarnFormat("Control message {0} received on wrong chunk stream {1}", hdr.MessageType, hdr.ChunkStreamId);
                        }

                        if (hdr.MessageStreamId != 0)
                        {
                            Global.Log.WarnFormat("Control message {0} received on wrong message stream {1}", hdr.MessageType, hdr.MessageStreamId);
                        }

                        using (EndianBinaryReader reader = new EndianBinaryReader(dataStream, true))
                        {
                            msg = new RtmpMessageWindowAckSize(reader.ReadUInt32());
                        }

                        break;
                    }

                case RtmpMessageType.SetPeerBandwidth:
                    {
                        if (hdr.ChunkStreamId != 2)
                        {
                            Global.Log.WarnFormat("Control message {0} received on wrong chunk stream {1}", hdr.MessageType, hdr.ChunkStreamId);
                        }

                        if (hdr.MessageStreamId != 0)
                        {
                            Global.Log.WarnFormat("Control message {0} received on wrong message stream {1}", hdr.MessageType, hdr.MessageStreamId);
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
                            Global.Log.ErrorFormat("Audio data received on wrong message stream {0}", hdr.MessageStreamId);
                            break;
                        }

                        byte mediaHeader = (byte)dataStream.ReadByte();
                        RtmpAudioCodec audioCodec = (RtmpAudioCodec)((mediaHeader & 0xF0) >> 4);
                        int sampleRate = 8000;
                        switch ((mediaHeader & 0x0C) >> 2)
                        {
                            case 0:
                                sampleRate = 8000;
                                break;
                            case 1:
                                sampleRate = 11025;
                                break;
                            case 2:
                                sampleRate = 22050;
                                break;
                            case 3:
                                sampleRate = 44100;
                                break;
                        }
                        int sampleSize = ((mediaHeader & 0x02) == 0) ? 8 : 16;
                        int channels = ((mediaHeader & 0x01) == 0) ? 1 : 2;

                        RtmpMediaPacketType packetType = (RtmpMediaPacketType)dataStream.ReadByte();

                        RtmpMessageMedia msgMedia = new RtmpMessageMedia(audioCodec, packetType, sampleRate, sampleSize, channels);
                        if (dataStream.OneMessageStream)
                        {
                            msgMedia.MediaData = dataStream.FirstPacketBuffer;
                            msgMedia.MediaData.AddRef();
                        }
                        else
                        {
                            msgMedia.MediaData = Global.MediaAllocator.LockBuffer();
                            msgMedia.MediaData.ActualBufferSize = hdr.MessageLength;
                            dataStream.Seek(messageStart, System.IO.SeekOrigin.Begin);
                            dataStream.Read(msgMedia.MediaData.Buffer, 0, hdr.MessageLength);
                        }

                        msg = msgMedia;
                        break;
                    }

                case RtmpMessageType.Video:
                    {
                        if (hdr.MessageStreamId == 0)
                        {
                            Global.Log.ErrorFormat("Video data received on wrong message stream {0}", hdr.MessageStreamId);
                            break;
                        }

                        byte mediaHeader = (byte)dataStream.ReadByte();
                        bool keyFrame = ((mediaHeader & 0xF0) >> 4) == 1;
                        RtmpVideoCodec videoCodec = (RtmpVideoCodec)(mediaHeader & 0x0F);

                        RtmpMediaPacketType packetType = (RtmpMediaPacketType)dataStream.ReadByte();

                        int decoderDelay = 0;
                        using (EndianBinaryReader reader = new EndianBinaryReader(dataStream, true))
                        {
                            decoderDelay = reader.ReadInt32(3);
                        }

                        RtmpMessageMedia msgMedia = new RtmpMessageMedia(videoCodec, packetType, decoderDelay, keyFrame);
                        if (dataStream.OneMessageStream)
                        {
                            msgMedia.MediaData = dataStream.FirstPacketBuffer;
                            msgMedia.MediaData.AddRef();
                        }
                        else
                        {
                            msgMedia.MediaData = Global.MediaAllocator.LockBuffer();
                            msgMedia.MediaData.ActualBufferSize = hdr.MessageLength;
                            dataStream.Seek(messageStart, System.IO.SeekOrigin.Begin);
                            dataStream.Read(msgMedia.MediaData.Buffer, 0, hdr.MessageLength);
                        }

                        msg = msgMedia;
                        break;
                    }

                case RtmpMessageType.DataAmf0:
                    {
                        if (hdr.MessageStreamId == 0)
                        {
                            Global.Log.ErrorFormat("Metadata received on wrong message stream {0}", hdr.MessageStreamId);
                            break;
                        }

                        msg = RtmpMessage.DecodeAmf0(hdr, dataStream);
                        break;
                    }

                case RtmpMessageType.CommandAmf0:
                    {
                        msg = RtmpMessage.DecodeAmf0(hdr, dataStream);
                        break;
                    }

                default:
                    {
                        Global.Log.WarnFormat("Received unsupported message: type {0}, format {1}, chunk stream {2}, msg stream {3}, timestamp {4}, length {5}",
                            hdr.MessageType, hdr.Format, hdr.ChunkStreamId, hdr.MessageStreamId, hdr.Timestamp, hdr.MessageLength);
                        break;
                    }
            }

            if (msg != null)
            {
                msg.ChunkStreamId = hdr.ChunkStreamId;
                msg.MessageStreamId = hdr.MessageStreamId;
                msg.Timestamp = hdr.Timestamp;
            }
            else
            {
                // if we don't support the message or parsing failed then seek to the end of the message
                dataStream.Seek(messageStart + hdr.MessageLength, System.IO.SeekOrigin.Begin);
            }

            return msg;
        }

        private static RtmpMessage DecodeAmf0(RtmpChunkHeader hdr, PacketBufferStream dataStream)
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
                    Global.Log.ErrorFormat("AMF0 parser failed: {0}", ex.ToString());
                    return null;
                }
            }

            RtmpMessage msg = null;

            if (hdr.MessageType == RtmpMessageType.CommandAmf0)
            {
                if (pars.Count < 2 || pars[0].GetType() != typeof(string) || pars[1].GetType() != typeof(double))
                {
                    Global.Log.ErrorFormat("Unexpected command parameters: count {0}, par0 type {1}, par1 type {2}",
                        pars.Count, pars.Count > 0 ? pars[0].GetType().ToString() : "Missing", pars.Count > 1 ? pars[1].GetType().ToString() : "Missing");
                    return null;
                }

                msg = new RtmpMessageCommand((string)pars[0], (int)(double)pars[1], pars.GetRange(2, pars.Count - 2));
            }
            else if (hdr.MessageType == RtmpMessageType.DataAmf0)
            {
                if (pars.Count < 2 || pars[0].GetType() != typeof(string))
                {
                    Global.Log.ErrorFormat("Unexpected metadata parameters: count {0}, par0 type {1}", pars.Count, pars.Count > 0 ? pars[0].GetType().ToString() : "Missing");
                    return null;
                }

                msg = new RtmpMessageMetadata(pars);
            }

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
