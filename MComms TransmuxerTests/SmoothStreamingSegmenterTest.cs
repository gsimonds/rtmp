using MComms_Transmuxer.SmoothStreaming;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using MComms_Transmuxer;
using MComms_Transmuxer.Common;

namespace MComms_TransmuxerTests
{
    
    
    /// <summary>
    ///This is a test class for SmoothStreamingSegmenterTest and is intended
    ///to contain all SmoothStreamingSegmenterTest Unit Tests
    ///</summary>
    [TestClass()]
    public class SmoothStreamingSegmenterTest
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
        ///A test for AdjustAbsoluteTime
        ///</summary>
        [TestMethod()]
        public void AdjustAbsoluteTimeTest()
        {
            Global.MediaAllocator = new PacketBufferAllocator(Global.OneMediaBufferSize, 1);
            string publishUri = "test";
            SmoothStreamingSegmenter_Accessor target = new SmoothStreamingSegmenter_Accessor(publishUri, true);
            target.timestampOffset = 1000;
            long gap = 10000;
            target.AdjustAbsoluteTime(gap);
            Assert.AreEqual(11000, target.timestampOffset);
        }

        /// <summary>
        ///A test for Dispose
        ///</summary>
        [TestMethod()]
        public void DisposeTest()
        {
            Global.MediaAllocator = new PacketBufferAllocator(Global.OneMediaBufferSize, 1);
            string publishUri = "test";
            SmoothStreamingSegmenter_Accessor target = new SmoothStreamingSegmenter_Accessor(publishUri, true);
            target.Dispose();
            Assert.AreEqual(IntPtr.Zero, target.mediaDataPtr);
        }

        /// <summary>
        ///A test for RegisterStream
        ///</summary>
        [TestMethod()]
        public void RegisterStreamTest()
        {
            Global.MediaAllocator = new PacketBufferAllocator(Global.OneMediaBufferSize, 1);
            string publishUri = "test";
            SmoothStreamingSegmenter_Accessor target = new SmoothStreamingSegmenter_Accessor(publishUri, true);

            MediaType mediaType = new MediaType();
            mediaType.ContentType = MediaContentType.Video;
            mediaType.Codec = MediaCodec.H264;
            mediaType.Bitrate = 1000000;
            mediaType.Width = 720;
            mediaType.Height = 480;
            mediaType.Framerate = new Fraction(30, 1);
            mediaType.PrivateData = new byte[]
            {
                0x01, 0x4D, 0x40, 0x1F, 0xFF, 0xE1, 0x00,
                0x16, 0x67, 0x4D, 0x40, 0x1F, 0xEC, 0xA0, 0x5A, 0x1E, 0xD8, 0x08, 0x80,
                0x00, 0x01, 0xF4, 0x80, 0x00, 0xEA, 0x60, 0x07, 0x8C, 0x18, 0xCB, 0x01,
                0x00, 0x04, 0x68, 0xE9, 0x3B, 0xC8
            };

            Guid actual = target.RegisterStream(mediaType);
            Assert.AreNotEqual(Guid.Empty, actual);
            Assert.IsNotNull(mediaType.PrivateDataIisString);
        }
    }
}
