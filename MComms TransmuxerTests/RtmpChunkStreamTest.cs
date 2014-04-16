using MComms_Transmuxer.RTMP;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using MComms_Transmuxer.Common;

namespace MComms_TransmuxerTests
{
    
    
    /// <summary>
    ///This is a test class for RtmpChunkStreamTest and is intended
    ///to contain all RtmpChunkStreamTest Unit Tests
    ///</summary>
	[TestClass()]
	public class RtmpChunkStreamTest
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
		///A test for RtmpChunkStream Constructor
		///</summary>
		[TestMethod()]
		public void RtmpChunkStreamConstructorTest()
		{
			uint chunkStreamId = 0; // TODO: Initialize to an appropriate value
			int chunkSize = 0; // TODO: Initialize to an appropriate value
			RtmpChunkStream target = new RtmpChunkStream(chunkStreamId, chunkSize);
			Assert.Inconclusive("TODO: Implement code to verify target");
		}

		/// <summary>
		///A test for Decode
		///</summary>
		[TestMethod()]
		public void DecodeTest()
		{
			uint chunkStreamId = 0; // TODO: Initialize to an appropriate value
			int chunkSize = 0; // TODO: Initialize to an appropriate value
			RtmpChunkStream target = new RtmpChunkStream(chunkStreamId, chunkSize); // TODO: Initialize to an appropriate value
			RtmpChunkHeader hdr = null; // TODO: Initialize to an appropriate value
			PacketBufferStream dataStream = null; // TODO: Initialize to an appropriate value
			RtmpMessage expected = null; // TODO: Initialize to an appropriate value
			RtmpMessage actual;
            bool canContinue = true;
			actual = target.Decode(hdr, dataStream, ref canContinue);
			Assert.AreEqual(expected, actual);
			Assert.Inconclusive("Verify the correctness of this test method.");
		}
	}
}
