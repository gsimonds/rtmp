namespace MComms_Transmuxer.Common
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class PacketBufferStream : Stream
    {
        private class BufferEntry
        {
            public int Offset { get; set; }
            public int Size { get; set; }
            public PacketBuffer Buffer { get; set; }
        }

        private long position = 0;
        private long positionKey = 0;
        private long totalLength = 0;
        private SortedList<long, long> position2BufferId = new SortedList<long, long>();
        private Hashtable bufferId2BufferEntry = new Hashtable();
        private BufferEntry singleEntry = null;

        public PacketBufferStream()
        {
        }

        public PacketBufferStream(PacketBuffer packet)
        {
            this.Append(packet, 0, packet.ActualBufferSize);
        }

        public PacketBufferStream(PacketBuffer packet, int offset, int count)
        {
            this.Append(packet, offset, count);
        }

        #region Stream implementation

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (BufferEntry entry in bufferId2BufferEntry.Values)
                {
                    entry.Buffer.Release();
                }
                bufferId2BufferEntry.Clear();
            }

            base.Dispose(disposing);
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override long Length
        {
            get { return this.totalLength; }
        }

        public override long Position
        {
            get
            {
                return this.position;
            }
            set
            {
                if (value < 0 || value > this.totalLength)
                {
                    throw new ArgumentOutOfRangeException();
                }
                this.position = value;
                if (this.position2BufferId.Count > 0)
                {
                    try
                    {
                        int index = this.position2BufferId.FindFirstIndexLessThanOrEqualTo(this.position);
                        if (index < 0)
                        {
                            Global.Log.ErrorFormat("Unexpected binary search result {0}", index);
                            index = 0;
                        }
                        this.positionKey = this.position2BufferId.Keys[index];
                    }
                    catch (Exception ex)
                    {
                        Global.Log.Error("Setting this.positionKey exception", ex);
                    }
                }
                else
                {
                    this.positionKey = 0;
                }
            }
        }

        public override void Flush()
        {
            // nothing to do
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    this.Position = offset;
                    break;
                case SeekOrigin.Current:
                    this.Position += offset;
                    break;
                case SeekOrigin.End:
                    this.Position = this.totalLength - offset;
                    break;
            }

            return this.position;
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override int ReadByte()
        {
            int value = 0;

            if (this.singleEntry != null)
            {
                // optimization for single-buffer case
                long intPacketOffset = 0;
                if (this.position > this.positionKey)
                {
                    intPacketOffset = this.position - this.positionKey;
                }

                if (intPacketOffset < this.singleEntry.Size)
                {
                    value = this.singleEntry.Buffer.Buffer[this.singleEntry.Offset + intPacketOffset];
                }
            }
            else
            {
                for (int i = this.position2BufferId.Keys.IndexOf(this.positionKey); i < this.position2BufferId.Count; ++i)
                {
                    long key = this.position2BufferId.Keys[i];

                    // update position key
                    this.positionKey = key;
                    long intPacketOffset = 0;
                    if (this.position > this.positionKey)
                    {
                        intPacketOffset = this.position - this.positionKey;
                    }

                    long bufferId = this.position2BufferId[key];
                    BufferEntry entry = (BufferEntry)this.bufferId2BufferEntry[bufferId];

                    if (intPacketOffset >= entry.Size)
                    {
                        // nothing to read in current packet
                        continue;
                    }

                    value = entry.Buffer.Buffer[entry.Offset + intPacketOffset];
                    break;
                }
            }

            this.position++;
            return value;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int actuallyRead = 0;

            for (int i = this.position2BufferId.Keys.IndexOf(this.positionKey); i < this.position2BufferId.Count; ++i)
            {
                long key = this.position2BufferId.Keys[i];

                // update position key
                this.positionKey = key;
                long intPacketOffset = 0;
                if (this.position > this.positionKey)
                {
                    intPacketOffset = this.position - this.positionKey;
                }

                long bufferId = this.position2BufferId[key];
                BufferEntry entry = (BufferEntry)this.bufferId2BufferEntry[bufferId];

                if (intPacketOffset >= entry.Size)
                {
                    // nothing to read in current packet
                    continue;
                }

                int copySize = (int)Math.Min(count - actuallyRead, entry.Size - intPacketOffset);
                if (count == 1 && copySize == 1)
                {
                    // optimization for byte read
                    buffer[offset] = entry.Buffer.Buffer[entry.Offset + intPacketOffset];
                }
                else
                {
                    Array.Copy(entry.Buffer.Buffer, entry.Offset + intPacketOffset, buffer, offset + actuallyRead, copySize);
                }

                actuallyRead += copySize;
                if (actuallyRead == count)
                {
                    break;
                }
            }

            this.position += actuallyRead;
            return actuallyRead;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (this.singleEntry != null)
            {
                // optimization for single-buffer case
                long intPacketOffset = 0;
                if (this.position > this.positionKey)
                {
                    intPacketOffset = this.position - this.positionKey;
                }

                if (intPacketOffset >= this.singleEntry.Size)
                {
                    // no more space to write
                    return;
                }

                int copySize = (int)Math.Min(count, this.singleEntry.Size - intPacketOffset);
                Array.Copy(buffer, offset, this.singleEntry.Buffer.Buffer, this.singleEntry.Offset + intPacketOffset, copySize);

                this.position += copySize;
            }
            else
            {
                int actuallyWritten = 0;

                for (int i = this.position2BufferId.Keys.IndexOf(this.positionKey); i < this.position2BufferId.Count; ++i)
                {
                    long key = this.position2BufferId.Keys[i];

                    // update position key
                    this.positionKey = key;
                    long intPacketOffset = 0;
                    if (this.position > this.positionKey)
                    {
                        intPacketOffset = this.position - this.positionKey;
                    }

                    long bufferId = this.position2BufferId[key];
                    BufferEntry entry = (BufferEntry)this.bufferId2BufferEntry[bufferId];

                    if (intPacketOffset >= entry.Size)
                    {
                        // nothing to write in current packet
                        continue;
                    }

                    int copySize = (int)Math.Min(count - actuallyWritten, entry.Size - intPacketOffset);
                    Array.Copy(buffer, offset + actuallyWritten, entry.Buffer.Buffer, entry.Offset + intPacketOffset, copySize);

                    actuallyWritten += copySize;
                    if (actuallyWritten == count)
                    {
                        break;
                    }
                }

                this.position += actuallyWritten;
            }
        }

        #endregion

        #region PacketBufferStream specific properties and methods

        public bool OneMessageStream { get; set; }

        public PacketBuffer FirstPacketBuffer
        {
            get
            {
                if (this.bufferId2BufferEntry.Count == 0)
                {
                    return null;
                }
                else if (this.singleEntry != null)
                {
                    return this.singleEntry.Buffer;
                }
                else
                {
                    IDictionaryEnumerator denum = this.bufferId2BufferEntry.GetEnumerator();
                    return denum.MoveNext() ? ((BufferEntry)(((DictionaryEntry)denum.Current).Value)).Buffer : null;
                }
            }
        }

        public void Append(PacketBuffer buffer, int offset, int count)
        {
            if (this.position != this.totalLength)
            {
                this.Seek(0, SeekOrigin.End);
            }

            buffer.AddRef();

            this.position2BufferId.Add(this.totalLength, buffer.Id);
            BufferEntry entry = new BufferEntry { Offset = offset, Size = count, Buffer = buffer };
            this.bufferId2BufferEntry.Add(buffer.Id, entry);

            if (this.bufferId2BufferEntry.Count == 1)
            {
                this.singleEntry = entry;
            }
            else
            {
                this.singleEntry = null;
            }

            this.positionKey = this.totalLength;
            this.totalLength += count;

            //Global.Log.DebugFormat("Added buffer {0}, size {1}, offset {2}", buffer.Id, count, offset);
        }

        /// <summary>
        /// Copies specified number of bytes from current position to specified stream.
        /// </summary>
        /// <param name="stream">Target stream receiving the data</param>
        /// <param name="count">Number of bytes to copy</param>
        /// <returns>Number of bytes actually copied</returns>
        public int CopyTo(PacketBufferStream stream, int count)
        {
            int actuallyCopied = 0;

            if (this.singleEntry != null)
            {
                // optimization for single-buffer case
                long intPacketOffset = 0;
                if (this.position > this.positionKey)
                {
                    intPacketOffset = this.position - this.positionKey;
                }

                if (intPacketOffset < this.singleEntry.Size)
                {
                    int copySize = (int)Math.Min(count, this.singleEntry.Size - intPacketOffset);
                    stream.Write(this.singleEntry.Buffer.Buffer, (int)(this.singleEntry.Offset + intPacketOffset), copySize);

                    actuallyCopied += copySize;
                }
            }
            else
            {
                for (int i = this.position2BufferId.Keys.IndexOf(this.positionKey); i < this.position2BufferId.Count; ++i)
                {
                    long key = this.position2BufferId.Keys[i];

                    // update position key
                    this.positionKey = key;
                    long intPacketOffset = 0;
                    if (this.position > this.positionKey)
                    {
                        intPacketOffset = this.position - this.positionKey;
                    }

                    long bufferId = this.position2BufferId[key];
                    BufferEntry entry = (BufferEntry)this.bufferId2BufferEntry[bufferId];

                    if (intPacketOffset >= entry.Size)
                    {
                        // nothing to read in current packet
                        continue;
                    }

                    int copySize = (int)Math.Min(count - actuallyCopied, entry.Size - intPacketOffset);
                    stream.Write(entry.Buffer.Buffer, (int)(entry.Offset + intPacketOffset), copySize);

                    actuallyCopied += copySize;
                    if (actuallyCopied == count)
                    {
                        break;
                    }
                }
            }

            this.position += actuallyCopied;
            return actuallyCopied;
        }

        /// <summary>
        /// Drop all data from the beginning of the stream till current position
        /// </summary>
        public void TrimBegin()
        {
            //Global.Log.DebugFormat("Pos {0}, key {1}", this.Position, this.positionKey);

            long removedLength = 0;

            while (this.position2BufferId.Count > 0 && this.position2BufferId.Keys[0] < this.positionKey)
            {
                long key = this.position2BufferId.Keys[0];

                long bufferId = this.position2BufferId[key];
                BufferEntry entry = (BufferEntry)this.bufferId2BufferEntry[bufferId];

                // accumulate removed length
                removedLength += entry.Size;

                // release the buffer
                entry.Buffer.Release();

                // clean up data
                this.position2BufferId.Remove(key);
                this.bufferId2BufferEntry.Remove(bufferId);
            }

            // adjust offset and size of current buffer
            if (removedLength < this.position)
            {
                long gap = this.position - removedLength;
                long bufferId = this.position2BufferId[this.positionKey];
                BufferEntry entry = (BufferEntry)this.bufferId2BufferEntry[bufferId];

                entry.Size -= (int)gap;
                entry.Offset += (int)gap;

                this.position2BufferId.Remove(this.positionKey);

                if (entry.Size == 0)
                {
                    // free completely used buffer
                    entry.Buffer.Release();
                    this.bufferId2BufferEntry.Remove(bufferId);
                }
                else
                {
                    // re-insert with new position
                    this.position2BufferId.Add(0, bufferId);
                }

                removedLength = this.position;
            }
            else
            {
                if (this.positionKey < this.totalLength)
                {
                    // re-insert with new position
                    long bufferId = this.position2BufferId[this.positionKey];
                    this.position2BufferId.Remove(this.positionKey);
                    this.position2BufferId.Add(0, bufferId);
                }
            }

            // adjust positions of remaining packets
            for (int i = this.position2BufferId.FindFirstIndexGreaterThan(this.positionKey); i < this.position2BufferId.Count; ++i)
            {
                long key = this.position2BufferId.Keys[i];
                long bufferId = this.position2BufferId[key];
                this.position2BufferId.Remove(key);
                this.position2BufferId.Add(key - removedLength, bufferId);
            }

            if (this.bufferId2BufferEntry.Count == 1)
            {
                IDictionaryEnumerator denum = this.bufferId2BufferEntry.GetEnumerator();
                this.singleEntry = denum.MoveNext() ? (BufferEntry)((DictionaryEntry)denum.Current).Value : null;
            }
            else
            {
                this.singleEntry = null;
            }

            this.totalLength -= removedLength;
            this.positionKey = 0;
            this.Position = 0;
        }

        #endregion
    }
}
