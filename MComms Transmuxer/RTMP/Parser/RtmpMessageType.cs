namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    enum RtmpMessageType
    {
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
        Data, // 0x12 (AMF0)
        SharedObject, // 0x13 (AMF0)
        // 0x14 (AMF0):
        CommandConnect,
        CommandCreateStream,
        CommandDeleteStream,
        CommandReleaseStream,
        CommandFCPublish,
        CommandFCUnpublish,
        CommandPublish,
        Aggregate, // 0x16
    }
}
