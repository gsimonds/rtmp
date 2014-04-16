using MComms_Transmuxer.RTMP;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using MComms_Transmuxer.Common;

namespace MComms_TransmuxerTests
{
    
    
    /// <summary>
    ///This is a test class for RtmpProtocolParserTest and is intended
    ///to contain all RtmpProtocolParserTest Unit Tests
    ///</summary>
	[TestClass()]
	public class RtmpProtocolParserTest
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
		///A test for Decode
		///</summary>
		[TestMethod()]
		public void DecodeTest()
		{
			RtmpProtocolParser target = new RtmpProtocolParser(); // TODO: Initialize to an appropriate value
			PacketBuffer dataPacket = null; // TODO: Initialize to an appropriate value
			RtmpMessage expected = null; // TODO: Initialize to an appropriate value
			RtmpMessage actual;
			actual = target.Decode(dataPacket);
			Assert.AreEqual(expected, actual);
			Assert.Inconclusive("Verify the correctness of this test method.");
		}

		/// <summary>
		///A test for Encode
		///</summary>
		[TestMethod()]
		public void EncodeTest()
		{
			RtmpProtocolParser target = new RtmpProtocolParser(); // TODO: Initialize to an appropriate value
			RtmpMessage msg = null; // TODO: Initialize to an appropriate value
			target.Encode(msg);
			Assert.Inconclusive("A method that does not return a value cannot be verified.");
		}

		/// <summary>
		///A test for GetSendPacket
		///</summary>
		[TestMethod()]
		public void GetSendPacketTest()
		{
			RtmpProtocolParser target = new RtmpProtocolParser(); // TODO: Initialize to an appropriate value
			PacketBuffer expected = null; // TODO: Initialize to an appropriate value
			PacketBuffer actual;
			actual = target.GetSendPacket();
			Assert.AreEqual(expected, actual);
			Assert.Inconclusive("Verify the correctness of this test method.");
		}
	}
}
