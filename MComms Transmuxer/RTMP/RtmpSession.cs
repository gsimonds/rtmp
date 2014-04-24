namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;
    using MComms_Transmuxer.SmoothStreaming;
    using MComms_Transmuxer.Transport;

    public class RtmpSession : IDisposable
    {
        private long sessionId = 0;
        private SocketTransport transport = null;
        private IPEndPoint sessionEndPoint = null;
        private volatile bool isRunning = true;
        private RtmpSessionState state = RtmpSessionState.Uninitialized;
        private RtmpProtocolParser parser = new RtmpProtocolParser();
        private Thread sessionThread = null;
        private Queue<PacketBuffer> receivedPackets = new Queue<PacketBuffer>();
        private RtmpHandshake handshakeS1 = null;
        private int messageStreamCounter = 1;
        private ulong receivedSize = 0;
        private ulong sentSize = 0;
        private ulong lastReportedReceivedSize = 0;
        private ulong lastReportedSentSize = 0;
        private uint receiveAckWindowSize = Global.RtmpDefaultAckWindowSize;
        private uint sendAckWindowSize = Global.RtmpDefaultAckWindowSize;
        private Dictionary<int, RtmpMessageStream> messageStreams = new Dictionary<int, RtmpMessageStream>();
        private IntPtr mediaDataPtr;

        public RtmpSession(long sessionId, SocketTransport transport, IPEndPoint sessionEndPoint)
        {
            this.sessionId = sessionId;
            this.transport = transport;
            this.sessionEndPoint = sessionEndPoint;
            this.mediaDataPtr = Marshal.AllocHGlobal(Global.MediaAllocator.BufferSize);
            this.sessionThread = new Thread(this.SessionThread);
            this.sessionThread.Start();
            Global.Log.DebugFormat("End point {0}, id {1}: created session object", this.sessionEndPoint, this.sessionId);
        }

        public void Dispose()
        {
            this.isRunning = false;
            this.sessionThread.Join();
            Marshal.FreeHGlobal(this.mediaDataPtr);
            Global.Log.DebugFormat("End point {0}, id {1}: session object disposed", this.sessionEndPoint, this.sessionId);
        }

        public void OnReceive(PacketBuffer packet)
        {
            lock (this.receivedPackets)
            {
                packet.AddRef();
                this.receivedPackets.Enqueue(packet);
            }
        }

        private void SessionThread()
        {
            Global.Log.InfoFormat("End point {0}, id {1}: session thread started...", this.sessionEndPoint, this.sessionId);

            while (this.isRunning)
            {
                PacketBuffer packet = null;
                bool nothingToDo = true;

                lock (this.receivedPackets)
                {
                    if (this.receivedPackets.Count > 0)
                    {
                        nothingToDo = false;
                        packet = this.receivedPackets.Dequeue();
                        receivedSize += (ulong)packet.ActualBufferSize;
                    }
                }

                try
                {
                    RtmpMessage msg = null;
                    while ((msg = parser.Decode(packet)) != null)
                    {
                        nothingToDo = false;
                        this.ProcessMessage(msg);
                        if (packet != null)
                        {
                            packet.Release();
                            packet = null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // something went wrong, drop the session
                    if (packet != null)
                    {
                        packet.Release();
                        packet = null;
                    }

                    Global.Log.ErrorFormat("Decode exception {0}, dropping session...", ex.ToString());
                    this.transport.Disconnect(this.sessionEndPoint);
                    break;
                }

                if (packet != null)
                {
                    packet.Release();
                    packet = null;
                }

                if (this.receiveAckWindowSize > 0)
                {
                    if ((this.receivedSize - this.lastReportedReceivedSize) >= this.receiveAckWindowSize)
                    {
                        this.parser.Encode(new RtmpMessageAck((uint)(this.receivedSize - this.lastReportedReceivedSize)));
                        this.lastReportedReceivedSize = this.receivedSize;
                    }
                }

                // send packets if any
                while ((packet = this.parser.GetSendPacket()) != null)
                {
                    nothingToDo = false;
                    this.transport.Send(this.sessionEndPoint, packet);

                    sentSize += (uint)packet.ActualBufferSize;
                    if (sentSize > uint.MaxValue)
                    {
                        sentSize -= uint.MaxValue;
                    }

                    packet.Release();
                }

                if (nothingToDo)
                {
                    // sleep only if we don't have anything to do
                    Thread.Sleep(1);
                }
            }

            Global.Log.InfoFormat("End point {0}, id {1}: session thread finished...", this.sessionEndPoint, this.sessionId);
        }

        private void ProcessMessage(RtmpMessage msg)
        {
            switch (msg.MessageType)
            {
                case RtmpIntMessageType.HandshakeC0:
                    {
                        if (this.state != RtmpSessionState.Uninitialized)
                        {
                            // wrong handshake sequence
                            this.transport.Disconnect(this.sessionEndPoint);
                            break;
                        }

                        RtmpHandshake handshake = (RtmpHandshake)msg;
                        if (handshake.Version == Global.RtmpVersion)
                        {
                            this.state = RtmpSessionState.HanshakeVersionSent;
                            this.parser.State = RtmpSessionState.HanshakeVersionSent;
                            // push S0 & S1 to parser
                            this.parser.Encode(RtmpHandshake.GenerateS0());
                            this.handshakeS1 = RtmpHandshake.GenerateS1();
                            this.parser.Encode(this.handshakeS1);
                        }
                        else
                        {
                            // unsupported protocol version
                            this.transport.Disconnect(this.sessionEndPoint);
                        }

                        break;
                    }

                case RtmpIntMessageType.HandshakeC1:
                    {
                        if (this.state != RtmpSessionState.HanshakeVersionSent)
                        {
                            // wrong handshake sequence
                            this.transport.Disconnect(this.sessionEndPoint);
                            break;
                        }

                        RtmpHandshake handshakeC1 = (RtmpHandshake)msg;
                        RtmpHandshake handshakeS2 = handshakeC1.GenerateS2();
                        this.state = RtmpSessionState.HanshakeAckSent;
                        this.parser.State = RtmpSessionState.HanshakeAckSent;
                        this.parser.Encode(handshakeS2);
                        break;
                    }

                case RtmpIntMessageType.HandshakeC2:
                    {
                        if (this.state != RtmpSessionState.HanshakeAckSent)
                        {
                            // wrong handshake sequence
                            this.transport.Disconnect(this.sessionEndPoint);
                            break;
                        }

                        RtmpHandshake handshakeC2 = (RtmpHandshake)msg;
                        if (handshakeC2.ValidateC2(this.handshakeS1))
                        {
                            this.state = RtmpSessionState.Receiving;
                            this.parser.State = RtmpSessionState.Receiving;
                            // register control message stream
                            this.parser.RegisterMessageStream(0);
                        }
                        else
                        {
                            // handshake validation failed
                            this.transport.Disconnect(this.sessionEndPoint);
                        }

                        break;
                    }

                case RtmpIntMessageType.ProtoControlSetChunkSize:
                    {
                        RtmpMessageSetChunkSize recvCtrl = (RtmpMessageSetChunkSize)msg;
                        this.parser.ChunkSize = (int)recvCtrl.ChunkSize;
                        Global.Log.DebugFormat("Received {0}, new chunk size {1}", msg.MessageType, recvCtrl.ChunkSize);
                        break;
                    }

                case RtmpIntMessageType.ProtoControlAbort:
                    {
                        RtmpMessageAbort recvCtrl = (RtmpMessageAbort)msg;
                        this.parser.Abort((uint)recvCtrl.TargetChunkStreamId);
                        Global.Log.DebugFormat("Received {0}, aborted chunk stream {1}", msg.MessageType, recvCtrl.TargetChunkStreamId);
                        break;
                    }

                case RtmpIntMessageType.ProtoControlAknowledgement:
                    {
                        RtmpMessageAck recvCtrl = (RtmpMessageAck)msg;
                        this.lastReportedSentSize = recvCtrl.ReceivedBytes;
                        Global.Log.DebugFormat("Received {0}, reported size {1}, actually sent {2}", msg.MessageType, recvCtrl.ReceivedBytes, this.sentSize);
                        // TODO: limit sending???
                        break;
                    }

                case RtmpIntMessageType.ProtoControlUserControl:
                    {
                        RtmpMessageUserControl recvCtrl = (RtmpMessageUserControl)msg;
                        switch (recvCtrl.EventType)
                        {
                            case RtmpMessageUserControl.EventTypes.SetBufferLength:
                                Global.Log.DebugFormat("Received {0}, event {1}, message stream id {2}, buffer length {3}", msg.MessageType, recvCtrl.EventType, recvCtrl.TargetMessageStreamId, recvCtrl.BufferLength);
                                break;
                            case RtmpMessageUserControl.EventTypes.PingRequest:
                            case RtmpMessageUserControl.EventTypes.PingResponse:
                                Global.Log.DebugFormat("Received {0}, event {1}, timestamp {2}", msg.MessageType, recvCtrl.EventType, recvCtrl.Timestamp);
                                break;
                            default:
                                Global.Log.DebugFormat("Received {0}, event {1}, message stream id {2}", msg.MessageType, recvCtrl.EventType, recvCtrl.TargetMessageStreamId);
                                break;
                        }
                        // TODO: support ping request?
                        break;
                    }

                case RtmpIntMessageType.ProtoControlWindowAknowledgementSize:
                    {
                        RtmpMessageWindowAckSize recvCtrl = (RtmpMessageWindowAckSize)msg;
                        this.receiveAckWindowSize = recvCtrl.AckSize;
                        Global.Log.DebugFormat("Received {0}, new recv window size {1}", msg.MessageType, recvCtrl.AckSize);
                        break;
                    }

                case RtmpIntMessageType.ProtoControlSetPeerBandwidth:
                    {
                        RtmpMessageSetPeerBandwidth recvCtrl = (RtmpMessageSetPeerBandwidth)msg;
                        if (this.sendAckWindowSize != recvCtrl.AckSize)
                        {
                            this.sendAckWindowSize = recvCtrl.AckSize;
                            this.parser.Encode(new RtmpMessageWindowAckSize(this.sendAckWindowSize));
                        }
                        Global.Log.DebugFormat("Received {0}, new send window size {1}", msg.MessageType, recvCtrl.AckSize);
                        break;
                    }

                case RtmpIntMessageType.CommandNetConnectionConnect:
                    {
                        if (this.state != RtmpSessionState.Receiving)
                        {
                            // wrong state
                            Global.Log.ErrorFormat("Command {0}, wrong state {1}, dropping session...", msg.MessageType, this.state);
                            this.transport.Disconnect(this.sessionEndPoint);
                            break;
                        }

                        RtmpMessageCommand recvComm = (RtmpMessageCommand)msg;
                        if (recvComm.Parameters.Count == 0 || recvComm.Parameters[0].GetType() != typeof(RtmpAmfObject))
                        {
                            // wrong command parameters
                            this.transport.Disconnect(this.sessionEndPoint);
                            break;
                        }

                        RtmpAmfObject par = recvComm.Parameters[0] as RtmpAmfObject;

                        // accept live streams only
                        if (!par.Strings.ContainsKey("app") || par.Strings["app"] != "live")
                        {
                            // wrong app
                            this.transport.Disconnect(this.sessionEndPoint);
                            break;
                        }

                        // prepare reply
                        Global.Log.DebugFormat("Received command {0}", msg.MessageType);

                        // send window acknowledgement size
                        this.parser.Encode(new RtmpMessageWindowAckSize(this.sendAckWindowSize));

                        // send peer bandwidth
                        this.parser.Encode(new RtmpMessageSetPeerBandwidth(this.receiveAckWindowSize, RtmpMessageSetPeerBandwidth.LimitTypes.Dynamic));

                        // send user control event "Stream 0 Begins"
                        this.parser.Encode(new RtmpMessageUserControl(RtmpMessageUserControl.EventTypes.StreamBegin, 0));

                        // set sunk size to 1024 bytes
                        this.parser.Encode(new RtmpMessageSetChunkSize(1024));
                        //this.parser.ChunkSize = 1024;

                        List<object> pars = new List<object>();

                        RtmpAmfObject amf = new RtmpAmfObject();
                        amf.Strings.Add("fmsVer", "FMS/3,0,1,123"); // NOTE: value taken from FFmpeg implementation
                        amf.Numbers.Add("capabilities", 31); // NOTE: value taken from FFmpeg implementation
                        amf.Numbers.Add("mode", 1); // TODO: adjust
                        pars.Add(amf);

                        amf = new RtmpAmfObject();
                        amf.Strings.Add("level", "status");
                        amf.Strings.Add("code", "NetConnection.Connect.Success");
                        amf.Strings.Add("description", "Connection succeeded.");
                        // TODO: "data" array?
                        amf.Numbers.Add("clientId", this.sessionId);
                        amf.Numbers.Add("objectEncoding", 0); // AMF0
                        pars.Add(amf);

                        RtmpMessageCommand sendComm = new RtmpMessageCommand("_result", 1, pars);
                        sendComm.ChunkStreamId = recvComm.ChunkStreamId;
                        sendComm.MessageStreamId = recvComm.MessageStreamId;

                        this.parser.Encode(sendComm);

                        break;
                    }

                case RtmpIntMessageType.CommandNetConnectionReleaseStream:
                    {
                        if (this.state != RtmpSessionState.Receiving)
                        {
                            // wrong state
                            Global.Log.ErrorFormat("Command {0}, wrong state {1}, dropping session...", msg.MessageType, this.state);
                            this.transport.Disconnect(this.sessionEndPoint);
                            break;
                        }

                        // TODO: implement
                        Global.Log.DebugFormat("Received command {0}", msg.MessageType);
                        break;
                    }

                case RtmpIntMessageType.CommandNetConnectionFCPublish:
                    {
                        if (this.state != RtmpSessionState.Receiving)
                        {
                            // wrong state
                            Global.Log.ErrorFormat("Command {0}, wrong state {1}, dropping session...", msg.MessageType, this.state);
                            this.transport.Disconnect(this.sessionEndPoint);
                            break;
                        }

                        RtmpMessageCommand recvComm = (RtmpMessageCommand)msg;

                        string publishStreamName = string.Empty;
                        if (recvComm.Parameters.Count >= 2)
                        {
                            if (recvComm.Parameters[1].GetType() == typeof(string))
                            {
                                publishStreamName = (string)recvComm.Parameters[1];
                            }
                        }

                        Global.Log.DebugFormat("Received command {0}", msg.MessageType);

                        List<object> pars = new List<object>();

                        pars.Add(new RtmpAmfNull());

                        RtmpAmfObject amf = new RtmpAmfObject();
                        amf.Strings.Add("level", "status");
                        amf.Strings.Add("code", "NetStream.Publish.Start");
                        amf.Strings.Add("description", "FCPublish to stream " + publishStreamName);
                        amf.Numbers.Add("clientId", this.sessionId);
                        pars.Add(amf);

                        RtmpMessageCommand sendComm = new RtmpMessageCommand("onFCPublish", 0, pars);
                        sendComm.ChunkStreamId = recvComm.ChunkStreamId;
                        sendComm.MessageStreamId = recvComm.MessageStreamId;

                        this.parser.Encode(sendComm);

                        break;
                    }

                case RtmpIntMessageType.CommandNetConnectionCreateStream:
                    {
                        if (this.state != RtmpSessionState.Receiving)
                        {
                            // wrong state
                            Global.Log.ErrorFormat("Command {0}, wrong state {1}, dropping session...", msg.MessageType, this.state);
                            this.transport.Disconnect(this.sessionEndPoint);
                            break;
                        }

                        RtmpMessageCommand recvComm = (RtmpMessageCommand)msg;

                        Global.Log.DebugFormat("Received command {0}", msg.MessageType);

                        // register new message stream
                        this.parser.RegisterMessageStream(this.messageStreamCounter);
                        this.messageStreams.Add(this.messageStreamCounter, new RtmpMessageStream(this.messageStreamCounter));

                        List<object> pars = new List<object>();
                        pars.Add(new RtmpAmfNull());
                        pars.Add((double)this.messageStreamCounter++);

                        RtmpMessageCommand sendComm = new RtmpMessageCommand("_result", recvComm.TransactionId, pars);
                        sendComm.ChunkStreamId = recvComm.ChunkStreamId;
                        sendComm.MessageStreamId = recvComm.MessageStreamId;

                        this.parser.Encode(sendComm);

                        break;
                    }

                case RtmpIntMessageType.CommandNetStreamPublish:
                    {
                        if (this.state != RtmpSessionState.Receiving)
                        {
                            // wrong state
                            Global.Log.ErrorFormat("Command {0}, wrong state {1}, dropping session...", msg.MessageType, this.state);
                            this.transport.Disconnect(this.sessionEndPoint);
                            break;
                        }

                        RtmpMessageStream messageStream = null;
                        if (this.messageStreams.ContainsKey(msg.MessageStreamId))
                        {
                            messageStream = this.messageStreams[msg.MessageStreamId];
                        }

                        if (messageStream == null)
                        {
                            // wrong publish sequence
                            Global.Log.ErrorFormat("Command {0}, unregistered message stream {1}, dropping session...", msg.MessageType, msg.MessageStreamId);
                            this.transport.Disconnect(this.sessionEndPoint);
                            break;
                        }

                        RtmpMessageCommand recvComm = (RtmpMessageCommand)msg;
                        if (recvComm.Parameters.Count < 3 ||
                            recvComm.Parameters[1].GetType() != typeof(string) ||
                            recvComm.Parameters[2].GetType() != typeof(string))
                        {
                            // wrong command parameters
                            Global.Log.ErrorFormat("Command {0}, corrupted parameters, dropping session...", msg.MessageType);
                            this.transport.Disconnect(this.sessionEndPoint);
                            break;
                        }

                        string publishType = (string)recvComm.Parameters[2];

                        // accept live streams only
                        if (publishType != "live")
                        {
                            // wrong app
                            Global.Log.ErrorFormat("Command {0}, unsupported publish type {1}, dropping session...", msg.MessageType, publishType);
                            this.transport.Disconnect(this.sessionEndPoint);
                            break;
                        }

                        // TODO: parse publishStreamName: query string, totalDatarate parameter
                        messageStream.PublishName = (string)recvComm.Parameters[1];

                        // prepare reply
                        Global.Log.DebugFormat("Received command {0}", msg.MessageType);

                        // send user control event "Stream N Begins"
                        this.parser.Encode(new RtmpMessageUserControl(RtmpMessageUserControl.EventTypes.StreamBegin, recvComm.MessageStreamId));

                        List<object> pars = new List<object>();

                        pars.Add(new RtmpAmfNull());

                        RtmpAmfObject amf = new RtmpAmfObject();
                        amf.Strings.Add("level", "status");
                        amf.Strings.Add("code", "NetStream.Publish.Start");
                        amf.Strings.Add("description", "Publishing " + messageStream.PublishName);
                        amf.Numbers.Add("clientId", this.sessionId);
                        pars.Add(amf);

                        RtmpMessageCommand sendComm = new RtmpMessageCommand("onStatus", 0, pars);
                        sendComm.ChunkStreamId = recvComm.ChunkStreamId;
                        sendComm.MessageStreamId = recvComm.MessageStreamId;

                        this.parser.Encode(sendComm);

                        break;
                    }

                case RtmpIntMessageType.Data:
                    {
                        if (this.state != RtmpSessionState.Receiving)
                        {
                            // wrong state
                            Global.Log.ErrorFormat("Command {0}, wrong state {1}, dropping session...", msg.MessageType, this.state);
                            this.transport.Disconnect(this.sessionEndPoint);
                            break;
                        }

                        RtmpMessageStream messageStream = null;
                        if (this.messageStreams.ContainsKey(msg.MessageStreamId))
                        {
                            messageStream = this.messageStreams[msg.MessageStreamId];
                        }

                        if (messageStream == null)
                        {
                            // unexpected message stream
                            Global.Log.ErrorFormat("Command {0}, unregistered message stream {1}, dropping session...", msg.MessageType, msg.MessageStreamId);
                            this.transport.Disconnect(this.sessionEndPoint);
                            break;
                        }

                        Global.Log.DebugFormat("Received command {0}", msg.MessageType);

                        RtmpMessageMetadata recvComm = (RtmpMessageMetadata)msg;

                        RtmpAmfObject metadata = null;
                        if (recvComm.Parameters.Count >= 2)
                        {
                            int startIndex = recvComm.Parameters.Count;

                            if ((string)recvComm.Parameters[0] == "@setDataFrame")
                            {
                                if (recvComm.Parameters.Count >= 3 && (string)recvComm.Parameters[1] == "onMetaData")
                                {
                                    startIndex = 2;
                                }
                            }
                            else if ((string)recvComm.Parameters[0] == "onMetaData")
                            {
                                if (recvComm.Parameters[1].GetType() == typeof(RtmpAmfObject))
                                {
                                    startIndex = 1;
                                }
                            }

                            for (int i = 2; i < recvComm.Parameters.Count; ++i)
                            {
                                if (recvComm.Parameters[i].GetType() == typeof(RtmpAmfObject))
                                {
                                    metadata = (RtmpAmfObject)recvComm.Parameters[i];
                                    break;
                                }
                            }
                        }

                        if (metadata != null)
                        {
                            MediaType videoMediaType = new MediaType { ContentType = MediaContentType.Video };
                            MediaType audioMediaType = new MediaType { ContentType = MediaContentType.Audio };
                            bool videoFound = false;
                            bool audioFound = false;

                            if (metadata.Numbers.ContainsKey("videocodecid"))
                            {
                                videoFound = true;
                                RtmpVideoCodec videoCodec = (RtmpVideoCodec)(int)metadata.Numbers["videocodecid"];
                                if (videoCodec != RtmpVideoCodec.AVC)
                                {
                                    // unsupported codec
                                    Global.Log.ErrorFormat("Command {0}, unsupported video codec {1}, dropping session...", msg.MessageType, videoCodec);
                                    this.transport.Disconnect(this.sessionEndPoint);
                                    break;
                                }
                                videoMediaType.Codec = MediaCodec.H264;
                            }

                            if (metadata.Numbers.ContainsKey("width"))
                            {
                                videoFound = true;
                                videoMediaType.Width = (int)metadata.Numbers["width"];
                            }

                            if (metadata.Numbers.ContainsKey("height"))
                            {
                                videoFound = true;
                                videoMediaType.Height = (int)metadata.Numbers["height"];
                            }

                            if (metadata.Numbers.ContainsKey("videoframerate"))
                            {
                                videoFound = true;
                                videoMediaType.Framerate = new Fraction(metadata.Numbers["videoframerate"], 1.0);
                            }
                            else if (metadata.Numbers.ContainsKey("framerate"))
                            {
                                videoFound = true;
                                videoMediaType.Framerate = new Fraction(metadata.Numbers["framerate"], 1.0);
                            }
                            else
                            {
                                videoMediaType.Framerate = new Fraction();
                            }

                            if (metadata.Numbers.ContainsKey("videodatarate"))
                            {
                                videoFound = true;
                                videoMediaType.Bitrate = (int)metadata.Numbers["videodatarate"] * 1000;
                            }

                            if (metadata.Numbers.ContainsKey("audiocodecid"))
                            {
                                videoFound = true;
                                RtmpAudioCodec audioCodec = (RtmpAudioCodec)(int)metadata.Numbers["audiocodecid"];
                                if (audioCodec != RtmpAudioCodec.AAC)
                                {
                                    // unsupported codec
                                    Global.Log.ErrorFormat("Command {0}, unsupported audio codec {1}, dropping session...", msg.MessageType, audioCodec);
                                    this.transport.Disconnect(this.sessionEndPoint);
                                    break;
                                }
                                audioMediaType.Codec = MediaCodec.AAC;
                            }

                            if (metadata.Numbers.ContainsKey("audioonly"))
                            {
                                audioFound = true;
                            }

                            if (metadata.Numbers.ContainsKey("audiodatarate"))
                            {
                                audioFound = true;
                                audioMediaType.Bitrate = (int)metadata.Numbers["audiodatarate"] * 1000;
                            }

                            if (metadata.Numbers.ContainsKey("audiosamplerate"))
                            {
                                audioFound = true;
                                audioMediaType.SampleRate = (int)metadata.Numbers["audiosamplerate"];
                            }

                            if (metadata.Numbers.ContainsKey("audiosamplesize"))
                            {
                                audioFound = true;
                                audioMediaType.SampleSize = (int)metadata.Numbers["audiosamplesize"];
                            }

                            if (metadata.Numbers.ContainsKey("audiochannels"))
                            {
                                audioFound = true;
                                audioMediaType.Channels = (int)metadata.Numbers["audiochannels"];
                            }
                            else if (metadata.Booleans.ContainsKey("stereo"))
                            {
                                audioFound = true;
                                audioMediaType.Channels = metadata.Booleans["stereo"] ? 2 : 1;
                            }

                            if (videoFound)
                            {
                                messageStream.VideoMediaType = videoMediaType;
                            }

                            if (audioFound)
                            {
                                messageStream.AudioMediaType = audioMediaType;
                            }
                        }
                        else
                        {
                            Global.Log.WarnFormat("Command {0}, metadata format not recognized", msg.MessageType);
                        }

                        break;
                    }

                case RtmpIntMessageType.Audio:
                    {
                        if (this.state != RtmpSessionState.Receiving)
                        {
                            // wrong state
                            Global.Log.ErrorFormat("Command {0}, wrong state {1}, dropping session...", msg.MessageType, this.state);
                            this.transport.Disconnect(this.sessionEndPoint);
                            break;
                        }

                        RtmpMessageStream messageStream = null;
                        if (this.messageStreams.ContainsKey(msg.MessageStreamId))
                        {
                            messageStream = this.messageStreams[msg.MessageStreamId];
                        }

                        if (messageStream == null)
                        {
                            // unexpected message stream
                            Global.Log.ErrorFormat("Command {0}, unregistered message stream {1}, dropping session...", msg.MessageType, msg.MessageStreamId);
                            this.transport.Disconnect(this.sessionEndPoint);
                            break;
                        }

                        RtmpMessageMedia recvComm = (RtmpMessageMedia)msg;

                        if (recvComm.AudioCodec != RtmpAudioCodec.AAC)
                        {
                            // unsupported codec
                            Global.Log.ErrorFormat("Command {0}, unsupported audio codec {1}, dropping session...", msg.MessageType, recvComm.AudioCodec);
                            this.transport.Disconnect(this.sessionEndPoint);
                            break;
                        }

                        if (messageStream.FirstAudioFrame)
                        {
                            if (messageStream.AudioMediaType == null)
                            {
                                messageStream.AudioMediaType = new MediaType { ContentType = MediaContentType.Audio };
                            }

                            messageStream.AudioMediaType.Codec = MediaCodec.AAC;
                            messageStream.AudioMediaType.SampleRate = recvComm.SampleRate;
                            messageStream.AudioMediaType.Channels = recvComm.Channels;
                            messageStream.AudioMediaType.SampleSize = recvComm.SampleSize;
                            // first received frame contains codec extra data
                            messageStream.AudioMediaType.ExtraData = new byte[recvComm.MediaData.ActualBufferSize];
                            Array.Copy(recvComm.MediaData.Buffer, 1, messageStream.AudioMediaType.ExtraData, 0, recvComm.MediaData.ActualBufferSize - 1);

                            messageStream.FirstAudioFrame = false;
                        }
                        else
                        {
                            // TODO: push to Smooth Streaming segmenter
                        }

                        recvComm.MediaData.Release();
                        break;
                    }

                case RtmpIntMessageType.Video:
                    {
                        if (this.state != RtmpSessionState.Receiving)
                        {
                            // wrong state
                            Global.Log.ErrorFormat("Command {0}, wrong state {1}, dropping session...", msg.MessageType, this.state);
                            this.transport.Disconnect(this.sessionEndPoint);
                            break;
                        }

                        RtmpMessageStream messageStream = null;
                        if (this.messageStreams.ContainsKey(msg.MessageStreamId))
                        {
                            messageStream = this.messageStreams[msg.MessageStreamId];
                        }

                        if (messageStream == null)
                        {
                            // unexpected message stream
                            Global.Log.ErrorFormat("Command {0}, unregistered message stream {1}, dropping session...", msg.MessageType, msg.MessageStreamId);
                            this.transport.Disconnect(this.sessionEndPoint);
                            break;
                        }

                        RtmpMessageMedia recvComm = (RtmpMessageMedia)msg;

                        if (recvComm.VideoCodec != RtmpVideoCodec.AVC)
                        {
                            // unsupported codec
                            Global.Log.ErrorFormat("Command {0}, unsupported video codec {1}, dropping session...", msg.MessageType, recvComm.VideoCodec);
                            this.transport.Disconnect(this.sessionEndPoint);
                            break;
                        }

                        if (messageStream.FirstVideoFrame)
                        {
                            if (messageStream.VideoMediaType == null)
                            {
                                messageStream.VideoMediaType = new MediaType { ContentType = MediaContentType.Video };
                            }

                            messageStream.VideoMediaType.Codec = MediaCodec.H264;
                            // first received frame contains codec extra data
                            int extraDataLen = recvComm.MediaData.ActualBufferSize - 2;
                            messageStream.VideoMediaType.ExtraData = new byte[extraDataLen];
                            Array.Copy(recvComm.MediaData.Buffer, recvComm.MediaData.ActualBufferSize - extraDataLen, messageStream.VideoMediaType.ExtraData, 0, extraDataLen);
                            //messageStream.VideoMediaType.ExtraData[3] = (byte)messageStream.VideoMediaType.ExtraData.Length;

                            StringBuilder sb = new StringBuilder();
                            for (int i = 0; i < messageStream.VideoMediaType.ExtraData.Length; ++i)
                            {
                                sb.AppendFormat("{0:X2}", messageStream.VideoMediaType.ExtraData[i]);
                            }
                            Debug.WriteLine("Private data: " + sb.ToString());

                            string hex = "00000001674d401fd94141fb011000003e90000ea600f18325800000000168e933c8";
                            messageStream.VideoMediaType.ExtraData = Enumerable.Range(0, hex.Length).Where(x => x % 2 == 0).Select(x => Convert.ToByte(hex.Substring(x, 2), 16)).ToArray();

                            // TODO: convert from AnnexB when necessary
                            // TODO: extract profile and level from metadata if specified

                            messageStream.FirstVideoFrame = false;
                        }
                        else
                        {
                            if (messageStream.MuxId == -1)
                            {
                                messageStream.MuxId = SmoothStreamingSegmenter.MCSSF_Initialize();
                            }

                            if (messageStream.VideoStreamId == -1)
                            {
                                SmoothStreamingSegmenter.MPEG2VIDEOINFO mvih = new SmoothStreamingSegmenter.MPEG2VIDEOINFO();
                                mvih.hdr.rcSource = new SmoothStreamingSegmenter.RECT { right = messageStream.VideoMediaType.Width, bottom = messageStream.VideoMediaType.Height };
                                mvih.hdr.rcTarget = new SmoothStreamingSegmenter.RECT { right = messageStream.VideoMediaType.Width, bottom = messageStream.VideoMediaType.Height };
                                mvih.hdr.dwBitRate = (uint)messageStream.VideoMediaType.Bitrate;
                                mvih.hdr.AvgTimePerFrame = 333667; // TODO: set proper
                                mvih.hdr.dwPictAspectRatioX = (uint)messageStream.VideoMediaType.Width;
                                mvih.hdr.dwPictAspectRatioY = (uint)messageStream.VideoMediaType.Height;

                                mvih.hdr.bmiHeader.biSize = (uint)Marshal.SizeOf(mvih.hdr.bmiHeader);
                                mvih.hdr.bmiHeader.biWidth = messageStream.VideoMediaType.Width;
                                mvih.hdr.bmiHeader.biHeight = messageStream.VideoMediaType.Height;
                                mvih.hdr.bmiHeader.biPlanes = 1;
                                mvih.hdr.bmiHeader.biBitCount = 24;
                                mvih.hdr.bmiHeader.biCompression = 0x31435641; // the only valid value is 'AVC1' as per Microsoft documentation
                                mvih.hdr.bmiHeader.biSizeImage = (uint)(mvih.hdr.bmiHeader.biWidth * mvih.hdr.bmiHeader.biHeight * mvih.hdr.bmiHeader.biBitCount / 8);
                                mvih.hdr.bmiHeader.biXPelsPerMeter = 0;
                                mvih.hdr.bmiHeader.biYPelsPerMeter = 0;
                                mvih.hdr.bmiHeader.biClrUsed = 0;
                                mvih.hdr.bmiHeader.biClrImportant = 0;

                                mvih.cbSequenceHeader = (uint)messageStream.VideoMediaType.ExtraData.Length;
                                mvih.dwProfile = 77;
                                mvih.dwLevel = 31;
                                mvih.dwFlags = 4;

                                int totalDataSize = messageStream.VideoMediaType.ExtraData.Length + Marshal.SizeOf(mvih);
                                Marshal.StructureToPtr(mvih, this.mediaDataPtr, true);
                                IntPtr extraDataPtr = IntPtr.Add(this.mediaDataPtr, Marshal.SizeOf(mvih) - 4);
                                Marshal.Copy(messageStream.VideoMediaType.ExtraData, 0, extraDataPtr, messageStream.VideoMediaType.ExtraData.Length);

                                // TODO: initialize first timestamp of segmenter
                                messageStream.VideoStreamId = SmoothStreamingSegmenter.MCSSF_AddStream(messageStream.MuxId, 2 /* video */, messageStream.VideoMediaType.Bitrate, 0, totalDataSize - 4, this.mediaDataPtr);

                                int headerSize = 0;
                                IntPtr headerPtr = IntPtr.Zero;
                                int res = SmoothStreamingSegmenter.MCSSF_GetHeader(messageStream.MuxId, messageStream.VideoStreamId, out headerSize, out headerPtr);

                                PacketBuffer header = Global.MediaAllocator.LockBuffer();
                                Marshal.Copy(headerPtr, header.Buffer, 0, headerSize);
                                header.ActualBufferSize = headerSize;

                                StringBuilder sb = new StringBuilder();
                                for (int i = 0; i < headerSize; ++i)
                                {
                                    if (header.Buffer[i] >= 0x20 && header.Buffer[i] <= 0x7F)
                                    {
                                        sb.Append((char)header.Buffer[i]);
                                    }
                                    else
                                    {
                                        sb.Append('.');
                                    }
                                }
                                Debug.WriteLine(sb.ToString());

                                // TODO: create publishing point using REST API for IIS Media Services

                                // TODO: push header to IIS publishing point
                                if (messageStream.WebRequest == null)
                                {
                                    messageStream.WebRequest = (HttpWebRequest)WebRequest.Create("http://192.168.0.101/sspush.isml/Streams(Encoder1)");
                                    messageStream.WebRequest.Method = "POST";
                                    messageStream.WebRequest.SendChunked = true;
                                    messageStream.WebRequest.KeepAlive = true;
                                    //messageStream.WebRequest.ContentType = "application/x-www-form-urlencoded";
                                    //messageStream.WebRequest.ContentLength = sendBytes.Length;
                                    messageStream.WebRequestStream = messageStream.WebRequest.GetRequestStream();
                                }

                                try
                                {
                                    messageStream.WebRequestStream.Write(header.Buffer, 0, header.ActualBufferSize);
                                    messageStream.WebRequestStream.Flush();
                                }
                                catch (Exception ex)
                                {
                                    int n = 1;
                                }

                                header.Release();
                            }

                            //// convert to annexb
                            //using (PacketBufferStream stream = new PacketBufferStream(recvComm.MediaData))
                            //{
                            //    using (EndianBinaryReader reader = new EndianBinaryReader(stream))
                            //    {
                            //        using (EndianBinaryWriter writer = new EndianBinaryWriter(stream))
                            //        {
                            //            do
                            //            {
                            //                reader.BaseStream.Seek(5, SeekOrigin.Begin);
                            //                uint nalSize = reader.ReadUInt32();
                            //                reader.BaseStream.Seek(-4, SeekOrigin.Current);
                            //                writer.Write((int)1);
                            //                reader.BaseStream.Seek(nalSize, SeekOrigin.Current);
                            //            }
                            //            while (reader.BaseStream.Position < reader.BaseStream.Length);
                            //        }
                            //    }
                            //}

                            Marshal.Copy(recvComm.MediaData.Buffer, 5, mediaDataPtr, recvComm.MediaData.ActualBufferSize - 5);
                            int outputDataSize = 0;
                            IntPtr outputDataPtr = IntPtr.Zero;
                            int pushResult = SmoothStreamingSegmenter.MCSSF_PushMedia(messageStream.MuxId, messageStream.VideoStreamId, recvComm.Timestamp * 10000, 0, recvComm.KeyFrame, recvComm.MediaData.ActualBufferSize - 5, mediaDataPtr, out outputDataSize, out outputDataPtr);

                            if (outputDataSize > 0)
                            {
                                // TODO: use allocator
                                PacketBuffer segment = Global.MediaAllocator.LockBuffer();
                                Marshal.Copy(outputDataPtr, segment.Buffer, 0, outputDataSize);
                                try
                                {
                                    messageStream.WebRequestStream.Write(segment.Buffer, 0, outputDataSize);
                                    messageStream.WebRequestStream.Flush();
                                }
                                catch (Exception ex)
                                {
                                    int n = 1;
                                }
                                segment.Release();
                            }
                            else
                            {
                                if (pushResult == 1)
                                {
                                    int n = 1;
                                }
                            }


                            //res = SmoothStreamingSegmenter.MCSSF_Uninitialize(muxId);



                        }

                        recvComm.MediaData.Release();
                        break;
                    }

                default:
                    {
                        if (this.state != RtmpSessionState.Receiving)
                        {
                            // wrong state
                            this.transport.Disconnect(this.sessionEndPoint);
                            break;
                        }

                        Global.Log.WarnFormat("Received unsupported message {0}", msg.MessageType);
                        break;
                    }
            }
        }

        // other public properties/methods TBD
    }
}
