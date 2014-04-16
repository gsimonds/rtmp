using MComms_Transmuxer.Transport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net;
using System.Net.Sockets;
using MComms_Transmuxer.Common;

namespace MComms_TransmuxerTests
{
    
    
    /// <summary>
    ///This is a test class for SocketTransportTest and is intended
    ///to contain all SocketTransportTest Unit Tests
    ///</summary>
	[TestClass()]
	public class SocketTransportTest
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
		///A test for Connect
		///</summary>
		[TestMethod()]
		public void ConnectTest()
		{
			SocketTransport target = new SocketTransport(); // TODO: Initialize to an appropriate value
			IPEndPoint endPoint = null; // TODO: Initialize to an appropriate value
			ProtocolType protocolType = new ProtocolType(); // TODO: Initialize to an appropriate value
			target.Connect(endPoint, protocolType);
			Assert.Inconclusive("A method that does not return a value cannot be verified.");
		}

		/// <summary>
		///A test for Disconnect
		///</summary>
		[TestMethod()]
		public void DisconnectTest()
		{
			SocketTransport target = new SocketTransport(); // TODO: Initialize to an appropriate value
			IPEndPoint endPoint = null; // TODO: Initialize to an appropriate value
			target.Disconnect(endPoint);
			Assert.Inconclusive("A method that does not return a value cannot be verified.");
		}

		/// <summary>
		///A test for Start
		///</summary>
		[TestMethod()]
		public void StartTest()
		{
			SocketTransport target = new SocketTransport(); // TODO: Initialize to an appropriate value
			IPEndPoint serverEndPoint = null; // TODO: Initialize to an appropriate value
			ProtocolType protocolType = new ProtocolType(); // TODO: Initialize to an appropriate value
			target.Start(serverEndPoint, protocolType);
			Assert.Inconclusive("A method that does not return a value cannot be verified.");
		}

		/// <summary>
		///A test for Send
		///</summary>
		[TestMethod()]
		public void SendTest()
		{
			SocketTransport target = new SocketTransport(); // TODO: Initialize to an appropriate value
			IPEndPoint endPoint = null; // TODO: Initialize to an appropriate value
			PacketBuffer packet = null; // TODO: Initialize to an appropriate value
			target.Send(endPoint, packet);
			Assert.Inconclusive("A method that does not return a value cannot be verified.");
		}

		/// <summary>
		///A test for Stop
		///</summary>
		[TestMethod()]
		public void StopTest()
		{
			SocketTransport target = new SocketTransport(); // TODO: Initialize to an appropriate value
			target.Stop();
			Assert.Inconclusive("A method that does not return a value cannot be verified.");
		}
	}
}
