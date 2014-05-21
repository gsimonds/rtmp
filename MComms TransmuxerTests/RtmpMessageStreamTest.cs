using MComms_Transmuxer.RTMP;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using MComms_Transmuxer;
using MComms_Transmuxer.Common;
using MComms_Transmuxer.SmoothStreaming;

namespace MComms_TransmuxerTests
{
    
    
    /// <summary>
    ///This is a test class for RtmpMessageStreamTest and is intended
    ///to contain all RtmpMessageStreamTest Unit Tests
    ///</summary>
    [TestClass()]
    public class RtmpMessageStreamTest
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
        /// A test for ProcessMediaData. Can test first media packet (i.e. configuration) only.
        /// Regular media data requires connection to publishing point
        ///</summary>
        [TestMethod()]
        public void ProcessMediaDataTest()
        {
            Global.Allocator = new PacketBufferAllocator(Global.TransportBufferSize, 2);
            Global.MediaAllocator = new PacketBufferAllocator(Global.OneMediaBufferSize, 1);

            int messageStreamId = 1;
            RtmpMessageStream_Accessor target = new RtmpMessageStream_Accessor(messageStreamId);
            target.PublishName = "test";
            target.FullPublishName = "test";
            target.segmenter = new SmoothStreamingSegmenter("test", true);

            List<object> pars = new List<object>();
            pars.Add("onMetaData");
            RtmpAmfObject amf = new RtmpAmfObject();
            amf.Numbers.Add("width", 720);
            amf.Numbers.Add("height", 480);
            amf.Numbers.Add("videodatarate", 1000);
            amf.Numbers.Add("framerate", 30);
            amf.Numbers.Add("videocodecid", 7);
            amf.Numbers.Add("audiodatarate", 128);
            amf.Numbers.Add("audiosamplerate", 44100);
            amf.Numbers.Add("audiosamplesize", 16);
            amf.Numbers.Add("audiocodecid", 10);
            amf.Booleans.Add("stereo", true);
            pars.Add(amf);
            RtmpMessageMetadata msgMeta = new RtmpMessageMetadata(pars);
            target.ProcessMetadata(msgMeta);

            RtmpMessageMedia msg = new RtmpMessageMedia(RtmpVideoCodec.AVC, RtmpMediaPacketType.Configuration, 0, true);
            msg.MediaData = Global.Allocator.LockBuffer();
            byte[] data = new byte[]
            {
                0x17, 0x00, 0x00, 0x00, 0x00, 0x01, 0x4D, 0x40, 0x1F, 0xFF, 0xE1, 0x00,
                0x16, 0x67, 0x4D, 0x40, 0x1F, 0xEC, 0xA0, 0x5A, 0x1E, 0xD8, 0x08, 0x80,
                0x00, 0x01, 0xF4, 0x80, 0x00, 0xEA, 0x60, 0x07, 0x8C, 0x18, 0xCB, 0x01,
                0x00, 0x04, 0x68, 0xE9, 0x3B, 0xC8
            };
            data.CopyTo(msg.MediaData.Buffer, 0);
            msg.MediaData.ActualBufferSize = data.Length;
            target.ProcessMediaData(msg);

            Assert.IsNotNull(target.videoMediaType);
            Assert.IsNotNull(target.videoMediaType.PrivateData);
            Assert.IsNotNull(target.videoMediaType.PrivateDataIisString);
            Assert.AreNotEqual(Guid.Empty, target.videoStreamId);

            msg = new RtmpMessageMedia(RtmpAudioCodec.AAC, RtmpMediaPacketType.Configuration, 44100, 16, 2);
            msg.MediaData = Global.Allocator.LockBuffer();
            data = new byte[]
            {
                0xAF, 0x00, 0x12, 0x10, 0x56, 0xE5, 0x00
            };
            data.CopyTo(msg.MediaData.Buffer, 0);
            msg.MediaData.ActualBufferSize = data.Length;
            target.ProcessMediaData(msg);

            Assert.IsNotNull(target.audioMediaType);
            Assert.IsNotNull(target.audioMediaType.PrivateData);
            Assert.AreNotEqual(Guid.Empty, target.audioStreamId);

            target.Dispose();
        }

        /// <summary>
        ///A test for ProcessMetadata
        ///</summary>
        [TestMethod()]
        public void ProcessMetadataTestMetadata()
        {
            Global.Allocator = new PacketBufferAllocator(Global.TransportBufferSize, 1);
            Global.MediaAllocator = new PacketBufferAllocator(Global.OneMediaBufferSize, 1);

            int messageStreamId = 1;
            RtmpMessageStream_Accessor target = new RtmpMessageStream_Accessor(messageStreamId);
            target.PublishName = "test";
            target.FullPublishName = "test";
            target.segmenter = new SmoothStreamingSegmenter("test", true);

            List<object> pars = new List<object>();
            pars.Add("onMetaData");
            RtmpAmfObject amf = new RtmpAmfObject();
            amf.Numbers.Add("width", 720);
            amf.Numbers.Add("height", 480);
            amf.Numbers.Add("videodatarate", 1000);
            amf.Numbers.Add("framerate", 30);
            amf.Numbers.Add("videocodecid", 7);
            amf.Numbers.Add("audiodatarate", 128);
            amf.Numbers.Add("audiosamplerate", 44100);
            amf.Numbers.Add("audiosamplesize", 16);
            amf.Numbers.Add("audiocodecid", 10);
            amf.Booleans.Add("stereo", true);
            pars.Add(amf);
            RtmpMessageMetadata msg = new RtmpMessageMetadata(pars);
            target.ProcessMetadata(msg);

            Assert.IsNotNull(target.videoMediaType);
            Assert.AreEqual(MediaContentType.Video, target.videoMediaType.ContentType);
            Assert.AreEqual(MediaCodec.H264, target.videoMediaType.Codec);
            Assert.AreEqual(1000000, target.videoMediaType.Bitrate);
            Assert.AreEqual(720, target.videoMediaType.Width);
            Assert.AreEqual(480, target.videoMediaType.Height);
            Assert.AreEqual(30, target.videoMediaType.Framerate.Num);

            target.Dispose();
        }

