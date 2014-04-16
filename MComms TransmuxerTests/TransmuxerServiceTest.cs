using MComms_Transmuxer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace MComms_TransmuxerTests
{
    
    
    /// <summary>
    ///This is a test class for TransmuxerServiceTest and is intended
    ///to contain all TransmuxerServiceTest Unit Tests
    ///</summary>
	[TestClass()]
	public class TransmuxerServiceTest
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
		///A test for OnStart
		///</summary>
		[TestMethod()]
		[DeploymentItem("MComms_Transmuxer.exe")]
		public void OnStartTest()
		{
			TransmuxerService_Accessor target = new TransmuxerService_Accessor(); // TODO: Initialize to an appropriate value
			string[] args = null; // TODO: Initialize to an appropriate value
			target.OnStart(args);
			Assert.Inconclusive("A method that does not return a value cannot be verified.");
		}

		/// <summary>
		///A test for OnStop
		///</summary>
		[TestMethod()]
		[DeploymentItem("MComms_Transmuxer.exe")]
		public void OnStopTest()
		{
			TransmuxerService_Accessor target = new TransmuxerService_Accessor(); // TODO: Initialize to an appropriate value
			target.OnStop();
			Assert.Inconclusive("A method that does not return a value cannot be verified.");
		}

		/// <summary>
		///A test for InitializeComponent
		///</summary>
		[TestMethod()]
		[DeploymentItem("MComms_Transmuxer.exe")]
		public void InitializeComponentTest()
		{
			TransmuxerService_Accessor target = new TransmuxerService_Accessor(); // TODO: Initialize to an appropriate value
			target.InitializeComponent();
			Assert.Inconclusive("A method that does not return a value cannot be verified.");
		}

		/// <summary>
		///A test for Dispose
		///</summary>
		[TestMethod()]
		[DeploymentItem("MComms_Transmuxer.exe")]
		public void DisposeTest()
		{
			TransmuxerService_Accessor target = new TransmuxerService_Accessor(); // TODO: Initialize to an appropriate value
			bool disposing = false; // TODO: Initialize to an appropriate value
			target.Dispose(disposing);
			Assert.Inconclusive("A method that does not return a value cannot be verified.");
		}

		/// <summary>
		///A test for TransmuxerService Constructor
		///</summary>
		[TestMethod()]
		public void TransmuxerServiceConstructorTest()
		{
			TransmuxerService target = new TransmuxerService();
			Assert.Inconclusive("TODO: Implement code to verify target");
		}
	}
}
