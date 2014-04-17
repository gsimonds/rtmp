namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using MComms_Transmuxer.Common;
    using MComms_Transmuxer.Transport;

    class RtmpSession : IDisposable
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

        public RtmpSession(long sessionId, SocketTransport transport, IPEndPoint sessionEndPoint)
        {
            this.sessionId = sessionId;
            this.transport = transport;
            this.sessionEndPoint = sessionEndPoint;
            this.sessionThread = new Thread(this.SessionThread);
            this.sessionThread.Start();
        }

        public void Dispose()
        {
            this.isRunning = false;
            this.sessionThread.Join();
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

                    Global.Log.ErrorFormat("End point {0}: exception {1}, dropping session...", this.sessionEndPoint, ex.ToString());
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
                        Global.Log.DebugFormat("End point {0}: received {1}, new chunk size {2}", this.sessionEndPoint, msg.MessageType, recvCtrl.ChunkSize);
                        break;
                    }

                case RtmpIntMessageType.ProtoControlAbort:
                    {
                        RtmpMessageAbort recvCtrl = (RtmpMessageAbort)msg;
                        this.parser.Abort((uint)recvCtrl.TargetChunkStreamId);
                        Global.Log.DebugFormat("End point {0}: received {1}, aborted chunk stream {2}", this.sessionEndPoint, msg.MessageType, recvCtrl.TargetChunkStreamId);
                        break;
                    }

                case RtmpIntMessageType.ProtoControlAknowledgement:
                    {
                        RtmpMessageAck recvCtrl = (RtmpMessageAck)msg;
                        this.lastReportedSentSize = recvCtrl.ReceivedBytes;
                        Global.Log.DebugFormat("End point {0}: received {1}, reported size {2}, actually sent {3}", this.sessionEndPoint, msg.MessageType, recvCtrl.ReceivedBytes, this.sentSize);
                        // TODO: limit sending???
                        break;
                    }

                case RtmpIntMessageType.ProtoControlUserControl:
                    {
                        RtmpMessageUserControl recvCtrl = (RtmpMessageUserControl)msg;
                        switch (recvCtrl.EventType)
                        {
                            case RtmpMessageUserControl.EventTypes.SetBufferLength:
                                Global.Log.DebugFormat("End point {0}: received {1}, event {2}, message stream id {3}, buffer length {4}", this.sessionEndPoint, msg.MessageType, recvCtrl.EventType, recvCtrl.TargetMessageStreamId, recvCtrl.BufferLength);
                                break;
                            case RtmpMessageUserControl.EventTypes.PingRequest:
                            case RtmpMessageUserControl.EventTypes.PingResponse:
                                Global.Log.DebugFormat("End point {0}: received {1}, event {2}, timestamp {3}", this.sessionEndPoint, msg.MessageType, recvCtrl.EventType, recvCtrl.Timestamp);
                                break;
                            default:
                                Global.Log.DebugFormat("End point {0}: received {1}, event {2}, message stream id {3}", this.sessionEndPoint, msg.MessageType, recvCtrl.EventType, recvCtrl.TargetMessageStreamId);
                                break;
                        }
                        // TODO: support ping request?
                        break;
                    }

                case RtmpIntMessageType.ProtoControlWindowAknowledgementSize:
                    {
                        RtmpMessageWindowAckSize recvCtrl = (RtmpMessageWindowAckSize)msg;
                        this.receiveAckWindowSize = recvCtrl.AckSize;
                        Global.Log.DebugFormat("End point {0}: received {1}, new recv window size {2}", this.sessionEndPoint, msg.MessageType, recvCtrl.AckSize);
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
                        Global.Log.DebugFormat("End point {0}: received {1}, new send window size {2}", this.sessionEndPoint, msg.MessageType, recvCtrl.AckSize);
                        break;
                    }

                case RtmpIntMessageType.CommandNetConnectionConnect:
                    {
                        if (this.state != RtmpSessionState.Receiving)
                        {
                            // wrong state
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
                            this.transport.Disconnect(this.sessionEndPoint);
                            break;
                        }

                        // TODO: implement
                        break;
                    }

                case RtmpIntMessageType.CommandNetConnectionFCPublish:
                    {
                        if (this.state != RtmpSessionState.Receiving)
                        {
                            // wrong state
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
                            this.transport.Disconnect(this.sessionEndPoint);
                            break;
                        }

                        RtmpMessageCommand recvComm = (RtmpMessageCommand)msg;

                        // register new message stream
                        this.parser.RegisterMessageStream(this.messageStreamCounter);

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
                            Global.Log.ErrorFormat("End point {0}: Command {1}, wrong state {2}, dropping session...", this.sessionEndPoint, msg.MessageType, this.state);
                            this.transport.Disconnect(this.sessionEndPoint);
                            break;
                        }

                        RtmpMessageCommand recvComm = (RtmpMessageCommand)msg;
                        if (recvComm.Parameters.Count < 3 ||
                            recvComm.Parameters[1].GetType() != typeof(string) ||
                            recvComm.Parameters[2].GetType() != typeof(string))
                        {
                            // wrong command parameters
                            Global.Log.ErrorFormat("End point {0}: Command {1}, corrupted parameters, dropping session...", this.sessionEndPoint, msg.MessageType);
                            this.transport.Disconnect(this.sessionEndPoint);
                            break;
                        }

                        string publishStreamName = (string)recvComm.Parameters[1];
                        string publishType = (string)recvComm.Parameters[2];

                        // accept live streams only
                        if (publishType != "live")
                        {
                            // wrong app
                            Global.Log.ErrorFormat("End point {0}: Command {1}, unsupported publish type {2}, dropping session...", this.sessionEndPoint, msg.MessageType, publishType);
                            this.transport.Disconnect(this.sessionEndPoint);
                            break;
                        }

                        if (!this.parser.IsMessageStreamRegistered(recvComm.MessageStreamId))
                        {
                            // wrong publish sequence
                            Global.Log.ErrorFormat("End point {0}: Command {1}, unregistered message stream {2}, dropping session...", this.sessionEndPoint, msg.MessageType, recvComm.MessageStreamId);
                            this.transport.Disconnect(this.sessionEndPoint);
                            break;
                        }

                        // prepare reply

                        // send user control event "Stream N Begins"
                        this.parser.Encode(new RtmpMessageUserControl(RtmpMessageUserControl.EventTypes.StreamBegin, recvComm.MessageStreamId));

                        List<object> pars = new List<object>();

                        pars.Add(new RtmpAmfNull());

                        RtmpAmfObject amf = new RtmpAmfObject();
                        amf.Strings.Add("level", "status");
                        amf.Strings.Add("code", "NetStream.Publish.Start");
                        amf.Strings.Add("description", "Publishing " + publishStreamName);
                        amf.Numbers.Add("clientId", this.sessionId);
                        pars.Add(amf);

                        RtmpMessageCommand sendComm = new RtmpMessageCommand("onStatus", 0, pars);
                        sendComm.ChunkStreamId = recvComm.ChunkStreamId;
                        sendComm.MessageStreamId = recvComm.MessageStreamId;

                        this.parser.Encode(sendComm);

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

                        break;
                    }
            }
        }

        // other public properties/methods TBD
    }
}
