﻿using MComms_Transmuxer.RTMP;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using MComms_Transmuxer.Transport;
using System.Net;
using MComms_Transmuxer;
using MComms_Transmuxer.Common;

namespace MComms_TransmuxerTests
{
    
    
    /// <summary>
    ///This is a test class for RtmpSessionTest and is intended
    ///to contain all RtmpSessionTest Unit Tests
    ///</summary>
    [TestClass()]
    public class RtmpSessionTest
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
        /// A test for Dispose, it includes also tests for OnReceive and ReleaseMessageStreams
        ///</summary>
        [TestMethod()]
        public void DisposeTest()
        {
            Global.Allocator = new PacketBufferAllocator(Global.TransportBufferSize, 10);
            long sessionId = 1;
            SocketTransport transport = new SocketTransport();
            transport.Start();
            IPEndPoint sessionEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9999);
            RtmpSession_Accessor target = new RtmpSession_Accessor(sessionId, transport, sessionEndPoint);

            target.messageStreams.Add(1, new RtmpMessageStream(1));

            byte[] buf = new byte[Global.TransportBufferSize];
            for (int i = 0; i < 10; ++i)
            {
                target.OnReceive(null, new TransportArgs(null, buf, 0, buf.Length));
            }

            Assert.IsTrue(target.receivedPackets.Count > 0);

            target.Dispose();
            Assert.IsNull(target.sessionThread);
            Assert.AreEqual(0, target.messageStreams.Count);
            Assert.AreEqual(0, target.receivedPackets.Count);
            Assert.IsNull(target.lastReceivedPacket);

            transport.Stop();
        }
    }
}
