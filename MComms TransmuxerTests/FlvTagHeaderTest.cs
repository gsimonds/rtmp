using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using MComms_Transmuxer;
using MComms_Transmuxer.Common;
using MComms_Transmuxer.RTMP;

namespace MComms_TransmuxerTests
{
    
    
    /// <summary>
    ///This is a test class for FlvTagHeaderTest and is intended
    ///to contain all FlvTagHeaderTest Unit Tests
    ///</summary>
    [TestClass()]
    public class FlvTagHeaderTest
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
            FlvTagHeader target = new FlvTagHeader();
            target.TagType = RtmpMessageType.Audio;
            target.DataSize = 1024;
            target.Timestamp = 2000;
            Global.Allocator = new PacketBufferAllocator(Global.TransportBufferSize, 1);
            PacketBuffer actual = target.ToPacketBuffer();
            byte[] actualBuffer = new byte[actual.ActualBufferSize];
            Array.Copy(actual.Buffer, actualBuffer, actual.ActualBufferSize);
            byte[] correctBuffer = new byte[]
            {
                0x08,0x00,0x04,0x00,0x00,0x07,0xd0,0x00,0x00,0x00,0x00,
            };
            CollectionAssert.AreEqual(correctBuffer, actualBuffer);
        }
    }
}
