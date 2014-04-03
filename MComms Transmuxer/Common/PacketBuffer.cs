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
    class PacketBuffer
    {
        private PacketBufferAllocator allocator = null;
        private int refCount = 0;
        private int bufferSize = 0;
        private int actualBufferSize = 0;
        private byte[] buffer = null;

        public PacketBuffer(PacketBufferAllocator allocator, int bufferSize)
        {
            this.allocator = allocator;
            this.bufferSize = bufferSize;
            this.buffer = new byte[bufferSize];
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
        }
    }
}
