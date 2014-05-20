namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// RTMP message type
    /// </summary>
    public enum RtmpMessageType : byte
    {
        /// <summary>
        /// Set chunk size
        /// </summary>
        SetChunkSize = 0x01,

        /// <summary>
        /// Abort chunk stream
        /// </summary>
        Abort = 0x02,

        /// <summary>
        /// Aknowledgement report
        /// </summary>
        Aknowledgement = 0x03,

        /// <summary>
        /// User control
        /// </summary>
        UserControl = 0x04,

        /// <summary>
        /// Aknowledgement request
        /// </summary>
        WindowAknowledgementSize = 0x05,

        /// <summary>
        /// Set peer bandwidth
        /// </summary>
        SetPeerBandwidth = 0x06,

        /// <summary>
        /// Audio packet
        /// </summary>
        Audio = 0x08,

        /// <summary>
        /// Video packet
        /// </summary>
        Video = 0x09,

        /// <summary>
        /// AMF3 encoded metadata
        /// </summary>
        DataAmf3 = 0x0F,

        /// <summary>
        /// AMF3 encoded shared object
        /// </summary>
        SharedObjectAmf3 = 0x10,

        /// <summary>
        /// AMF3 encoded command
        /// </summary>
        CommandAmf3 = 0x11,

        /// <summary>
        /// AMF0 encoded metadata
        /// </summary>
        DataAmf0 = 0x12,

        /// <summary>
        /// AMF0 encoded shared object
        /// </summary>
        SharedObjectAmf0 = 0x13,

        /// <summary>
        /// AMF0 encoded command
        /// </summary>
        CommandAmf0 = 0x14,

        /// <summary>
        /// Aggregate
        /// </summary>
        Aggregate = 0x16,

        /// <summary>
        /// Undefined, for internal use only
        /// </summary>
        Undefined = 0xFF,
    }

    /// <summary>
    /// Internal RTMP message type
    /// </summary>
    public enum RtmpIntMessageType
    {
        /// <summary>
        /// Unknown/unsupported message type
        /// </summary>
        Undetermined,

        /// <summary>
        /// Handshake C0
        /// </summary>
        HandshakeC0,

        /// <summary>
        /// Handshake S0
        /// </summary>
        HandshakeS0,

        /// <summary>
        /// Handshake C1
        /// </summary>
        HandshakeC1,

        /// <summary>
        /// Handshake S1
        /// </summary>
        HandshakeS1,

        /// <summary>
        /// Handshake C2
        /// </summary>
        HandshakeC2,

        /// <summary>
        /// Handshake S2
        /// </summary>
        HandshakeS2,

        /// <summary>
        /// Protocol control message SetChunkSize (0x01)
        /// </summary>
        ProtoControlSetChunkSize,

        /// <summary>
        /// Protocol control message Abort (0x02)
        /// </summary>
        ProtoControlAbort,

        /// <summary>
        /// Protocol control message Aknowledgement (0x03)
        /// </summary>
        ProtoControlAknowledgement,

        /// <summary>
        /// Protocol control message UserControl (0x04)
        /// </summary>
        ProtoControlUserControl,

        /// <summary>
        /// Protocol control message WindowAknowledgementSize (0x05)
        /// </summary>
        ProtoControlWindowAknowledgementSize,

        /// <summary>
        /// Protocol control message SetPeerBandwidth (0x06)
        /// </summary>
        ProtoControlSetPeerBandwidth,

        /// <summary>
        /// Audio packet (0x08)
        /// </summary>
        Audio,

        /// <summary>
        /// Video packet (0x09)
        /// </summary>
        Video,

        /// <summary>
        /// AMF0 encoded stream metadata (0x12)
        /// </summary>
        DataMetadata,

        /// <summary>
        /// AMF0 encoded timestamps (0x12)
        /// </summary>
        DataTimestamp,

        /// <summary>
        /// AMF0 encoded unsupported metadata (0x12)
        /// </summary>
        DataUnsupported,

        /// <summary>
        /// AMF0 encoded shared object (0x13)
        /// </summary>
        SharedObject,

        /// <summary>
        /// AMF0 encoded command NetConnection.Connect (0x14)
        /// </summary>
        CommandNetConnectionConnect,

        /// <summary>
        /// AMF0 encoded command NetConnection.CreateStream (0x14)
        /// </summary>
        CommandNetConnectionCreateStream,

        /// <summary>
        /// AMF0 encoded command NetConnection.ReleaseStream (0x14)
        /// </summary>
        CommandNetConnectionReleaseStream,

        /// <summary>
        /// AMF0 encoded command NetConnection.FCPublish (0x14)
        /// </summary>
        CommandNetConnectionFCPublish,

        /// <summary>
        /// AMF0 encoded command NetConnection.OnFCPublish (0x14)
        /// </summary>
        CommandNetConnectionOnFCPublish,

        /// <summary>
        /// AMF0 encoded command NetConnection.FCUnpublish (0x14)
        /// </summary>
        CommandNetConnectionFCUnpublish,

        /// <summary>
        /// AMF0 encoded command NetConnection.OnFCUnpublish (0x14)
        /// </summary>
        CommandNetConnectionOnFCUnpublish,

        /// <summary>
        /// AMF0 encoded command NetStream.CloseStream (0x14)
        /// </summary>
        CommandNetStreamCloseStream,

        /// <summary>
        /// AMF0 encoded command NetStream.DeleteStream (0x14)
        /// </summary>
        CommandNetStreamDeleteStream,

        /// <summary>
        /// AMF0 encoded command NetStream.Publish (0x14)
        /// </summary>
        CommandNetStreamPublish,

        /// <summary>
        /// AMF0 encoded command NetStream.OnStatus (0x14)
        /// </summary>
        CommandNetStreamOnStatus,

        /// <summary>
        /// AMF0 encoded command NetStream._result (0x14)
        /// </summary>
        CommandResult,

        /// <summary>
        /// AMF0 encoded command NetStream._error (0x14)
        /// </summary>
        CommandError,

        /// <summary>
        /// AMF0 encoded unsupported command (0x14)
        /// </summary>
        CommandUnsupported,

        /// <summary>
        /// Aggregate (0x16)
        /// </summary>
        Aggregate,
    }
}
