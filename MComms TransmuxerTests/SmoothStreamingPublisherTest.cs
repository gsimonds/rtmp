using MComms_Transmuxer.SmoothStreaming;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using MComms_Transmuxer.Common;

namespace MComms_TransmuxerTests
{
    
    
    /// <summary>
    ///This is a test class for SmoothStreamingPublisherTest and is intended
    ///to contain all SmoothStreamingPublisherTest Unit Tests
    ///</summary>
    [TestClass()]
    public class SmoothStreamingPublisherTest
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
        ///A test for Create
        ///</summary>
        [TestMethod()]
        public void CreateTest()
        {
            string publishUri = "test";
            SmoothStreamingPublisher publisher1 = SmoothStreamingPublisher.Create(publishUri, true);
            Assert.IsNotNull(publisher1);
            SmoothStreamingPublisher publisher2 = SmoothStreamingPublisher.Create(publishUri, true);
            Assert.IsTrue(publisher1 == publisher2);
        }

        /// <summary>
        ///A test for DeleteAll
        ///</summary>
        [TestMethod()]
        public void DeleteAllTest()
        {
            string publishUri = "test";
            SmoothStreamingPublisher publisher1 = SmoothStreamingPublisher.Create(publishUri, true);
            SmoothStreamingPublisher.DeleteAll();
            Assert.AreEqual(0, SmoothStreamingPublisher_Accessor.publishers.Count);
        }

        /// <summary>
        ///A test for DeleteExpired
        ///</summary>
        [TestMethod()]
        public void DeleteExpiredTest()
        {
            string publishUri = "test";
            SmoothStreamingPublisher_Accessor publisher = new SmoothStreamingPublisher_Accessor(new PrivateObject(SmoothStreamingPublisher.Create(publishUri, true)));
            SmoothStreamingPublisher.DeleteExpired();
            Assert.AreEqual(1, SmoothStreamingPublisher_Accessor.publishers.Count);
            publisher.lastActivity = publisher.lastActivity.AddMinutes(-1);
            SmoothStreamingPublisher.DeleteExpired();
            Assert.AreEqual(0, SmoothStreamingPublisher_Accessor.publishers.Count);
        }

        /// <summary>
        ///A test for RegisterMediaType
        ///</summary>
        [TestMethod()]
        public void RegisterMediaTypeTest()
        {
            string publishUri = "test";
            SmoothStreamingPublisher_Accessor target = new SmoothStreamingPublisher_Accessor(new PrivateObject(SmoothStreamingPublisher.Create(publishUri, true)));

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

            Guid actual1 = target.RegisterMediaType(mediaType);
            Assert.AreNotEqual(Guid.Empty, actual1);

            Guid actual2 = target.RegisterMediaType(mediaType);
            Assert.AreEqual(actual1, actual2);
        }

        /// <summary>
        ///A test for GetMediaType
        ///</summary>
        [TestMethod()]
        public void GetMediaTypeTest()
        {
            string publishUri = "test";
            SmoothStreamingPublisher_Accessor target = new SmoothStreamingPublisher_Accessor(new PrivateObject(SmoothStreamingPublisher.Create(publishUri, true)));

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
            Guid streamId = target.RegisterMediaType(mediaType);

            MediaType actual = target.GetMediaType(streamId);
            Assert.AreEqual(mediaType, actual);
        }

        /// <summary>
        ///A test for GetSynchronizationInfo
        ///</summary>
        [TestMethod()]
        public void GetSynchronizationInfoTest()
        {
            string publishUri = "test";
            SmoothStreamingPublisher_Accessor target = new SmoothStreamingPublisher_Accessor(new PrivateObject(SmoothStreamingPublisher.Create(publishUri, true)));
            
            DateTime testTime = DateTime.Now.AddHours(-1);
            long testTimestamp = 333333;

            target.lastPacketAbsoluteTime = testTime;
            target.lastPacketTimestamp = testTimestamp;

            DateTime lastAbsoluteTime;
            long lastTimestamp;
            target.GetSynchronizationInfo(out lastAbsoluteTime, out lastTimestamp);

            Assert.AreEqual(testTime, lastAbsoluteTime);
            Assert.AreEqual(testTimestamp, lastTimestamp);
        }

        /// <summary>
        ///A test for UnregisterExpiredStreams
        ///</summary>
        [TestMethod()]
        [DeploymentItem("MComms_Transmuxer.exe")]
        public void UnregisterExpiredStreamsTest()
        {
            SmoothStreamingPublisher.DeleteAll();

            string publishUri = "test";
            SmoothStreamingPublisher_Accessor target = new SmoothStreamingPublisher_Accessor(new PrivateObject(SmoothStreamingPublisher.Create(publishUri, true)));

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
            Guid streamId = target.RegisterMediaType(mediaType);

            target.UnregisterExpiredStreams();
            Assert.AreEqual(1, target.streams.Count);

            target.publishStreamId2LastActivity[streamId] = DateTime.Now.AddMinutes(-1);
            target.lastExpiredStreamsChecked = DateTime.MinValue;
            target.UnregisterExpiredStreams();
            Assert.AreEqual(0, target.streams.Count);
        }
    }
}
