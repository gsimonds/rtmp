using MComms_Transmuxer.RTMP;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using MComms_Transmuxer;
using MComms_Transmuxer.Common;

namespace MComms_TransmuxerTests
{
    
    
    /// <summary>
    ///This is a test class for RtmpChunkHeaderTest and is intended
    ///to contain all RtmpChunkHeaderTest Unit Tests
    ///</summary>
    [TestClass()]
    public class RtmpChunkHeaderTest
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
        public void DecodeType0Test()
        {
            PacketBufferStream dataStream = new PacketBufferStream();
            Global.Allocator = new PacketBufferAllocator(Global.TransportBufferSize, 1);
            PacketBuffer packet = Global.Allocator.LockBuffer();
            byte[] correctBuffer = new byte[]
            {
                0x03,0x00,0x00,0x02,0x00,0x00,0x59,0x14,0x01,0x00,0x00,0x00,
            };
            correctBuffer.CopyTo(packet.Buffer, 0);
            packet.ActualBufferSize = correctBuffer.Length;
            dataStream.Append(packet, 0, packet.ActualBufferSize);

            RtmpChunkHeader actual = RtmpChunkHeader.Decode(dataStream);
            Assert.IsNotNull(actual);
            Assert.AreEqual(actual.HeaderSize, correctBuffer.Length);
            Assert.AreEqual(actual.Format, 0);
            Assert.AreEqual((int)actual.ChunkStreamId, 3);
            Assert.AreEqual((int)actual.Timestamp, 2);
            Assert.AreEqual(actual.MessageLength, 89);
            Assert.AreEqual(actual.MessageType, RtmpMessageType.CommandAmf0);
            Assert.AreEqual(actual.MessageStreamId, 1);
        }

        /// <summary>
        ///A test for Decode
        ///</summary>
        [TestMethod()]
        public void DecodeType1Test()
        {
            PacketBufferStream dataStream = new PacketBufferStream();
            Global.Allocator = new PacketBufferAllocator(Global.TransportBufferSize, 1);
            PacketBuffer packet = Global.Allocator.LockBuffer();
            byte[] correctBuffer = new byte[]
            {
                0x43,0x00,0x00,0x03,0x00,0x00,0x26,0x14,
            };
            correctBuffer.CopyTo(packet.Buffer, 0);
            packet.ActualBufferSize = correctBuffer.Length;
            dataStream.Append(packet, 0, packet.ActualBufferSize);

            RtmpChunkHeader actual = RtmpChunkHeader.Decode(dataStream);
            Assert.IsNotNull(actual);
            Assert.AreEqual(actual.HeaderSize, correctBuffer.Length);
            Assert.AreEqual(actual.Format, 1);
            Assert.AreEqual((int)actual.ChunkStreamId, 3);
            Assert.AreEqual((int)actual.TimestampDelta, 3);
            Assert.AreEqual(actual.MessageLength, 38);
            Assert.AreEqual(actual.MessageType, RtmpMessageType.CommandAmf0);
        }

        /// <summary>
        ///A test for Decode
        ///</summary>
        [TestMethod()]
        public void DecodeType2Test()
        {
            PacketBufferStream dataStream = new PacketBufferStream();
            Global.Allocator = new PacketBufferAllocator(Global.TransportBufferSize, 1);
            PacketBuffer packet = Global.Allocator.LockBuffer();
            byte[] correctBuffer = new byte[]
            {
                0x83,0x00,0x00,0x04,
            };
            correctBuffer.CopyTo(packet.Buffer, 0);
            packet.ActualBufferSize = correctBuffer.Length;
            dataStream.Append(packet, 0, packet.ActualBufferSize);

            RtmpChunkHeader actual = RtmpChunkHeader.Decode(dataStream);
            Assert.IsNotNull(actual);
            Assert.AreEqual(actual.HeaderSize, correctBuffer.Length);
            Assert.AreEqual(actual.Format, 2);
            Assert.AreEqual((int)actual.ChunkStreamId, 3);
            Assert.AreEqual((int)actual.TimestampDelta, 4);
        }

        /// <summary>
        ///A test for Decode
        ///</summary>
        [TestMethod()]
        public void DecodeType3Test()
        {
            PacketBufferStream dataStream = new PacketBufferStream();
            Global.Allocator = new PacketBufferAllocator(Global.TransportBufferSize, 1);
            PacketBuffer packet = Global.Allocator.LockBuffer();
            byte[] correctBuffer = new byte[]
            {
                0xC4,
            };
            correctBuffer.CopyTo(packet.Buffer, 0);
            packet.ActualBufferSize = correctBuffer.Length;
            dataStream.Append(packet, 0, packet.ActualBufferSize);

            RtmpChunkHeader actual = RtmpChunkHeader.Decode(dataStream);
            Assert.IsNotNull(actual);
            Assert.AreEqual(actual.HeaderSize, correctBuffer.Length);
            Assert.AreEqual(actual.Format, 3);
            Assert.AreEqual((int)actual.ChunkStreamId, 4);
        }

        /// <summary>
        ///A test for ToRtmpChunk
        ///</summary>
        [TestMethod()]
        public void ToPacketBufferTest()
        {
            RtmpChunkHeader target = new RtmpChunkHeader();
            target.ChunkStreamId = 4;
            target.Timestamp = 1000;
            target.MessageLength = 2000;
            target.MessageType = RtmpMessageType.CommandAmf0;
            target.MessageStreamId = 1;
            Global.Allocator = new PacketBufferAllocator(Global.TransportBufferSize, 1);
            PacketBuffer actual = target.ToPacketBuffer();
            byte[] actualBuffer = new byte[actual.ActualBufferSize];
            Array.Copy(actual.Buffer, actualBuffer, actual.ActualBufferSize);
            byte[] correctBuffer = new byte[]
            {
                0x04,0x00,0x03,0xe8,0x00,0x07,0xd0,0x14,0x01,0x00,0x00,0x00,
            };
            CollectionAssert.AreEqual(correctBuffer, actualBuffer);
        }
    }
}
