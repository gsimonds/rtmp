using MComms_Transmuxer.RTMP;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using MComms_Transmuxer.Common;

namespace MComms_TransmuxerTests
{
    
    
    /// <summary>
    ///This is a test class for RtmpMessageTest and is intended
    ///to contain all RtmpMessageTest Unit Tests
    ///</summary>
    [TestClass()]
    public class RtmpMessageTest
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
        ///A test for Decode onFCPublish
        ///</summary>
        [TestMethod()]
        public void DecodeTestFCPublish()
        {
            byte[] dataBuffer = new byte[]
            {
                //Header
                0x03,0x00,0x00,0x00,0x00,0x00,0x8d,0x14,0x00,0x00,0x00,0x00,
                // onFCPublishlevelstatuscodeNetStream.Publish.StartdescriptionFCPublish to stream myStream5.clientidA	
                0x02, 0x00, 0x0b, 0x6f, 0x6e, 0x46, 0x43, 0x50, 0x75, 0x62, 0x6c, 0x69, 0x73, 0x68, 0x00, 0x00, 
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x05, 0x03, 0x00, 0x05, 0x6c, 0x65, 0x76, 0x65, 0x6c, 
                0x02, 0x00, 0x06, 0x73, 0x74, 0x61, 0x74, 0x75, 0x73, 0x00, 0x04, 0x63, 0x6f, 0x64, 0x65, 0x02, 
                0x00, 0x17, 0x4e, 0x65, 0x74, 0x53, 0x74, 0x72, 0x65, 0x61, 0x6d, 0x2e, 0x50, 0x75, 0x62, 0x6c,
                0x69, 0x73, 0x68, 0x2e, 0x53, 0x74, 0x61, 0x72, 0x74, 0x00, 0x0b, 0x64, 0x65, 0x73, 0x63, 0x72,
                0x69, 0x70, 0x74, 0x69, 0x6f, 0x6e, 0x02, 0x00, 0x1e, 0x46, 0x43, 0x50, 0x75, 0x62, 0x6c, 0x69,
                0x73, 0x68, 0x20, 0x74, 0x6f, 0x20, 0x73, 0x74, 0x72, 0x65, 0x61, 0x6d, 0x20, 0x6d, 0x79, 0x53,
                0x74, 0x72, 0x65, 0x61, 0x6d, 0x35, 0x2e, 0x00, 0x08, 0x63, 0x6c, 0x69, 0x65, 0x6e, 0x74, 0x69,
                0x64, 0x00, 0x41, 0xd4, 0x93, 0xa7, 0xe0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x09
            };
            // create allocator with 1 buffer of 141 bytes
            PacketBufferAllocator allocator = new PacketBufferAllocator(dataBuffer.GetLength(0), 1);
            // PacketBuffer should be initialized this way, not via operator "new"
            PacketBuffer packetBuffer = allocator.LockBuffer();

            PacketBufferStream packetBufferStream = new PacketBufferStream();
            packetBufferStream.Append(packetBuffer, 0, dataBuffer.GetLength(0));
            packetBufferStream.Write(dataBuffer, 0, dataBuffer.GetLength(0));
            //Assert.IsTrue(packetBufferStream.Length > 0);

            // after data has been written, we have to seek to the beginning of the stream
            packetBufferStream.Seek(0, System.IO.SeekOrigin.Begin);

            // it's better to initialize header using header parser
            RtmpChunkHeader hdr = RtmpChunkHeader.Decode(packetBufferStream);
            Assert.IsNotNull(hdr);

            RtmpMessage actual;
            actual = RtmpMessage.Decode(hdr, packetBufferStream);
            Assert.IsNotNull(actual);
            Assert.AreEqual(RtmpIntMessageType.CommandNetConnectionOnFCPublish, actual.MessageType);

            // cleanup buffers
            packetBuffer.Release();
            packetBufferStream.Dispose();
        }

