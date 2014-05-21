using MComms_Transmuxer.RTMP;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using MComms_Transmuxer;
using MComms_Transmuxer.Common;

namespace MComms_TransmuxerTests
{
    
    
    /// <summary>
    ///This is a test class for RtmpProtocolParserTest and is intended
    ///to contain all RtmpProtocolParserTest Unit Tests
    ///</summary>
    [TestClass()]
    public class RtmpProtocolParserTest
    {


        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        /// <summary>
        ///A test for Decode
        ///</summary>
        [TestMethod()]
        public void DecodeTest()
        {
            Global.Allocator = new PacketBufferAllocator(Global.TransportBufferSize, 1);
            Global.MediaAllocator = new PacketBufferAllocator(Global.OneMediaBufferSize, 1);

            // prepare video message divided into 2 chunks
            byte[] startingSequence = new byte[]
            {
                0x04,0x00,0x00,0xFF,0x00,0x00,0xA0,0x09,0x01,0x00,0x00,0x00,0x17,0x01,
            };
            PacketBuffer packetBuffer = Global.Allocator.LockBuffer();
            startingSequence.CopyTo(packetBuffer.Buffer, 0);
            packetBuffer.Buffer[128 + 12] = 0xC4;
            packetBuffer.ActualBufferSize = 160 + 12 + 1;

            RtmpProtocolParser target = new RtmpProtocolParser();
            target.State = RtmpSessionState.Receiving;
            target.RegisterMessageStream(1);

            RtmpMessageMedia actual = (RtmpMessageMedia)target.Decode(packetBuffer);
            Assert.IsNotNull(actual);
            Assert.AreEqual(4, (int)actual.ChunkStreamId);
            Assert.AreEqual(1, actual.MessageStreamId);
            Assert.AreEqual(RtmpIntMessageType.Video, actual.MessageType);
            Assert.AreEqual(255, actual.Timestamp);
            Assert.AreEqual(MediaContentType.Video, actual.ContentType);
            Assert.AreEqual(RtmpMediaPacketType.Media, actual.PacketType);
            Assert.AreEqual(RtmpVideoCodec.AVC, actual.VideoCodec);
            Assert.AreEqual(true, actual.KeyFrame);
            Assert.AreEqual(160, actual.MediaData.ActualBufferSize);
        }

        /// <summary>
        ///A test for Abort
        ///</summary>
        [TestMethod()]
        public void AbortTest()
        {
            Global.Allocator = new PacketBufferAllocator(Global.TransportBufferSize, 1);
            Global.MediaAllocator = new PacketBufferAllocator(Global.OneMediaBufferSize, 1);

            // prepare video message divided into 2 chunks
            byte[] startingSequence = new byte[]
            {
                0x04,0x00,0x00,0xFF,0x00,0x00,0xA0,0x09,0x01,0x00,0x00,0x00,0x17,0x01,
            };
            PacketBuffer packetBuffer = Global.Allocator.LockBuffer();
            startingSequence.CopyTo(packetBuffer.Buffer, 0);
            packetBuffer.ActualBufferSize = 128 + 12;

            RtmpProtocolParser_Accessor target = new RtmpProtocolParser_Accessor();
            target.State = RtmpSessionState.Receiving;
            target.RegisterMessageStream(1);

            RtmpMessage actual = target.Decode(packetBuffer);
            Assert.IsNull(actual);

            target.Abort(4);
            RtmpChunkStream_Accessor chunkStream = new RtmpChunkStream_Accessor(new PrivateObject(target.chunkStreams[4]));
            Assert.IsNull(chunkStream.incompleteMessageStream);
            Assert.IsNull(chunkStream.incompletePacketBuffer);
            Assert.IsNull(chunkStream.incompleteMessageChunkHeader);
        }

        /// <summary>
        ///A test for Encode
        ///</summary>
        [TestMethod()]
        public void EncodeTest()
        {
            Global.Allocator = new PacketBufferAllocator(Global.TransportBufferSize, 1);
            RtmpProtocolParser_Accessor target = new RtmpProtocolParser_Accessor();
            RtmpMessageSetChunkSize msg = new RtmpMessageSetChunkSize(1024);
            target.Encode(msg);
            Assert.AreEqual(1, target.outputQueue.Count);
        }

        /// <summary>
        ///A test for GetSendPacket
        ///</summary>
        [TestMethod()]
        public void GetSendPacketTest()
        {
            Global.Allocator = new PacketBufferAllocator(Global.TransportBufferSize, 1);
            RtmpProtocolParser_Accessor target = new RtmpProtocolParser_Accessor();
            RtmpMessageSetChunkSize msg = new RtmpMessageSetChunkSize(1024);
            target.Encode(msg);
            PacketBuffer actual = target.GetSendPacket();
            Assert.IsNotNull(actual);
            byte[] actualBuffer = new byte[actual.ActualBufferSize];
            Array.Copy(actual.Buffer, actualBuffer, actual.ActualBufferSize);
            byte[] correctBuffer = new byte[]
            {
                0x02,0x00,0x00,0x00,0x00,0x00,0x04,0x01,0x00,0x00,0x00,0x00,0x00,0x00,0x04,0x00,
            };
            CollectionAssert.AreEqual(correctBuffer, actualBuffer);
        }

        /// <summary>
        ///A test for IsMessageStreamRegistered
        ///</summary>
        [TestMethod()]
        public void IsMessageStreamRegisteredTest()
        {
            RtmpProtocolParser target = new RtmpProtocolParser();
            target.RegisterMessageStream(1);
            Assert.IsTrue(target.IsMessageStreamRegistered(1));
            Assert.IsFalse(target.IsMessageStreamRegistered(0));
        }

        /// <summary>
        ///A test for RegisterMessageStream
        ///</summary>
        [TestMethod()]
        public void RegisterMessageStreamTest()
        {
            RtmpProtocolParser_Accessor target = new RtmpProtocolParser_Accessor();
            target.RegisterMessageStream(1);
            Assert.AreEqual(1, target.registeredMessageStreams.Count);
            Assert.AreEqual(1, target.registeredMessageStreams[0]);
        }

        /// <summary>
        ///A test for UnregisterMessageStream
        ///</summary>
        [TestMethod()]
        public void UnregisterMessageStreamTest()
        {
            RtmpProtocolParser_Accessor target = new RtmpProtocolParser_Accessor();
            target.RegisterMessageStream(1);
            Assert.AreEqual(1, target.registeredMessageStreams.Count);
            Assert.AreEqual(1, target.registeredMessageStreams[0]);
            target.UnregisterMessageStream(1);
            Assert.AreEqual(0, target.registeredMessageStreams.Count);
        }
    }
}
