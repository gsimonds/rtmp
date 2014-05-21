using MComms_Transmuxer.RTMP;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using MComms_Transmuxer;
using MComms_Transmuxer.Common;

namespace MComms_TransmuxerTests
{
    
    
    /// <summary>
    ///This is a test class for RtmpMessageCommandTest and is intended
    ///to contain all RtmpMessageCommandTest Unit Tests
    ///</summary>
    [TestClass()]
    public class RtmpMessageCommandTest
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
        ///A test for ToRtmpChunk
        ///</summary>
        [TestMethod()]
        public void ToRtmpChunkTest()
        {
            string commandName = "_error";
            int transactionId = 1;
            List<object> parameters = new List<object>();
            parameters.Add(new RtmpAmfNull());
            RtmpAmfObject amf = new RtmpAmfObject();
            amf.Strings.Add("level", "status");
            amf.Strings.Add("code", "NetConnection.Connect.Failed");
            amf.Strings.Add("description", "Unrecognized command parameters");
            amf.Numbers.Add("clientId", 1);
            parameters.Add(amf);

            RtmpMessageCommand target = new RtmpMessageCommand(commandName, transactionId, parameters);
            target.ChunkStreamId = 3;
            target.MessageStreamId = 0;

            Global.Allocator = new PacketBufferAllocator(Global.TransportBufferSize, 1);
            PacketBuffer actual = target.ToRtmpChunk();
            byte[] actualBuffer = new byte[actual.ActualBufferSize];
            Array.Copy(actual.Buffer, actualBuffer, actual.ActualBufferSize);
            byte[] correctBuffer = new byte[]
            {
                0x03,0x00,0x00,0x00,0x00,0x00,0x8e,0x14,0x00,0x00,0x00,0x00,0x02,0x00,0x06,0x5f,
                0x65,0x72,0x72,0x6f,0x72,0x00,0x3f,0xf0,0x00,0x00,0x00,0x00,0x00,0x00,0x05,0x03,
                0x00,0x05,0x6c,0x65,0x76,0x65,0x6c,0x02,0x00,0x06,0x73,0x74,0x61,0x74,0x75,0x73,
                0x00,0x04,0x63,0x6f,0x64,0x65,0x02,0x00,0x1c,0x4e,0x65,0x74,0x43,0x6f,0x6e,0x6e,
                0x65,0x63,0x74,0x69,0x6f,0x6e,0x2e,0x43,0x6f,0x6e,0x6e,0x65,0x63,0x74,0x2e,0x46,
                0x61,0x69,0x6c,0x65,0x64,0x00,0x0b,0x64,0x65,0x73,0x63,0x72,0x69,0x70,0x74,0x69,
                0x6f,0x6e,0x02,0x00,0x1f,0x55,0x6e,0x72,0x65,0x63,0x6f,0x67,0x6e,0x69,0x7a,0x65,
                0x64,0x20,0x63,0x6f,0x6d,0x6d,0x61,0x6e,0x64,0x20,0x70,0x61,0x72,0x61,0x6d,0x65,
                0x74,0x65,0x72,0x73,0x00,0x08,0x63,0x6c,0x69,0x65,0x6e,0x74,0x49,0x64,0x00,0x3f,
                0xf0,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x09,
            };
            CollectionAssert.AreEqual(correctBuffer, actualBuffer);
        }
    }
}