        /// <summary>
        ///A test for Decode onFCPublish
        ///</summary>
        [TestMethod()]
        public void DecodeTestStreamBegin()
        {
            byte[] dataBuffer = new byte[]
            {
                // Real Time Messaging Protocol (User Control Message Stream Begin 1)
                0x02,0x00,0x00,0x00,0x00,0x00,0x06,0x04,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
                0x00,0x01
            };

            PacketBufferAllocator allocator = new PacketBufferAllocator(dataBuffer.GetLength(0), 1);
            PacketBuffer packetBuffer = allocator.LockBuffer();
            packetBuffer.ActualBufferSize = dataBuffer.GetLength(0);
            PacketBufferStream packetBufferStream = new PacketBufferStream(packetBuffer);
            packetBufferStream.Write(dataBuffer, 0, dataBuffer.GetLength(0));
            Assert.IsTrue(packetBufferStream.Length > 0);

            packetBufferStream.Seek(0, System.IO.SeekOrigin.Begin);
            RtmpChunkHeader hdr = RtmpChunkHeader.Decode(packetBufferStream);
            Assert.IsNotNull(hdr);

            RtmpMessage actual;
            actual = RtmpMessage.Decode(hdr, packetBufferStream);
            Assert.IsNotNull(actual);
            Assert.AreEqual(actual.MessageType, RtmpIntMessageType.ProtoControlUserControl);
        }

        /// <summary>
        ///A test for Decode Publish
        ///</summary>
        [TestMethod()]
        public void DecodeTestPublish()
        {
            byte[] dataBuffer = new byte[]
            {
                //Real Time Messaging Protocol (AMF0 Command onStatus('NetStream.Publish.Start'))
                0x03,0x00,0x00,0x00,0x00,0x00,0x81,0x14,0x01,0x00,0x00,0x00,0x02,0x00,0x08,0x6f,
                0x6e,0x53,0x74,0x61,0x74,0x75,0x73,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
                0x05,0x03,0x00,0x05,0x6c,0x65,0x76,0x65,0x6c,0x02,0x00,0x06,0x73,0x74,0x61,0x74,
                0x75,0x73,0x00,0x04,0x63,0x6f,0x64,0x65,0x02,0x00,0x17,0x4e,0x65,0x74,0x53,0x74,
                0x72,0x65,0x61,0x6d,0x2e,0x50,0x75,0x62,0x6c,0x69,0x73,0x68,0x2e,0x53,0x74,0x61,
                0x72,0x74,0x00,0x0b,0x64,0x65,0x73,0x63,0x72,0x69,0x70,0x74,0x69,0x6f,0x6e,0x02,
                0x00,0x15,0x50,0x75,0x62,0x6c,0x69,0x73,0x68,0x69,0x6e,0x67,0x20,0x6d,0x79,0x53,
                0x74,0x72,0x65,0x61,0x6d,0x35,0x2e,0x00,0x08,0x63,0x6c,0x69,0x65,0x6e,0x74,0x69,
                0x64,0x00,0x41,0xd4,0x93,0xa7,0xe0,0x00,0x00,0x00,0x00,0x00,0x09
            };

            PacketBufferAllocator allocator = new PacketBufferAllocator(dataBuffer.GetLength(0), 1);
            PacketBuffer packetBuffer = allocator.LockBuffer();
            packetBuffer.ActualBufferSize = dataBuffer.GetLength(0);
            PacketBufferStream packetBufferStream = new PacketBufferStream(packetBuffer);
            packetBufferStream.Write(dataBuffer, 0, dataBuffer.GetLength(0));
            Assert.IsTrue(packetBufferStream.Length > 0);

            packetBufferStream.Seek(0, System.IO.SeekOrigin.Begin);
            RtmpChunkHeader hdr = RtmpChunkHeader.Decode(packetBufferStream);
            Assert.IsNotNull(hdr);

            RtmpMessage actual;
            actual = RtmpMessage.Decode(hdr, packetBufferStream);
            Assert.IsNotNull(actual);
            Assert.AreEqual(RtmpIntMessageType.CommandNetStreamOnStatus, actual.MessageType);
        }
    }
}
