using MComms_Transmuxer.RTMP;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using MComms_Transmuxer;
using MComms_Transmuxer.Common;

namespace MComms_TransmuxerTests
{
    
    
    /// <summary>
    ///This is a test class for RtmpMessageMetadataTest and is intended
    ///to contain all RtmpMessageMetadataTest Unit Tests
    ///</summary>
    [TestClass()]
    public class RtmpMessageMetadataTest
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
        ///A test for ToFlvTag
        ///</summary>
        [TestMethod()]
        public void ToFlvTagTest()
        {
            List<object> parameters = new List<object>();
            parameters.Add("@setDataFrame");
            parameters.Add("onMetaData");
            RtmpAmfObject amf = new RtmpAmfObject();
            amf.Numbers.Add("videocodecid", (int)RtmpVideoCodec.AVC);
            amf.Numbers.Add("audiocodecid", (int)RtmpAudioCodec.AAC);
            parameters.Add(amf);

            RtmpMessageMetadata target = new RtmpMessageMetadata(parameters);
            target.ChunkStreamId = 3;
            target.MessageStreamId = 0;

            Global.Allocator = new PacketBufferAllocator(Global.TransportBufferSize, 1);
            PacketBuffer actual = target.ToFlvTag();
            byte[] actualBuffer = new byte[actual.ActualBufferSize];
            Array.Copy(actual.Buffer, actualBuffer, actual.ActualBufferSize);
            byte[] correctBuffer = new byte[]
            {
                0x12,0x00,0x00,0x3f,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x02,0x00,0x0a,0x6f,0x6e,
                0x4d,0x65,0x74,0x61,0x44,0x61,0x74,0x61,0x03,0x00,0x0c,0x76,0x69,0x64,0x65,0x6f,
                0x63,0x6f,0x64,0x65,0x63,0x69,0x64,0x00,0x40,0x1c,0x00,0x00,0x00,0x00,0x00,0x00,
                0x00,0x0c,0x61,0x75,0x64,0x69,0x6f,0x63,0x6f,0x64,0x65,0x63,0x69,0x64,0x00,0x40,
                0x24,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x09,
            };
            CollectionAssert.AreEqual(correctBuffer, actualBuffer);
        }

        /// <summary>
        ///A test for ToRtmpChunk
        ///</summary>
        [TestMethod()]
        public void ToRtmpChunkTest()
        {
            List<object> parameters = new List<object>();
            parameters.Add("@setDataFrame");
            parameters.Add("onMetaData");
            RtmpAmfObject amf = new RtmpAmfObject();
            amf.Numbers.Add("videocodecid", (int)RtmpVideoCodec.AVC);
            amf.Numbers.Add("audiocodecid", (int)RtmpAudioCodec.AAC);
            parameters.Add(amf);

            RtmpMessageMetadata target = new RtmpMessageMetadata(parameters);
            target.ChunkStreamId = 3;
            target.MessageStreamId = 0;

            Global.Allocator = new PacketBufferAllocator(Global.TransportBufferSize, 1);
            PacketBuffer actual = target.ToRtmpChunk();
            byte[] actualBuffer = new byte[actual.ActualBufferSize];
            Array.Copy(actual.Buffer, actualBuffer, actual.ActualBufferSize);
            byte[] correctBuffer = new byte[]
            {
                0x03,0x00,0x00,0x00,0x00,0x00,0x3f,0x12,0x00,0x00,0x00,0x00,0x02,0x00,0x0a,0x6f,
                0x6e,0x4d,0x65,0x74,0x61,0x44,0x61,0x74,0x61,0x03,0x00,0x0c,0x76,0x69,0x64,0x65,
                0x6f,0x63,0x6f,0x64,0x65,0x63,0x69,0x64,0x00,0x40,0x1c,0x00,0x00,0x00,0x00,0x00,
                0x00,0x00,0x0c,0x61,0x75,0x64,0x69,0x6f,0x63,0x6f,0x64,0x65,0x63,0x69,0x64,0x00,
                0x40,0x24,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x09,
            };
            CollectionAssert.AreEqual(correctBuffer, actualBuffer);
        }
    }
}
