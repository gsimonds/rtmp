namespace MComms_Transmuxer.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    class PacketBufferAllocator
    {
        private int bufferSize = 0;
        private int bufferCount = 0;
        private volatile int freeBufferCount = 0;
        private List<PacketBuffer> freeBuffers;
        private List<PacketBuffer> lockedBuffers;

        public PacketBufferAllocator(int bufferSize, int bufferCount)
        {
            this.bufferSize = bufferSize;
            this.bufferCount = bufferCount;
            this.freeBufferCount = bufferCount;

            for (int i = 0; i < this.bufferCount; ++i)
            {
                PacketBuffer buffer = new PacketBuffer(this, this.bufferSize);
                this.freeBuffers.Add(buffer);
            }
        }

        public int BufferSize
        {
            get
            {
                return this.bufferSize;
            }
        }

        public int BufferCount
        {
            get
            {
                return this.bufferCount;
            }
        }

        public int FreeBufferCount
        {
            get
            {
                return this.freeBufferCount;
            }
        }

        public void Reallocate(int bufferSize, int bufferCount)
        {
            lock (this)
            {
                this.bufferSize = bufferSize;

                // delete all free buffers
                this.freeBuffers.Clear();

                // allocate buffers with new size
                for (int i = 0; i < this.bufferCount; ++i)
                {
                    PacketBuffer buffer = new PacketBuffer(this, this.bufferSize);
                    this.freeBuffers.Add(buffer);
                }
            }
        }

        public PacketBuffer LockBuffer()
        {
            lock (this)
            {
                if (this.freeBuffers.Count == 0)
                {
                    return null;
                }

                PacketBuffer buffer = this.freeBuffers[0];
                this.freeBuffers.RemoveAt(0);
                this.lockedBuffers.Add(buffer);

                this.freeBufferCount = freeBuffers.Count;

                buffer.AddRef();
                return buffer;
            }
        }

        /// <summary>
        /// This function must not be called directly.
        /// It's intended for internal use by friend PacketBuffer class.
        /// </summary>
        /// <param name="buffer"></param>
        public void ReleaseBuffer(PacketBuffer buffer)
        {
            lock (this)
            {
                if (!this.lockedBuffers.Contains(buffer))
                {
                    return;
                }

                lockedBuffers.Remove(buffer);

                if (buffer.Size != bufferSize)
                {
                    // buffers were re-allocated,
                    // this buffer is an old buffer which must be deleted
                }
                else
                {
                    this.freeBuffers.Add(buffer);
                    this.freeBufferCount = freeBuffers.Count;
                }
            }
        }
    }
}
