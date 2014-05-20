namespace MComms_Transmuxer.Transport
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// This class creates a single large buffer which can be divided up
    /// and assigned to SocketAsyncEventArgs objects for use with each
    /// socket I/O operation.
    /// This enables buffers to be easily reused and guards against
    /// fragmenting heap memory.
    ///
    /// This buffer is a byte array which the Windows TCP buffer can copy its data to.
    /// </summary>
    internal class SocketBufferManager
    {
        /// <summary>
        /// The total number of bytes controlled by the buffer pool
        /// </summary>
        Int32 totalBytesInBufferBlock;

        /// <summary>
        /// Byte array maintained by the Buffer Manager
        /// </summary>
        byte[] bufferBlock;

        /// <summary>
        /// Pool of free blocks
        /// </summary>
        Stack<int> freeIndexPool;

        /// <summary>
        /// Current index in the pool
        /// </summary>
        Int32 currentIndex;

        /// <summary>
        /// Data block size for each SAEA
        /// </summary>
        Int32 bufferBytesAllocatedForEachSaea;

        /// <summary>
        /// Creates new instance of SocketBufferManager
        /// </summary>
        /// <param name="totalBytes">Total size of byte array</param>
        /// <param name="totalBufferBytesInEachSaeaObject">Data block size for each SAEA</param>
        public SocketBufferManager(Int32 totalBytes, Int32 totalBufferBytesInEachSaeaObject)
        {
            totalBytesInBufferBlock = totalBytes;
            this.currentIndex = 0;
            this.bufferBytesAllocatedForEachSaea = totalBufferBytesInEachSaeaObject;
            this.freeIndexPool = new Stack<int>();
            // Create one large buffer block.
            this.bufferBlock = new byte[totalBytesInBufferBlock];
        }

        /// <summary>
        /// Divide that one large buffer block out to each SocketAsyncEventArg object.
        /// Assign a buffer space from the buffer block to the 
        /// specified SocketAsyncEventArgs object.
        /// </summary>
        /// <param name="args">SAEA to assign buffer to</param>
        /// <param name="count">Number of bytes in requested buffer</param>
        /// <returns>True if the buffer was successfully set, else false</returns>
        internal bool SetBuffer(SocketAsyncEventArgs args, int count = 0)
        {
            if (args.Buffer != null)
            {
                // buffer already set
                return true;
            }

            if (count == 0 || count > this.bufferBytesAllocatedForEachSaea)
            {
                count = this.bufferBytesAllocatedForEachSaea;
            }

            lock (this)
            {
                if (this.freeIndexPool.Count > 0)
                {
                    //This if-statement is only true if you have called the FreeBuffer
                    //method previously, which would put an offset for a buffer space 
                    //back into this stack.
                    args.SetBuffer(this.bufferBlock, this.freeIndexPool.Pop(), count);
                }
                else
                {
                    //Inside this else-statement is the code that is used to set the 
                    //buffer for each SAEA object when the pool of SAEA objects is built
                    //in the Init method.
                    if ((totalBytesInBufferBlock - this.bufferBytesAllocatedForEachSaea) < this.currentIndex)
                    {
                        return false;
                    }
                    args.SetBuffer(this.bufferBlock, this.currentIndex, count);
                    this.currentIndex += this.bufferBytesAllocatedForEachSaea;
                }
                return true;
            }
        }

        /// <summary>
        /// Removes the buffer from a SocketAsyncEventArg object.   This frees the
        /// buffer back to the buffer pool. Try NOT to use the FreeBuffer method,
        /// unless you need to destroy the SAEA object, or maybe in the case
        /// of some exception handling. Instead, on the server
        /// keep the same buffer space assigned to one SAEA object for the duration of
        /// this app's running.
        /// </summary>
        /// <param name="args">SAEA to release buffer from</param>
        internal void FreeBuffer(SocketAsyncEventArgs args)
        {
            lock (this)
            {
                this.freeIndexPool.Push(args.Offset);
                args.SetBuffer(null, 0, 0);
            }
        }
    }
}
