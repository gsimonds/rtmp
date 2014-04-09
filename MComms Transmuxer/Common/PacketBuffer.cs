namespace MComms_Transmuxer.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// General purpose data packet
    /// </summary>
    public class PacketBuffer
    {
        private static object idCounterLock = new object();
        private static long idCounter = 0;

        private PacketBufferAllocator allocator = null;
        private int refCount = 0;
        private int bufferSize = 0;
        private int actualBufferSize = 0;
        private int position = 0;
        private byte[] buffer = null;
        private long id = 0;

        public PacketBuffer(PacketBufferAllocator allocator, int bufferSize)
        {
            lock (PacketBuffer.idCounterLock)
            {
                this.id = PacketBuffer.idCounter++;
            }

            this.allocator = allocator;
            this.bufferSize = bufferSize;
            this.buffer = new byte[bufferSize];
        }

        public long Id
        {
            get
            {
                return this.id;
            }
        }

        public byte[] Buffer
        {
            get
            {
                return buffer;
            }
        }

        public int Size
        {
            get
            {
                return this.bufferSize;
            }
        }

        public int ActualBufferSize
        {
            get
            {
                return this.actualBufferSize;
            }
            set
            {
                this.actualBufferSize = value;
            }
        }

        public int Position
        {
            get
            {
                return this.position;
            }
            set
            {
                this.position = value;
            }
        }

        public int AddRef()
        {
            lock (this)
            {
                return ++this.refCount;
            }
        }

        public int Release()
        {
            lock (this)
            {
                if (--this.refCount == 0)
                {
                    this.CleanUp();
                    this.allocator.ReleaseBuffer(this);
                }
                return this.refCount;
            }
        }

        public void CleanUp()
        {
            this.actualBufferSize = 0;
            this.position = 0;
        }
    }
}
