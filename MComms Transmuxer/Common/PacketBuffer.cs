namespace MComms_Transmuxer.Common
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.CompilerServices;
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

#if TRACE_PACKET_BUFFERS
        private List<KeyValuePair<DateTime, string>> refdBy = new List<KeyValuePair<DateTime, string>>();
        private List<KeyValuePair<DateTime, string>> releasedBy = new List<KeyValuePair<DateTime, string>>();
#endif

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

        public void CleanUp()
        {
            this.actualBufferSize = 0;
            this.position = 0;
        }

#if TRACE_PACKET_BUFFERS

        /// <summary>
        /// Converts calling method name to string
        /// </summary>
        /// <returns>Method name</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private string GetCallingMethod()
        {
            StackTrace st = new StackTrace();
            try
            {
                StackFrame sf = st.GetFrame(2);
                string s = sf.GetMethod().Name;
                if (s == "LockBuffer")
                {
                    sf = st.GetFrame(3);
                    s = sf.GetMethod().Name;
                }
                return s;
            }
            catch
            {
                return "Unknown";
            }
        }

#endif

        public int AddRef()
        {
            lock (this)
            {
#if TRACE_PACKET_BUFFERS
                this.refdBy.Add(new KeyValuePair<DateTime, string>(DateTime.Now, this.GetCallingMethod()));
#endif
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
#if TRACE_PACKET_BUFFERS
                    this.refdBy.Clear();
                    this.releasedBy.Clear();
#endif
                    this.allocator.ReleaseBuffer(this);
                }
#if TRACE_PACKET_BUFFERS
                else
                {
                    this.releasedBy.Add(new KeyValuePair<DateTime, string>(DateTime.Now, this.GetCallingMethod()));
                }
#endif
                return this.refCount;
            }
        }
    }
}
