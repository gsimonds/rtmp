using MComms_Transmuxer.RTMP;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using MComms_Transmuxer;
using MComms_Transmuxer.Common;

namespace MComms_TransmuxerTests
{
    
    
    /// <summary>
    ///This is a test class for FlvFileHeaderTest and is intended
    ///to contain all FlvFileHeaderTest Unit Tests
    ///</summary>
    [TestClass()]
    public class FlvFileHeaderTest
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
        ///A test for ToPacketBuffer
        ///</summary>
        [TestMethod()]
        public void ToPacketBufferTest()
        {
            bool haveAudio = true;
            bool haveVideo = true;
            FlvFileHeader target = new FlvFileHeader(haveAudio, haveVideo);
            Global.Allocator = new PacketBufferAllocator(Global.TransportBufferSize, 1);
            PacketBuffer actual = target.ToPacketBuffer();
            byte[] actualBuffer = new byte[actual.ActualBufferSize];
            Array.Copy(actual.Buffer, actualBuffer, actual.ActualBufferSize);
            byte[] correctBuffer = new byte[]
            {
                0x46,0x4c,0x56,0x01,0x05,0x00,0x00,0x00,0x09
            };
            CollectionAssert.AreEqual(correctBuffer, actualBuffer);
        }
    }
}
