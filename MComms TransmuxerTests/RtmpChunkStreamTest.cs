using MComms_Transmuxer.RTMP;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using MComms_Transmuxer;
using MComms_Transmuxer.Common;

namespace MComms_TransmuxerTests
{
    
    
    /// <summary>
    ///This is a test class for RtmpChunkStreamTest and is intended
    ///to contain all RtmpChunkStreamTest Unit Tests
    ///</summary>
    [TestClass()]
    public class RtmpChunkStreamTest
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

            uint chunkStreamId = 3;
            int chunkSize = Global.RtmpDefaultChunkSize;
            RtmpChunkStream target = new RtmpChunkStream(chunkStreamId, chunkSize);

            // prepare video message divided into 2 chunks
            byte[] startingSequence = new byte[]
            {
                0x04,0x00,0x00,0xFF,0x00,0x00,0xA0,0x09,0x01,0x00,0x00,0x00,0x17,0x01,
            };
            PacketBuffer packetBuffer = Global.Allocator.LockBuffer();
            startingSequence.CopyTo(packetBuffer.Buffer, 0);
            packetBuffer.Buffer[128 + 12] = 0xC4;
            packetBuffer.ActualBufferSize = 160 + 12 + 1;

            PacketBufferStream dataStream = new PacketBufferStream(packetBuffer);
            dataStream.Seek(0, System.IO.SeekOrigin.Begin);

            RtmpChunkHeader hdr = RtmpChunkHeader.Decode(dataStream);
            Assert.IsNotNull(hdr);

            bool canContinue = true;
            RtmpMessageMedia actual = (RtmpMessageMedia)target.Decode(hdr, dataStream, ref canContinue);

            // after first chunk RtmpMessage must be null and canContinue is true
            Assert.IsNull(actual);
            Assert.IsTrue(canContinue);

            hdr = RtmpChunkHeader.Decode(dataStream);
            Assert.IsNotNull(hdr);

            canContinue = true;
            actual = (RtmpMessageMedia)target.Decode(hdr, dataStream, ref canContinue);

            // after second chunk we must have decoded message
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

            uint chunkStreamId = 3;
            int chunkSize = Global.RtmpDefaultChunkSize;
            RtmpChunkStream_Accessor target = new RtmpChunkStream_Accessor(chunkStreamId, chunkSize);

            // prepare video message divided into 2 chunks
            byte[] startingSequence = new byte[]
            {
                0x04,0x00,0x00,0xFF,0x00,0x00,0xA0,0x09,0x01,0x00,0x00,0x00,0x17,0x01,
            };
            PacketBuffer packetBuffer = Global.Allocator.LockBuffer();
            startingSequence.CopyTo(packetBuffer.Buffer, 0);
            packetBuffer.Buffer[128 + 12] = 0xC4;
            packetBuffer.ActualBufferSize = 160 + 12 + 1;

            PacketBufferStream dataStream = new PacketBufferStream(packetBuffer);
            dataStream.Seek(0, System.IO.SeekOrigin.Begin);

            RtmpChunkHeader hdr = RtmpChunkHeader.Decode(dataStream);
            Assert.IsNotNull(hdr);

            bool canContinue = true;
            RtmpMessage actual = target.Decode(hdr, dataStream, ref canContinue);

            Assert.IsNotNull(target.incompleteMessageStream);
            Assert.IsNotNull(target.incompletePacketBuffer);
            Assert.AreEqual(hdr, target.incompleteMessageChunkHeader);

            target.Abort();
            Assert.IsNull(target.incompleteMessageStream);
            Assert.IsNull(target.incompletePacketBuffer);
            Assert.IsNull(target.incompleteMessageChunkHeader);
        }
    }
}
