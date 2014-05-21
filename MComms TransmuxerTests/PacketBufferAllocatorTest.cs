using MComms_Transmuxer.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace MComms_TransmuxerTests
{
    
    
    /// <summary>
    ///This is a test class for PacketBufferAllocatorTest and is intended
    ///to contain all PacketBufferAllocatorTest Unit Tests
    ///</summary>
    [TestClass()]
    public class PacketBufferAllocatorTest
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
        ///A test for LockBuffer
        ///</summary>
        [TestMethod()]
        public void LockBufferTest()
        {
            int bufferSize = 1024;
            int bufferCount = 1;
            PacketBufferAllocator_Accessor target = new PacketBufferAllocator_Accessor(bufferSize, bufferCount);
            PacketBuffer buffer1 = target.LockBuffer();
            Assert.IsNotNull(buffer1);
            Assert.IsTrue(target.freeBuffers.Count == 0);
            Assert.IsTrue(target.lockedBuffers.Count == 1);
            // automatic expansion
            PacketBuffer buffer2 = target.LockBuffer();
            Assert.IsNotNull(buffer2);
            Assert.IsTrue(target.freeBuffers.Count == 0);
            Assert.IsTrue(target.lockedBuffers.Count == 2);
        }

        /// <summary>
        ///A test for ReleaseBuffer
        ///</summary>
        [TestMethod()]
        public void ReleaseBufferTest()
        {
            int bufferSize = 1024;
            int bufferCount = 1;
            PacketBufferAllocator_Accessor target = new PacketBufferAllocator_Accessor(bufferSize, bufferCount);
            PacketBuffer buffer = target.LockBuffer();
            Assert.IsNotNull(buffer);
            Assert.IsTrue(target.freeBuffers.Count == 0);
            Assert.IsTrue(target.lockedBuffers.Count == 1);
            target.ReleaseBuffer(buffer);
            Assert.IsTrue(target.freeBuffers.Count == 1);
            Assert.IsTrue(target.lockedBuffers.Count == 0);
        }

        /// <summary>
        ///A test for Reallocate
        ///</summary>
        [TestMethod()]
        public void ReallocateTest()
        {
            int bufferSize = 1024;
            int bufferCount = 1;
            PacketBufferAllocator_Accessor target = new PacketBufferAllocator_Accessor(bufferSize, bufferCount);
            int bufferSize1 = 2048;
            int bufferCount1 = 5;
            target.Reallocate(bufferSize1, bufferCount1);
            Assert.AreEqual(target.freeBuffers.Count, bufferCount1);
            Assert.AreEqual(target.freeBuffers[0].Buffer.Length, bufferSize1);
        }
    }
}
