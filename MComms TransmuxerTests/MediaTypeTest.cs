using MComms_Transmuxer.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace MComms_TransmuxerTests
{
    
    
    /// <summary>
    ///This is a test class for MediaTypeTest and is intended
    ///to contain all MediaTypeTest Unit Tests
    ///</summary>
    [TestClass()]
    public class MediaTypeTest
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
        ///A test for IsPrivateDataEqual
        ///</summary>
        [TestMethod()]
        public void IsPrivateDataEqualTest()
        {
            MediaType target = new MediaType();
            target.PrivateData = new byte[] { 0x00, 0x01, 0x02, 0x03 };
            Assert.IsFalse(target.IsPrivateDataEqual(null));
            byte[] privateData = new byte[] { 0x00, 0x01, 0x02 };
            Assert.IsFalse(target.IsPrivateDataEqual(privateData));
            privateData = new byte[] { 0x00, 0x01, 0x02, 0x04 };
            Assert.IsFalse(target.IsPrivateDataEqual(privateData));
            privateData = new byte[] { 0x00, 0x01, 0x02, 0x03 };
            Assert.IsTrue(target.IsPrivateDataEqual(privateData));
        }
    }
}
