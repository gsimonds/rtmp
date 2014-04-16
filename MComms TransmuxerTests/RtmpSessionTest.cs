using MComms_Transmuxer.RTMP;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using MComms_Transmuxer.Transport;
using System.Net;
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
		///A test for OnReceive
		///</summary>
		[TestMethod()]
		public void OnReceiveTest()
		{
            int sessionId = 1;
			SocketTransport transport = null; // TODO: Initialize to an appropriate value
			IPEndPoint sessionEndPoint = null; // TODO: Initialize to an appropriate value
			RtmpSession target = new RtmpSession(sessionId, transport, sessionEndPoint); // TODO: Initialize to an appropriate value
			PacketBuffer packet = null; // TODO: Initialize to an appropriate value
			target.OnReceive(packet);
			Assert.Inconclusive("A method that does not return a value cannot be verified.");
		}
	}
}