        /// <summary>
        ///A test for ProcessMetadata
        ///</summary>
        [TestMethod()]
        public void ProcessMetadataTestTimestampSmpte()
        {
            Global.Allocator = new PacketBufferAllocator(Global.TransportBufferSize, 1);
            Global.MediaAllocator = new PacketBufferAllocator(Global.OneMediaBufferSize, 1);

            int messageStreamId = 1;
            RtmpMessageStream_Accessor target = new RtmpMessageStream_Accessor(messageStreamId);
            target.PublishName = "test";
            target.FullPublishName = "test";
            target.segmenter = new SmoothStreamingSegmenter("test", true);

            List<object> pars = new List<object>();
            pars.Add("onMetaData");
            RtmpAmfObject amf = new RtmpAmfObject();
            amf.Numbers.Add("width", 720);
            amf.Numbers.Add("height", 480);
            amf.Numbers.Add("videodatarate", 1000);
            amf.Numbers.Add("framerate", 30);
            amf.Numbers.Add("videocodecid", 7);
            amf.Numbers.Add("audiodatarate", 128);
            amf.Numbers.Add("audiosamplerate", 44100);
            amf.Numbers.Add("audiosamplesize", 16);
            amf.Numbers.Add("audiocodecid", 10);
            amf.Booleans.Add("stereo", true);
            pars.Add(amf);
            RtmpMessageMetadata msgMeta = new RtmpMessageMetadata(pars);
            target.ProcessMetadata(msgMeta);

            pars = new List<object>();
            pars.Add("onFI");
            amf = new RtmpAmfObject();
            amf.Strings.Add("tc", "00:00:01:00");
            pars.Add(amf);
            RtmpMessageMetadata msg = new RtmpMessageMetadata(pars);
            target.ProcessMetadata(msg);

            Assert.AreNotEqual(long.MinValue, target.timestampFirstSync);
            Assert.AreEqual(new DateTime(1001, 1, 1, 0, 0, 1, 0), target.absoluteTimeOrigin);

            pars = new List<object>();
            pars.Add("onFI");
            amf = new RtmpAmfObject();
            amf.Strings.Add("tc", "00:00:21:00");
            pars.Add(amf);
            msg = new RtmpMessageMetadata(pars);
            target.ProcessMetadata(msg);

            Assert.AreEqual(-20000, target.timestampAdjust);

            target.Dispose();
        }

        /// <summary>
        ///A test for ProcessMetadata
        ///</summary>
        [TestMethod()]
        public void ProcessMetadataTestTimestampSystem()
        {
            Global.Allocator = new PacketBufferAllocator(Global.TransportBufferSize, 1);
            Global.MediaAllocator = new PacketBufferAllocator(Global.OneMediaBufferSize, 1);

            int messageStreamId = 1;
            RtmpMessageStream_Accessor target = new RtmpMessageStream_Accessor(messageStreamId);
            target.PublishName = "test";
            target.FullPublishName = "test";
            target.segmenter = new SmoothStreamingSegmenter("test", true);

            List<object> pars = new List<object>();
            pars.Add("onMetaData");
            RtmpAmfObject amf = new RtmpAmfObject();
            amf.Numbers.Add("width", 720);
            amf.Numbers.Add("height", 480);
            amf.Numbers.Add("videodatarate", 1000);
            amf.Numbers.Add("framerate", 30);
            amf.Numbers.Add("videocodecid", 7);
            amf.Numbers.Add("audiodatarate", 128);
            amf.Numbers.Add("audiosamplerate", 44100);
            amf.Numbers.Add("audiosamplesize", 16);
            amf.Numbers.Add("audiocodecid", 10);
            amf.Booleans.Add("stereo", true);
            pars.Add(amf);
            RtmpMessageMetadata msgMeta = new RtmpMessageMetadata(pars);
            target.ProcessMetadata(msgMeta);

            pars = new List<object>();
            pars.Add("onFI");
            amf = new RtmpAmfObject();
            amf.Strings.Add("sd", "21-05-2014");
            amf.Strings.Add("st", "20:00:01.234");
            pars.Add(amf);
            RtmpMessageMetadata msg = new RtmpMessageMetadata(pars);
            target.ProcessMetadata(msg);

            Assert.AreNotEqual(long.MinValue, target.timestampFirstSync);
            Assert.AreEqual(new DateTime(2014, 5, 21, 20, 0, 1, 234), target.absoluteTimeOrigin);

            pars = new List<object>();
            pars.Add("onFI");
            amf = new RtmpAmfObject();
            amf.Strings.Add("sd", "21-05-2014");
            amf.Strings.Add("st", "20:00:00. 34");
            pars.Add(amf);
            msg = new RtmpMessageMetadata(pars);
            target.ProcessMetadata(msg);

            Assert.AreEqual(1200, target.timestampAdjust);

            target.Dispose();
        }
    }
}
