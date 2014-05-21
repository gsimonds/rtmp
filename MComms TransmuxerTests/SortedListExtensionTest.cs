using MComms_Transmuxer.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace MComms_TransmuxerTests
{
    
    
    /// <summary>
    ///This is a test class for SortedListExtensionTest and is intended
    ///to contain all SortedListExtensionTest Unit Tests
    ///</summary>
    [TestClass()]
    public class SortedListExtensionTest
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
        ///A test for FindFirstIndexGreaterThan
        ///</summary>
        [TestMethod()]
        public void FindFirstIndexGreaterThanTest()
        {
            SortedList<int, int> list = new SortedList<int, int>();
            list.Add(0, 0);
            list.Add(100, 100);
            list.Add(200, 200);
            Assert.AreEqual(0, list.FindFirstIndexGreaterThan(-1));
            Assert.AreEqual(1, list.FindFirstIndexGreaterThan(0));
            Assert.AreEqual(3, list.FindFirstIndexGreaterThan(200));
        }

        /// <summary>
        ///A test for FindFirstIndexLessThanOrEqualTo
        ///</summary>
        [TestMethod()]
        public void FindFirstIndexLessThanOrEqualToTest()
        {
            SortedList<int, int> list = new SortedList<int, int>();
            list.Add(0, 0);
            list.Add(100, 100);
            list.Add(200, 200);
            Assert.AreEqual(-1, list.FindFirstIndexLessThanOrEqualTo(-1));
            Assert.AreEqual(0, list.FindFirstIndexLessThanOrEqualTo(1));
            Assert.AreEqual(2, list.FindFirstIndexLessThanOrEqualTo(500));
        }
    }
}
