using MComms_Transmuxer.RTMP;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using MComms_Transmuxer.Common;
using System.Collections.Generic;

namespace MComms_TransmuxerTests
{
    
    
    /// <summary>
    ///This is a test class for EndianBinaryWriterAmfExtensionTest and is intended
    ///to contain all EndianBinaryWriterAmfExtensionTest Unit Tests
    ///</summary>
    [TestClass()]
    public class EndianBinaryWriterAmfExtensionTest
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
        ///A test for WriteAmf0
        ///</summary>
        [TestMethod()]
        public void WriteAmf0Test()
        {
            MemoryStream ms = new MemoryStream();
            EndianBinaryWriter writer = new EndianBinaryWriter(ms);
            RtmpAmfObject amfObject = new RtmpAmfObject();
            amfObject.Numbers.Add("Number", 1.0);
            amfObject.Strings.Add("String", "test");
            amfObject.Booleans.Add("Boolean", true);
            bool isArray = false;
            EndianBinaryWriterAmfExtension.WriteAmf0(writer, amfObject, isArray);
            byte[] actualBuffer = new byte[ms.Length];
            ms.Position = 0;
            ms.Read(actualBuffer, 0, (int)ms.Length);
            byte[] correctBuffer = new byte[]
            {
                0x03,0x00,0x06,0x53,0x74,0x72,0x69,0x6e,0x67,0x02,0x00,0x04,0x74,0x65,0x73,0x74,
                0x00,0x06,0x4e,0x75,0x6d,0x62,0x65,0x72,0x00,0x3f,0xf0,0x00,0x00,0x00,0x00,0x00,
                0x00,0x00,0x07,0x42,0x6f,0x6f,0x6c,0x65,0x61,0x6e,0x01,0x01,0x00,0x00,0x09,
            };
            CollectionAssert.AreEqual(correctBuffer, actualBuffer);
        }

        /// <summary>
        ///A test for WriteAmf0
        ///</summary>
        [TestMethod()]
        public void WriteAmf0Test1()
        {
            MemoryStream ms = new MemoryStream();
            EndianBinaryWriter writer = new EndianBinaryWriter(ms);
            RtmpAmfObject amfObject = new RtmpAmfObject();
            amfObject.Numbers.Add("Number1", 2.0);
            amfObject.Numbers.Add("Number2", 3.0);
            amfObject.Numbers.Add("Number3", 4.0);
            bool isArray = true;
            EndianBinaryWriterAmfExtension.WriteAmf0(writer, amfObject, isArray);
            byte[] actualBuffer = new byte[ms.Length];
            ms.Position = 0;
            ms.Read(actualBuffer, 0, (int)ms.Length);
            byte[] correctBuffer = new byte[]
            {
                0x08,0x00,0x00,0x00,0x03,0x00,0x07,0x4e,0x75,0x6d,0x62,0x65,0x72,0x31,0x00,0x40,
                0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x07,0x4e,0x75,0x6d,0x62,0x65,0x72,0x32,
                0x00,0x40,0x08,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x07,0x4e,0x75,0x6d,0x62,0x65,
                0x72,0x33,0x00,0x40,0x10,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x09,
            };
            CollectionAssert.AreEqual(correctBuffer, actualBuffer);
        }

        /// <summary>
        ///A test for WriteAmf0
        ///</summary>
        [TestMethod()]
        public void WriteAmf0Test2()
        {
            MemoryStream ms = new MemoryStream();
            EndianBinaryWriter writer = new EndianBinaryWriter(ms);
            List<object> list = new List<object>();
            list.Add(5.0);
            list.Add("test string");
            list.Add(true);
            list.Add(new RtmpAmfNull());
            RtmpAmfObject amfObject = new RtmpAmfObject();
            amfObject.Numbers.Add("Number1", 6.0);
            list.Add(amfObject);
            EndianBinaryWriterAmfExtension.WriteAmf0(writer, list);
            byte[] actualBuffer = new byte[ms.Length];
            ms.Position = 0;
            ms.Read(actualBuffer, 0, (int)ms.Length);
            byte[] correctBuffer = new byte[]
            {
                0x00,0x40,0x14,0x00,0x00,0x00,0x00,0x00,0x00,0x02,0x00,0x0b,0x74,0x65,0x73,0x74,
                0x20,0x73,0x74,0x72,0x69,0x6e,0x67,0x01,0x01,0x05,0x03,0x00,0x07,0x4e,0x75,0x6d,
                0x62,0x65,0x72,0x31,0x00,0x40,0x18,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x09,
            };
            CollectionAssert.AreEqual(correctBuffer, actualBuffer);
        }
    }
}
