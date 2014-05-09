namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public enum RtmpMessageType : byte
    {
        SetChunkSize = 0x01,
        Abort = 0x02,
        Aknowledgement = 0x03,
        UserControl = 0x04,
        WindowAknowledgementSize = 0x05,
        SetPeerBandwidth = 0x06,
        Audio = 0x08,
        Video = 0x09,
        DataAmf3 = 0x0F,
        SharedObjectAmf3 = 0x10,
        CommandAmf3 = 0x11,
        DataAmf0 = 0x12,
        SharedObjectAmf0 = 0x13,
        CommandAmf0 = 0x14,
        Aggregate = 0x16,
        Undefined = 0xFF, // for internal use only
    }

    public enum RtmpIntMessageType
    {
        Undetermined,
        HandshakeC0,
        HandshakeS0,
        HandshakeC1,
        HandshakeS1,
        HandshakeC2,
        HandshakeS2,
        ProtoControlSetChunkSize, // 0x01
        ProtoControlAbort, // 0x02
        ProtoControlAknowledgement, // 0x03
        ProtoControlUserControl, // 0x04
        ProtoControlWindowAknowledgementSize, // 0x05
        ProtoControlSetPeerBandwidth, // 0x06
        Audio, // 0x08
        Video, // 0x09
        // 0x12 (AMF0):
        DataMetadata,
        DataTimestamp,
        DataUnsupported,
        SharedObject, // 0x13 (AMF0)
        // 0x14 (AMF0):
        CommandNetConnectionConnect,
        CommandNetConnectionCreateStream,
        CommandNetConnectionReleaseStream,
        CommandNetConnectionFCPublish,
        CommandNetConnectionOnFCPublish,
        CommandNetConnectionFCUnpublish,
        CommandNetConnectionOnFCUnpublish,
        CommandNetStreamCloseStream,
        CommandNetStreamDeleteStream,
        CommandNetStreamPublish,
        CommandNetStreamOnStatus,
        CommandResult,
        CommandError,
        CommandUnsupported,
        Aggregate, // 0x16
    }
}
