using MComms_Transmuxer.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using MComms_Transmuxer;

namespace MComms_TransmuxerTests
{
    
    
    /// <summary>
    ///This is a test class for PacketBufferStreamTest and is intended
    ///to contain all PacketBufferStreamTest Unit Tests
    ///</summary>
    [TestClass()]
    public class PacketBufferStreamTest
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
        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            // initialize global allocator for all tests
            Global.Allocator = new PacketBufferAllocator(Global.TransportBufferSize, 100);
        }
        
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
        ///A test for Dispose
        ///</summary>
        [TestMethod()]
        [DeploymentItem("MComms_Transmuxer.exe")]
        public void DisposeTest()
        {
            PacketBufferStream_Accessor target = new PacketBufferStream_Accessor();
            PacketBuffer packet = Global.Allocator.LockBuffer();
            packet.ActualBufferSize = 10;
            target.Append(packet, 0, packet.ActualBufferSize);
            bool disposing = true;
            target.Dispose(disposing);
            Assert.AreEqual(target.bufferId2BufferEntry.Count, 0);
        }

        /// <summary>
        ///A test for CanRead
        ///</summary>
        [TestMethod()]
        public void CanReadTest()
        {
            PacketBufferStream target = new PacketBufferStream();
            Assert.IsTrue(target.CanRead);
        }

        /// <summary>
        ///A test for CanSeek
        ///</summary>
        [TestMethod()]
        public void CanSeekTest()
        {
            PacketBufferStream target = new PacketBufferStream();
            Assert.IsTrue(target.CanSeek);
        }

        /// <summary>
        ///A test for CanWrite
        ///</summary>
        [TestMethod()]
        public void CanWriteTest()
        {
            PacketBufferStream target = new PacketBufferStream();
            Assert.IsTrue(target.CanWrite);
        }

        /// <summary>
        ///A test for Length
        ///</summary>
        [TestMethod()]
        public void LengthTest()
        {
            PacketBufferStream_Accessor target = new PacketBufferStream_Accessor();
            PacketBuffer packet = Global.Allocator.LockBuffer();
            packet.ActualBufferSize = 10;
            target.Append(packet, 0, packet.ActualBufferSize);
            Assert.AreEqual((int)target.Length, packet.ActualBufferSize);
        }

        /// <summary>
        ///A test for Seek
        ///</summary>
        [TestMethod()]
        public void SeekTest()
        {
            PacketBufferStream_Accessor target = new PacketBufferStream_Accessor();
            PacketBuffer packet = Global.Allocator.LockBuffer();
            packet.ActualBufferSize = 10;
            target.Append(packet, 0, packet.ActualBufferSize);
            target.Seek(2, SeekOrigin.Begin);
            Assert.AreEqual((int)target.Position, 2);
            target.Seek(3, SeekOrigin.Current);
            Assert.AreEqual((int)target.Position, 5);
            target.Seek(1, SeekOrigin.End);
            Assert.AreEqual((int)target.Position, 9);
        }

        /// <summary>
        ///A test for ReadByte
        ///</summary>
        [TestMethod()]
        public void ReadByteTest()
        {
            PacketBufferStream_Accessor target = new PacketBufferStream_Accessor();
            PacketBuffer packet = Global.Allocator.LockBuffer();
            packet.ActualBufferSize = 10;
            byte expected = 0x55;
            packet.Buffer[5] = expected;
            target.Append(packet, 0, packet.ActualBufferSize);
            target.Position = 5;
            byte actual = (byte)target.ReadByte();
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        ///A test for Read
        ///</summary>
        [TestMethod()]
        public void ReadTest()
        {
            PacketBufferStream_Accessor target = new PacketBufferStream_Accessor();
            PacketBuffer packet = Global.Allocator.LockBuffer();
            byte[] correctBuffer = new byte[]
            {
                0x08,0x00,0x04,0x00,0x00,0x07,0xd0,0x00,0x00,0x00,0xEE,
            };
            correctBuffer.CopyTo(packet.Buffer, 0);
            packet.ActualBufferSize = correctBuffer.Length;
            target.Append(packet, 0, packet.ActualBufferSize);
            byte[] actualBuffer = new byte[correctBuffer.Length];
            int actuallyRead = target.Read(actualBuffer, 0, actualBuffer.Length);
            Assert.AreEqual(actuallyRead, actualBuffer.Length);
            CollectionAssert.AreEqual(correctBuffer, actualBuffer);
        }

        /// <summary>
        ///A test for Write
        ///</summary>
        [TestMethod()]
        public void WriteTest()
        {
            PacketBufferStream_Accessor target = new PacketBufferStream_Accessor();
            PacketBuffer packet = Global.Allocator.LockBuffer();
            byte[] correctBuffer = new byte[]
            {
                0x08,0x00,0x04,0x00,0x00,0x07,0xd0,0x00,0x00,0x00,0xEE,
            };
            packet.ActualBufferSize = correctBuffer.Length;
            target.Append(packet, 0, packet.ActualBufferSize);
            target.Write(correctBuffer, 0, correctBuffer.Length);
            byte[] actualBuffer = new byte[correctBuffer.Length];
            Array.Copy(packet.Buffer, actualBuffer, packet.ActualBufferSize);
            CollectionAssert.AreEqual(correctBuffer, actualBuffer);
        }

        /// <summary>
        ///A test for FirstPacketBuffer
        ///</summary>
        [TestMethod()]
        public void FirstPacketBufferTest()
        {
            PacketBufferStream_Accessor target = new PacketBufferStream_Accessor();
            PacketBuffer packet1 = Global.Allocator.LockBuffer();
            packet1.ActualBufferSize = 10;
            target.Append(packet1, 0, packet1.ActualBufferSize);
            Assert.AreEqual(target.FirstPacketBuffer, packet1);
            PacketBuffer packet2 = Global.Allocator.LockBuffer();
            packet2.ActualBufferSize = 10;
            target.Append(packet2, 0, packet2.ActualBufferSize);
            Assert.AreEqual(target.FirstPacketBuffer, packet1);
        }

        /// <summary>
        ///A test for Append
        ///</summary>
        [TestMethod()]
        public void AppendTest()
        {
            PacketBufferStream_Accessor target = new PacketBufferStream_Accessor();
            PacketBuffer packet = Global.Allocator.LockBuffer();
            packet.ActualBufferSize = 10;
            target.Append(packet, 0, packet.ActualBufferSize);
            Assert.AreEqual(target.position2BufferId[0], packet.Id);
            PacketBufferStream_Accessor.BufferEntry entry = new PacketBufferStream_Accessor.BufferEntry(new PrivateObject(target.bufferId2BufferEntry[packet.Id]));
            Assert.AreEqual(entry.Offset, 0);
            Assert.AreEqual(entry.Size, packet.ActualBufferSize);
            Assert.AreEqual(entry.Buffer, packet);
        }

        /// <summary>
        ///A test for CopyTo
        ///</summary>
        [TestMethod()]
        public void CopyToTest()
        {
            PacketBufferStream_Accessor target = new PacketBufferStream_Accessor();
            PacketBuffer packet1 = Global.Allocator.LockBuffer();
            byte[] correctBuffer = new byte[]
            {
                0x08,0x00,0x04,0x00,0x00,0x07,0xd0,0x00,0x00,0x00,0xEE,
            };
            correctBuffer.CopyTo(packet1.Buffer, 0);
            packet1.ActualBufferSize = correctBuffer.Length;
            target.Append(packet1, 0, packet1.ActualBufferSize);

            PacketBufferStream stream = new PacketBufferStream();
            PacketBuffer packet2 = Global.Allocator.LockBuffer();
            packet2.ActualBufferSize = correctBuffer.Length;
            stream.Append(packet2, 0, packet2.ActualBufferSize);

            int copied = target.CopyTo(stream, correctBuffer.Length);
            Assert.AreEqual(copied, correctBuffer.Length);
            CollectionAssert.AreEqual(packet1.Buffer, packet2.Buffer);
        }

        /// <summary>
        ///A test for TrimBegin
        ///</summary>
        [TestMethod()]
        public void TrimBeginTest()
        {
            PacketBufferStream_Accessor target = new PacketBufferStream_Accessor();
            PacketBuffer packet1 = Global.Allocator.LockBuffer();
            packet1.ActualBufferSize = 10;
            target.Append(packet1, 0, packet1.ActualBufferSize);

            // cut the beginning of the first packet
            target.Position = 8;
            target.TrimBegin();
            PacketBufferStream_Accessor.BufferEntry entry = new PacketBufferStream_Accessor.BufferEntry(new PrivateObject(target.bufferId2BufferEntry[packet1.Id]));
            Assert.AreEqual(entry.Offset, 8);
            Assert.AreEqual(entry.Size, 2);
            Assert.AreEqual(entry.Buffer, packet1);

            // cut out the first packet completely
            PacketBuffer packet2 = Global.Allocator.LockBuffer();
            packet2.ActualBufferSize = 10;
            target.Append(packet2, 0, packet1.ActualBufferSize);
            target.Position = 8;
            target.TrimBegin();
            Assert.AreEqual(target.position2BufferId.Count, 1);
            Assert.AreEqual(target.position2BufferId[0], packet2.Id);
            entry = new PacketBufferStream_Accessor.BufferEntry(new PrivateObject(target.bufferId2BufferEntry[packet2.Id]));
            Assert.AreEqual(entry.Offset, 6);
            Assert.AreEqual(entry.Size, 4);
            Assert.AreEqual(entry.Buffer, packet2);
        }
    }
}
