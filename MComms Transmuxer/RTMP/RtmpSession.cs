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
        private DateTime lastActivity = DateTime.Now;

        public RtmpSession(long sessionId, SocketTransport transport, IPEndPoint sessionEndPoint)
        {
            this.sessionId = sessionId;
            this.transport = transport;
            this.sessionEndPoint = sessionEndPoint;
            this.sessionThread = new Thread(this.SessionThread);
            this.sessionThread.Start();
            Global.Log.DebugFormat("End point {0}, id {1}: created session object", this.sessionEndPoint, this.sessionId);
        }

        #region IDisposable

        public void Dispose()
        {
            this.isRunning = false;

            if (this.sessionThread != null)
            {
                this.sessionThread.Join();
                this.sessionThread = null;
            }

            this.ReleaseMessageStreams();

            Global.Log.DebugFormat("End point {0}, id {1}: session object disposed", this.sessionEndPoint, this.sessionId);
        }

        #endregion

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

                // disconnect by inactivity
                if ((DateTime.Now - this.lastActivity).TotalMilliseconds >= Global.RtmpSessionInactivityTimeoutMs)
                {
                    Global.Log.ErrorFormat("Dropping inactive session ({0} ms timeout)", this.state, (DateTime.Now - this.lastActivity).TotalMilliseconds);
                    this.transport.Disconnect(this.sessionEndPoint);
                    break;
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
            this.lastActivity = DateTime.Now;

            switch (msg.MessageType)
            {
                case RtmpIntMessageType.HandshakeC0:
                    {
                        if (this.state != RtmpSessionState.Uninitialized)
                        {
                            // wrong handshake sequence
                            Global.Log.ErrorFormat("Command {0}, wrong state {1}, dropping session...", msg.MessageType, this.state);
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
                            Global.Log.ErrorFormat("Unsupported protocol version {0}, dropping session...", handshake.Version);
                            this.transport.Disconnect(this.sessionEndPoint);
                        }

                        break;
                    }

                case RtmpIntMessageType.HandshakeC1:
                    {
                        if (this.state != RtmpSessionState.HanshakeVersionSent)
                        {
                            // wrong handshake sequence
                            Global.Log.ErrorFormat("Command {0}, wrong state {1}, dropping session...", msg.MessageType, this.state);
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
                            Global.Log.ErrorFormat("Command {0}, wrong state {1}, dropping session...", msg.MessageType, this.state);
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
                            Global.Log.Error("Handshake validation failed, dropping session...");
                            this.transport.Disconnect(this.sessionEndPoint);
                        }

                        break;
                    }

                case RtmpIntMessageType.ProtoControlSetChunkSize:
                    {
                        if (this.state != RtmpSessionState.Receiving)
                        {
                            // wrong state
                            Global.Log.ErrorFormat("Command {0}, wrong state {1}, dropping session...", msg.MessageType, this.state);
                            this.transport.Disconnect(this.sessionEndPoint);
                            break;
                        }

                        RtmpMessageSetChunkSize recvCtrl = (RtmpMessageSetChunkSize)msg;
                        this.parser.ChunkSize = (int)recvCtrl.ChunkSize;
                        Global.Log.DebugFormat("Received {0}, new chunk size {1}", msg.MessageType, recvCtrl.ChunkSize);
                        break;
                    }

                case RtmpIntMessageType.ProtoControlAbort:
                    {
                        if (this.state != RtmpSessionState.Receiving)
                        {
                            // wrong state
                            Global.Log.ErrorFormat("Command {0}, wrong state {1}, dropping session...", msg.MessageType, this.state);
                            this.transport.Disconnect(this.sessionEndPoint);
                            break;
                        }

                        RtmpMessageAbort recvCtrl = (RtmpMessageAbort)msg;
                        this.parser.Abort((uint)recvCtrl.TargetChunkStreamId);
                        Global.Log.DebugFormat("Received {0}, aborted chunk stream {1}", msg.MessageType, recvCtrl.TargetChunkStreamId);
                        break;
                    }

                case RtmpIntMessageType.ProtoControlAknowledgement:
                    {
                        if (this.state != RtmpSessionState.Receiving)
                        {
                            // wrong state
                            Global.Log.ErrorFormat("Command {0}, wrong state {1}, dropping session...", msg.MessageType, this.state);
                            this.transport.Disconnect(this.sessionEndPoint);
                            break;
                        }

                        RtmpMessageAck recvCtrl = (RtmpMessageAck)msg;
                        this.lastReportedSentSize = recvCtrl.ReceivedBytes;
                        Global.Log.DebugFormat("Received {0}, reported size {1}, actually sent {2}", msg.MessageType, recvCtrl.ReceivedBytes, this.sentSize);
                        // TODO: limit sending???
                        break;
                    }

                case RtmpIntMessageType.ProtoControlUserControl:
                    {
                        if (this.state != RtmpSessionState.Receiving)
                        {
                            // wrong state
                            Global.Log.ErrorFormat("Command {0}, wrong state {1}, dropping session...", msg.MessageType, this.state);
                            this.transport.Disconnect(this.sessionEndPoint);
                            break;
                        }

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
                        if (this.state != RtmpSessionState.Receiving)
                        {
                            // wrong state
                            Global.Log.ErrorFormat("Command {0}, wrong state {1}, dropping session...", msg.MessageType, this.state);
                            this.transport.Disconnect(this.sessionEndPoint);
                            break;
                        }

                        RtmpMessageWindowAckSize recvCtrl = (RtmpMessageWindowAckSize)msg;
                        this.receiveAckWindowSize = recvCtrl.AckSize;
                        Global.Log.DebugFormat("Received {0}, new recv window size {1}", msg.MessageType, recvCtrl.AckSize);
                        break;
                    }

                case RtmpIntMessageType.ProtoControlSetPeerBandwidth:
                    {
                        if (this.state != RtmpSessionState.Receiving)
                        {
                            // wrong state
                            Global.Log.ErrorFormat("Command {0}, wrong state {1}, dropping session...", msg.MessageType, this.state);
                            this.transport.Disconnect(this.sessionEndPoint);
                            break;
                        }

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

                        Global.Log.DebugFormat("Received command {0}", msg.MessageType);

                        RtmpMessageCommand recvComm = (RtmpMessageCommand)msg;
                        if (recvComm.Parameters.Count == 0 || recvComm.Parameters[0].GetType() != typeof(RtmpAmfObject))
                        {
                            // wrong command parameters
                            Global.Log.DebugFormat("Unrecognized command parameters");

                            List<object> errorPars = new List<object>();

                            errorPars.Add(new RtmpAmfNull());

                            RtmpAmfObject amf = new RtmpAmfObject();
                            amf.Strings.Add("level", "status");
                            amf.Strings.Add("code", "NetConnection.Connect.Failed");
                            amf.Strings.Add("description", "Unrecognized command parameters");
                            amf.Numbers.Add("clientId", this.sessionId);
                            errorPars.Add(amf);

                            RtmpMessageCommand sendComm = new RtmpMessageCommand("_error", 1, errorPars);
                            sendComm.ChunkStreamId = recvComm.ChunkStreamId;
                            sendComm.MessageStreamId = recvComm.MessageStreamId;

                            this.parser.Encode(sendComm);
                            break;
                        }

                        RtmpAmfObject par = recvComm.Parameters[0] as RtmpAmfObject;

                        // accept live streams only
                        if (!par.Strings.ContainsKey("app") || par.Strings["app"] != "live")
                        {
                            // wrong app
                            Global.Log.DebugFormat("Unsupported application {0}", par.Strings.ContainsKey("app") ? par.Strings["app"] : string.Empty);

                            List<object> errorPars = new List<object>();

                            errorPars.Add(new RtmpAmfNull());

                            RtmpAmfObject amf = new RtmpAmfObject();
                            amf.Strings.Add("level", "status");
                            amf.Strings.Add("code", "NetConnection.Connect.Failed");
                            amf.Strings.Add("description", "Unsupported application");
                            amf.Numbers.Add("clientId", this.sessionId);
                            errorPars.Add(amf);

                            RtmpMessageCommand sendComm = new RtmpMessageCommand("_error", 1, errorPars);
                            sendComm.ChunkStreamId = recvComm.ChunkStreamId;
                            sendComm.MessageStreamId = recvComm.MessageStreamId;

                            this.parser.Encode(sendComm);
                            break;
                        }

                        // prepare reply

                        // send window acknowledgement size
                        this.parser.Encode(new RtmpMessageWindowAckSize(this.sendAckWindowSize));

                        // send peer bandwidth
                        this.parser.Encode(new RtmpMessageSetPeerBandwidth(this.receiveAckWindowSize, RtmpMessageSetPeerBandwidth.LimitTypes.Dynamic));

                        // send user control event "Stream 0 Begins"
                        this.parser.Encode(new RtmpMessageUserControl(RtmpMessageUserControl.EventTypes.StreamBegin, 0));

                        // set sunk size
                        this.parser.Encode(new RtmpMessageSetChunkSize(Global.RtmpOurChunkSize));

                        {
                            List<object> pars = new List<object>();

                            RtmpAmfObject amf = new RtmpAmfObject();
                            amf.Strings.Add("fmsVer", "FMS/3,0,1,123"); // NOTE: value taken from FFmpeg implementation
                            amf.Numbers.Add("capabilities", 31); // NOTE: value taken from FFmpeg implementation
                            pars.Add(amf);

                            amf = new RtmpAmfObject();
                            amf.Strings.Add("level", "status");
                            amf.Strings.Add("code", "NetConnection.Connect.Success");
                            amf.Strings.Add("description", "Connection succeeded.");
                            amf.Numbers.Add("clientId", this.sessionId);
                            amf.Numbers.Add("objectEncoding", 0); // AMF0
                            pars.Add(amf);

                            RtmpMessageCommand sendComm = new RtmpMessageCommand("_result", 1, pars);
                            sendComm.ChunkStreamId = recvComm.ChunkStreamId;
                            sendComm.MessageStreamId = recvComm.MessageStreamId;

                            this.parser.Encode(sendComm);
                        }

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

                        // release all message streams if any
                        this.ReleaseMessageStreams();

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
                            Global.Log.ErrorFormat("Command {0}, unregistered message stream {1}", msg.MessageType, msg.MessageStreamId);

                            List<object> errorPars = new List<object>();

                            errorPars.Add(new RtmpAmfNull());

                            RtmpAmfObject amf = new RtmpAmfObject();
                            amf.Strings.Add("level", "status");
                            amf.Strings.Add("code", "NetStream.Publish.Failed");
                            amf.Strings.Add("description", "Unregistered message stream");
                            amf.Numbers.Add("clientId", this.sessionId);
                            errorPars.Add(amf);

                            RtmpMessageCommand sendComm = new RtmpMessageCommand("onStatus", 0, errorPars);
                            sendComm.ChunkStreamId = msg.ChunkStreamId;
                            sendComm.MessageStreamId = msg.MessageStreamId;

                            this.parser.Encode(sendComm);
                            break;
                        }

                        RtmpMessageCommand recvComm = (RtmpMessageCommand)msg;
                        if (recvComm.Parameters.Count < 3 ||
                            recvComm.Parameters[1].GetType() != typeof(string) ||
                            recvComm.Parameters[2].GetType() != typeof(string))
                        {
                            // wrong command parameters
                            Global.Log.ErrorFormat("Command {0}, corrupted parameters", msg.MessageType);

                            List<object> errorPars = new List<object>();

                            errorPars.Add(new RtmpAmfNull());

                            RtmpAmfObject amf = new RtmpAmfObject();
                            amf.Strings.Add("level", "status");
                            amf.Strings.Add("code", "NetStream.Publish.Failed");
                            amf.Strings.Add("description", "Unrecognized command parameters");
                            amf.Numbers.Add("clientId", this.sessionId);
                            errorPars.Add(amf);

                            RtmpMessageCommand sendComm = new RtmpMessageCommand("onStatus", 0, errorPars);
                            sendComm.ChunkStreamId = msg.ChunkStreamId;
                            sendComm.MessageStreamId = msg.MessageStreamId;

                            this.parser.Encode(sendComm);
                            break;
                        }

                        string publishType = (string)recvComm.Parameters[2];

                        // accept live streams only
                        if (publishType != "live")
                        {
                            // wrong app
                            Global.Log.ErrorFormat("Command {0}, unsupported publish type {1}", msg.MessageType, publishType);

                            List<object> errorPars = new List<object>();

                            errorPars.Add(new RtmpAmfNull());

                            RtmpAmfObject amf = new RtmpAmfObject();
                            amf.Strings.Add("level", "status");
                            amf.Strings.Add("code", "NetStream.Publish.Failed");
                            amf.Strings.Add("description", "Unsupported application");
                            amf.Numbers.Add("clientId", this.sessionId);
                            errorPars.Add(amf);

                            RtmpMessageCommand sendComm = new RtmpMessageCommand("onStatus", 0, errorPars);
                            sendComm.ChunkStreamId = msg.ChunkStreamId;
                            sendComm.MessageStreamId = msg.MessageStreamId;

                            this.parser.Encode(sendComm);
                            break;
                        }

                        string publishName = (string)recvComm.Parameters[1];

                        // remove query part from the publish name
                        int queryPos = publishName.IndexOf('?');
                        if (queryPos >= 0)
                        {
                            publishName = publishName.Substring(0, queryPos);
                        }

                        messageStream.PublishName = publishName;
                        messageStream.Publishing = true; // creating segmenter

                        // prepare reply
                        Global.Log.DebugFormat("Received command {0}", msg.MessageType);

                        // send user control event "Stream N Begins"
                        this.parser.Encode(new RtmpMessageUserControl(RtmpMessageUserControl.EventTypes.StreamBegin, recvComm.MessageStreamId));

                        {
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
                        }

                        break;
                    }

                case RtmpIntMessageType.CommandNetConnectionFCUnpublish:
                    {
                        if (this.state != RtmpSessionState.Receiving)
                        {
                            // wrong state
                            Global.Log.ErrorFormat("Command {0}, wrong state {1}, dropping session...", msg.MessageType, this.state);
                            this.transport.Disconnect(this.sessionEndPoint);
                            break;
                        }

                        RtmpMessageCommand recvComm = (RtmpMessageCommand)msg;
                        if (recvComm.Parameters.Count < 2 ||
                            recvComm.Parameters[1].GetType() != typeof(string))
                        {
                            // wrong command parameters
                            Global.Log.ErrorFormat("Command {0}, corrupted parameters", msg.MessageType);

                            List<object> errorPars = new List<object>();

                            errorPars.Add(new RtmpAmfNull());

                            RtmpAmfObject amf = new RtmpAmfObject();
                            amf.Strings.Add("level", "status");
                            amf.Strings.Add("code", "NetStream.Publish.Failed");
                            amf.Strings.Add("description", "Unrecognized command parameters");
                            amf.Numbers.Add("clientId", this.sessionId);
                            errorPars.Add(amf);

                            RtmpMessageCommand sendComm = new RtmpMessageCommand("onFCUnpublish", 0, errorPars);
                            sendComm.ChunkStreamId = msg.ChunkStreamId;
                            sendComm.MessageStreamId = msg.MessageStreamId;

                            this.parser.Encode(sendComm);
                            break;
                        }

                        Global.Log.DebugFormat("Received command {0}", msg.MessageType);

                        string publishName = (string)recvComm.Parameters[1];

                        // remove query part from the publish name
                        int queryPos = publishName.IndexOf('?');
                        if (queryPos >= 0)
                        {
                            publishName = publishName.Substring(0, queryPos);
                        }

                        // close IIS connection and release segmenter
                        foreach (RtmpMessageStream messageStream in messageStreams.Values)
                        {
                            if (messageStream.PublishName == publishName)
                            {
                                messageStream.Publishing = false;
                                Global.Log.DebugFormat("Unpublished message stream {0}, publish name {1}", messageStream.MessageStreamId, messageStream.PublishName);
                            }
                        }

                        {
                            List<object> pars = new List<object>();

                            pars.Add(new RtmpAmfNull());

                            RtmpAmfObject amf = new RtmpAmfObject();
                            amf.Strings.Add("level", "status");
                            amf.Strings.Add("code", "NetStream.Publish.Stop");
                            amf.Strings.Add("description", "Unpublishing " + publishName);
                            amf.Numbers.Add("clientId", this.sessionId);
                            pars.Add(amf);

                            RtmpMessageCommand sendComm = new RtmpMessageCommand("onFCUnpublish", 0, pars);
                            sendComm.ChunkStreamId = recvComm.ChunkStreamId;
                            sendComm.MessageStreamId = recvComm.MessageStreamId;

                            this.parser.Encode(sendComm);
                        }

                        {
                            List<object> pars = new List<object>();

                            pars.Add(new RtmpAmfNull());

                            RtmpAmfObject amf = new RtmpAmfObject();
                            amf.Strings.Add("level", "status");
                            amf.Strings.Add("code", "NetStream.Unpublish.Success");
                            amf.Strings.Add("description", "Stream " + publishName + " has been unpublished");
                            amf.Numbers.Add("clientId", this.sessionId);
                            pars.Add(amf);

                            RtmpMessageCommand sendComm = new RtmpMessageCommand("onStatus", 0, pars);
                            sendComm.ChunkStreamId = recvComm.ChunkStreamId;
                            sendComm.MessageStreamId = recvComm.MessageStreamId;

                            this.parser.Encode(sendComm);
                        }

                        break;
                    }

                case RtmpIntMessageType.CommandNetStreamCloseStream:
                    {
                        if (this.state != RtmpSessionState.Receiving)
                        {
                            // wrong state
                            Global.Log.ErrorFormat("Command {0}, wrong state {1}, dropping session...", msg.MessageType, this.state);
                            this.transport.Disconnect(this.sessionEndPoint);
                            break;
                        }

                        Global.Log.DebugFormat("Received command {0}", msg.MessageType);

                        // this is a redundant command, it could follow FCUnpublish
                        // just in case reset publising flag one more time
                        if (messageStreams.ContainsKey(msg.MessageStreamId))
                        {
                            messageStreams[msg.MessageStreamId].Publishing = false;
                            Global.Log.DebugFormat("Closed message stream {0}", msg.MessageStreamId);
                        }

                        break;
                    }

                case RtmpIntMessageType.CommandNetStreamDeleteStream:
                    {
                        if (this.state != RtmpSessionState.Receiving)
                        {
                            // wrong state
                            Global.Log.ErrorFormat("Command {0}, wrong state {1}, dropping session...", msg.MessageType, this.state);
                            this.transport.Disconnect(this.sessionEndPoint);
                            break;
                        }

                        RtmpMessageCommand recvComm = (RtmpMessageCommand)msg;
                        if (recvComm.Parameters.Count < 2 ||
                            recvComm.Parameters[1].GetType() != typeof(double))
                        {
                            // wrong command parameters, just close connection
                            Global.Log.ErrorFormat("Command {0}, corrupted parameters", msg.MessageType);
                            this.transport.Disconnect(this.sessionEndPoint);
                            break;
                        }

                        Global.Log.DebugFormat("Received command {0}", msg.MessageType);

                        int messageStreamId = (int)(double)recvComm.Parameters[1];
                        if (messageStreams.ContainsKey(messageStreamId))
                        {
                            messageStreams[messageStreamId].Dispose();
                            messageStreams.Remove(messageStreamId);
                            Global.Log.DebugFormat("Deleted message stream {0}", messageStreamId);
                        }

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
                            Global.Log.ErrorFormat("Command {0}, unregistered message stream {1}", msg.MessageType, msg.MessageStreamId);
                            break;
                        }

                        Global.Log.DebugFormat("Received command {0}", msg.MessageType);

                        try
                        {
                            messageStream.ProcessMetadata((RtmpMessageMetadata)msg);
                        }
                        catch (CriticalStreamException unex)
                        {
                            Global.Log.Error(unex.Message);
                            this.transport.Disconnect(this.sessionEndPoint);
                        }
                        catch (Exception ex)
                        {
                            Global.Log.ErrorFormat("Failed to process metadata: {0}", ex.ToString());
                        }

                        break;
                    }

                case RtmpIntMessageType.Audio:
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
                            Global.Log.ErrorFormat("Command {0}, unregistered message stream {1}", msg.MessageType, msg.MessageStreamId);
                            break;
                        }

                        try
                        {
                            messageStream.ProcessMediaData((RtmpMessageMedia)msg);
                        }
                        catch (CriticalStreamException unex)
                        {
                            Global.Log.Error(unex.Message);
                            this.transport.Disconnect(this.sessionEndPoint);
                        }
                        catch (Exception ex)
                        {
                            Global.Log.ErrorFormat("Failed to process media data: {0}", ex.ToString());
                        }

                        ((RtmpMessageMedia)msg).MediaData.Release();

                        break;
                    }

                default:
                    {
                        if (this.state != RtmpSessionState.Receiving)
                        {
                            // wrong state
                            Global.Log.ErrorFormat("Command {0}, wrong state {1}, dropping session...", msg.MessageType, this.state);
                            this.transport.Disconnect(this.sessionEndPoint);
                            break;
                        }

                        Global.Log.WarnFormat("Received unsupported message {0}", msg.MessageType);
                        break;
                    }
            }
        }

        private void ReleaseMessageStreams()
        {
            foreach (RtmpMessageStream messageStream in messageStreams.Values)
            {
                messageStream.Dispose();
            }

            messageStreams.Clear();
        }
    }
}
