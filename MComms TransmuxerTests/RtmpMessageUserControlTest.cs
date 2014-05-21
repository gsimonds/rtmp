using MComms_Transmuxer.RTMP;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using MComms_Transmuxer;
using MComms_Transmuxer.Common;

namespace MComms_TransmuxerTests
{
    
    
    /// <summary>
    ///This is a test class for RtmpMessageUserControlTest and is intended
    ///to contain all RtmpMessageUserControlTest Unit Tests
    ///</summary>
    [TestClass()]
    public class RtmpMessageUserControlTest
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
            RtmpMessageUserControl.EventTypes eventType = RtmpMessageUserControl.EventTypes.StreamBegin;
            int targetMessageStreamId = 1;
            RtmpMessageUserControl target = new RtmpMessageUserControl(eventType, targetMessageStreamId);
            Global.Allocator = new PacketBufferAllocator(Global.TransportBufferSize, 1);
            PacketBuffer actual = target.ToRtmpChunk();
            byte[] actualBuffer = new byte[actual.ActualBufferSize];
            Array.Copy(actual.Buffer, actualBuffer, actual.ActualBufferSize);
            byte[] correctBuffer = new byte[]
            {
                0x02,0x00,0x00,0x00,0x00,0x00,0x06,0x04,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
                0x00,0x01,
            };
            CollectionAssert.AreEqual(correctBuffer, actualBuffer);
        }
    }
}
