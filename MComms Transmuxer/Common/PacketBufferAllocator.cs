namespace MComms_Transmuxer.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class PacketBufferAllocator
    {
        private int bufferSize = 0;
        private int bufferCount = 0;
        private volatile int freeBufferCount = 0;
        private List<PacketBuffer> freeBuffers = new List<PacketBuffer>();
        private List<PacketBuffer> lockedBuffers = new List<PacketBuffer>();

#if ALLOCATOR_USAGE_STAT
        // statistics
        private int maxLockedBuffers = 0;
        private long lockedBuffersTotal = 0;
        private long lockedBuffersCount = 0;
        private long usedBuffersSize = 0;
        private long usedBuffersCount = 0;
#endif

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
                Global.Log.WarnFormat("Re-allocating buffers size {0} count {1}", bufferSize, bufferCount);

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
                    int addBufferCount = this.bufferCount / 10;
                    if (addBufferCount == 0) ++addBufferCount;
                    Global.Log.WarnFormat("Allocator[{0}/{1}]: No more empty buffers, allocate additional {2} buffers", this.bufferSize, this.bufferCount, addBufferCount);

                    for (int i = 0; i < addBufferCount; ++i)
                    {
                        this.freeBuffers.Add(new PacketBuffer(this, this.bufferSize));
                    }

                    this.bufferCount += addBufferCount;
                    this.freeBufferCount = addBufferCount;
                }

                PacketBuffer buffer = this.freeBuffers[this.freeBuffers.Count - 1];
                this.freeBuffers.RemoveAt(this.freeBuffers.Count - 1);
                this.lockedBuffers.Add(buffer);

#if ALLOCATOR_USAGE_STAT
                if (this.lockedBuffers.Count > this.maxLockedBuffers)
                {
                    this.maxLockedBuffers = this.lockedBuffers.Count;
                }

                this.lockedBuffersTotal += this.lockedBuffers.Count;
                this.lockedBuffersCount++;

                if (this.bufferCount > 1000 && this.lockedBuffersCount % (this.bufferCount * 10) == 0)
                {
                    Global.Log.DebugFormat("Allocator[{0}/{1}]: avg locked buffers {2}, max locked buffers {3}", this.bufferSize, this.bufferCount, (this.lockedBuffersTotal / this.lockedBuffersCount), this.maxLockedBuffers);
                }
#endif

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

#if ALLOCATOR_USAGE_STAT
                this.usedBuffersSize += buffer.ActualBufferSize;
                this.usedBuffersCount++;

                if (this.bufferCount > 1000 && this.usedBuffersCount % (this.bufferCount * 10) == 0)
                {
                    Global.Log.DebugFormat("Allocator[{0}/{1}]: avg buffer usage {2:0.00}%", this.bufferSize, this.bufferCount, ((double)this.usedBuffersSize / this.bufferSize / this.usedBuffersCount) * 100.0, this.maxLockedBuffers);
                }
#endif

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
