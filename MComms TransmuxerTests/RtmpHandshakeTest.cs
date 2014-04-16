using MComms_Transmuxer.RTMP;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using MComms_Transmuxer.Common;

namespace MComms_TransmuxerTests
{
    
    
    /// <summary>
    ///This is a test class for RtmpHandshakeTest and is intended
    ///to contain all RtmpHandshakeTest Unit Tests
    ///</summary>
	[TestClass()]
	public class RtmpHandshakeTest
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
		///A test for RtmpHandshake Constructor
		///</summary>
		[TestMethod()]
		public void RtmpHandshakeConstructorTest()
		{
			RtmpHandshake target = new RtmpHandshake();
			Assert.Inconclusive("TODO: Implement code to verify target");
		}

		/// <summary>
		///A test for DecodeC0
		///</summary>
		[TestMethod()]
		public void DecodeC0Test()
		{
			PacketBufferStream dataStream = null; // TODO: Initialize to an appropriate value
			RtmpHandshake expected = null; // TODO: Initialize to an appropriate value
			RtmpHandshake actual;
			actual = RtmpHandshake.DecodeC0(dataStream);
			Assert.AreEqual(expected, actual);
			Assert.Inconclusive("Verify the correctness of this test method.");
		}

		/// <summary>
		///A test for DecodeC1
		///</summary>
		[TestMethod()]
		public void DecodeC1Test()
		{
			PacketBufferStream dataStream = null; // TODO: Initialize to an appropriate value
			RtmpHandshake expected = null; // TODO: Initialize to an appropriate value
			RtmpHandshake actual;
			actual = RtmpHandshake.DecodeC1(dataStream);
			Assert.AreEqual(expected, actual);
			Assert.Inconclusive("Verify the correctness of this test method.");
		}

		/// <summary>
		///A test for DecodeC2
		///</summary>
		[TestMethod()]
		public void DecodeC2Test()
		{
			PacketBufferStream dataStream = null; // TODO: Initialize to an appropriate value
			RtmpHandshake expected = null; // TODO: Initialize to an appropriate value
			RtmpHandshake actual;
			actual = RtmpHandshake.DecodeC2(dataStream);
			Assert.AreEqual(expected, actual);
			Assert.Inconclusive("Verify the correctness of this test method.");
		}

		/// <summary>
		///A test for GenerateS0
		///</summary>
		[TestMethod()]
		public void GenerateS0Test()
		{
			RtmpHandshake expected = null; // TODO: Initialize to an appropriate value
			RtmpHandshake actual;
			actual = RtmpHandshake.GenerateS0();
			Assert.AreEqual(expected, actual);
			Assert.Inconclusive("Verify the correctness of this test method.");
		}

		/// <summary>
		///A test for GenerateS1
		///</summary>
		[TestMethod()]
		public void GenerateS1Test()
		{
			RtmpHandshake expected = null; // TODO: Initialize to an appropriate value
			RtmpHandshake actual;
			actual = RtmpHandshake.GenerateS1();
			Assert.AreEqual(expected, actual);
			Assert.Inconclusive("Verify the correctness of this test method.");
		}

		/// <summary>
		///A test for GenerateS2
		///</summary>
		[TestMethod()]
		public void GenerateS2Test()
		{
			RtmpHandshake target = new RtmpHandshake(); // TODO: Initialize to an appropriate value
			RtmpHandshake expected = null; // TODO: Initialize to an appropriate value
			RtmpHandshake actual;
			actual = target.GenerateS2();
			Assert.AreEqual(expected, actual);
			Assert.Inconclusive("Verify the correctness of this test method.");
		}

		/// <summary>
		///A test for ToPacketBuffer
		///</summary>
		[TestMethod()]
		public void ToPacketBufferTest()
		{
			RtmpHandshake target = new RtmpHandshake(); // TODO: Initialize to an appropriate value
			PacketBuffer expected = null; // TODO: Initialize to an appropriate value
			PacketBuffer actual;
			actual = target.ToPacketBuffer();
			Assert.AreEqual(expected, actual);
			Assert.Inconclusive("Verify the correctness of this test method.");
		}

		/// <summary>
		///A test for ValidateC2
		///</summary>
		[TestMethod()]
		public void ValidateC2Test()
		{
			RtmpHandshake target = new RtmpHandshake(); // TODO: Initialize to an appropriate value
			RtmpHandshake handshakeS1 = null; // TODO: Initialize to an appropriate value
			bool expected = false; // TODO: Initialize to an appropriate value
			bool actual;
			actual = target.ValidateC2(handshakeS1);
			Assert.AreEqual(expected, actual);
			Assert.Inconclusive("Verify the correctness of this test method.");
		}
	}
}
