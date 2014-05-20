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
        #region Private constants and fields

        /// <summary>
        /// Static lock for id counter
        /// </summary>
        private static object idCounterLock = new object();

        /// <summary>
        /// Id counter used to assign unique id for every PacketBuffer object
        /// </summary>
        private static long idCounter = 0;

        /// <summary>
        /// Parent allocator which owns current packet
        /// </summary>
        private PacketBufferAllocator allocator = null;

        /// <summary>
        /// Object refernce count
        /// </summary>
        private int refCount = 0;

        /// <summary>
        /// Total buffer size
        /// </summary>
        private int bufferSize = 0;

        /// <summary>
        /// Currently used buffer size
        /// </summary>
        private int actualBufferSize = 0;

        /// <summary>
        /// Byte buffer
        /// </summary>
        private byte[] buffer = null;

        /// <summary>
        /// Object unique id
        /// </summary>
        private long id = 0;

        /// <summary>
        /// Current position in a buffer.
        /// This is just a cursor used by some long lasting operations to store current state.
        /// </summary>
        private int position = 0;

#if TRACE_PACKET_BUFFERS
        /// <summary>
        /// List of functions referenced current packet
        /// </summary>
        private List<KeyValuePair<DateTime, string>> refdBy = new List<KeyValuePair<DateTime, string>>();

        /// <summary>
        /// List of functions released current packet
        /// </summary>
        private List<KeyValuePair<DateTime, string>> releasedBy = new List<KeyValuePair<DateTime, string>>();
#endif

        #endregion

        #region Constructor

        /// <summary>
        /// Creates the instance of PacketBuffer owned by specified allocator
        /// with the specified buffer size
        /// </summary>
        /// <param name="allocator">Owner</param>
        /// <param name="bufferSize">Buffer size to create</param>
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

        #endregion

        #region Public properties and methods

        /// <summary>
        /// Packet unique identifier
        /// </summary>
        public long Id
        {
            get
            {
                return this.id;
            }
        }

        /// <summary>
        /// Byte buffer
        /// </summary>
        public byte[] Buffer
        {
            get
            {
                return buffer;
            }
        }

        /// <summary>
        /// Total buffer size
        /// </summary>
        public int Size
        {
            get
            {
                return this.bufferSize;
            }
        }

        /// <summary>
        /// Currently used buffer size
        /// </summary>
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

        /// <summary>
        /// Current position in a buffer.
        /// This is just a cursor used by some long lasting operations to store current state.
        /// </summary>
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

        /// <summary>
        /// Reset packet to "empty" state
        /// </summary>
        public void CleanUp()
        {
            this.actualBufferSize = 0;
            this.position = 0;
        }

        /// <summary>
        /// Add reference
        /// </summary>
        /// <returns>Current reference count</returns>
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

        /// <summary>
        /// Release reference
        /// </summary>
        /// <returns>Current reference count</returns>
        public int Release()
        {
            lock (this)
            {
                if (--this.refCount == 0)
                {
                    this.allocator.ReleaseBuffer(this);
                    this.CleanUp();
#if TRACE_PACKET_BUFFERS
                    this.refdBy.Clear();
                    this.releasedBy.Clear();
#endif
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

        #endregion

        #region Private methods

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
                System.Reflection.MethodBase method = sf.GetMethod();
                if (method.Name == "LockBuffer")
                {
                    sf = st.GetFrame(3);
                    method = sf.GetMethod();
                }
                return method.DeclaringType.Name + "." + method.Name;
            }
            catch
            {
                return "Unknown";
            }
        }

#endif

        #endregion
    }
}
