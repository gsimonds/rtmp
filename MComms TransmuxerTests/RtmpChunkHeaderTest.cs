using MComms_Transmuxer.RTMP;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using MComms_Transmuxer.Common;

namespace MComms_TransmuxerTests
{
    
    
    /// <summary>
    ///This is a test class for RtmpChunkHeaderTest and is intended
    ///to contain all RtmpChunkHeaderTest Unit Tests
    ///</summary>
	[TestClass()]
	public class RtmpChunkHeaderTest
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
		///A test for RtmpChunkHeader Constructor
		///</summary>
		[TestMethod()]
		public void RtmpChunkHeaderConstructorTest()
		{
			RtmpChunkHeader target = new RtmpChunkHeader();
			Assert.Inconclusive("TODO: Implement code to verify target");
		}

		/// <summary>
		///A test for Decode
		///</summary>
		[TestMethod()]
		public void DecodeTest()
		{
			PacketBufferStream dataStream = null; // TODO: Initialize to an appropriate value
			RtmpChunkHeader expected = null; // TODO: Initialize to an appropriate value
			RtmpChunkHeader actual;
			actual = RtmpChunkHeader.Decode(dataStream);
			Assert.AreEqual(expected, actual);
			Assert.Inconclusive("Verify the correctness of this test method.");
		}

		/// <summary>
		///A test for ToPacketBuffer
		///</summary>
		[TestMethod()]
		public void ToPacketBufferTest()
		{
			RtmpChunkHeader target = new RtmpChunkHeader(); // TODO: Initialize to an appropriate value
			PacketBuffer packet = null; // TODO: Initialize to an appropriate value
			PacketBuffer expected = null; // TODO: Initialize to an appropriate value
			PacketBuffer actual;
			actual = target.ToPacketBuffer(packet);
			Assert.AreEqual(expected, actual);
			Assert.Inconclusive("Verify the correctness of this test method.");
		}

		/// <summary>
		///A test for ToPacketBuffer
		///</summary>
		[TestMethod()]
		public void ToPacketBufferTest1()
		{
			RtmpChunkHeader target = new RtmpChunkHeader(); // TODO: Initialize to an appropriate value
			PacketBuffer expected = null; // TODO: Initialize to an appropriate value
			PacketBuffer actual;
			actual = target.ToPacketBuffer();
			Assert.AreEqual(expected, actual);
			Assert.Inconclusive("Verify the correctness of this test method.");
		}
	}
}
