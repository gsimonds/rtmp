namespace MComms_Transmuxer.Common
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class PacketBufferStream : Stream
    {
        private long position = 0;
        private long positionKey = 0;
        private long totalLength = 0;
        private Dictionary<long, long> position2BufferId = new Dictionary<long, long>();
        private Dictionary<long, long> bufferId2Offset = new Dictionary<long, long>();
        private Dictionary<long, long> bufferId2Size = new Dictionary<long, long>();
        private Dictionary<long, PacketBuffer> bufferId2Buffer = new Dictionary<long, PacketBuffer>();

        public PacketBufferStream()
        {
        }

        public PacketBufferStream(PacketBuffer packet)
        {
            this.Insert(packet, 0, packet.ActualBufferSize);
        }

        public PacketBufferStream(PacketBuffer packet, int offset, int count)
        {
            this.Insert(packet, offset, count);
        }

        #region Stream implementation

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (PacketBuffer packet in bufferId2Buffer.Values)
                {
                    packet.Release();
                }
                bufferId2Buffer.Clear();
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
                    this.positionKey = this.position2BufferId.Keys.Last<long>(key => key <= this.position);
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

        public override int Read(byte[] buffer, int offset, int count)
        {
            int actuallyRead = 0;

            IEnumerable<long> foundKeys = this.position2BufferId.Keys.Where<long>(key => key >= this.positionKey);
            foreach (long key in foundKeys)
            {
                // update position key
                this.positionKey = key;
                long intPacketOffset = 0;
                if (this.position > this.positionKey)
                {
                    intPacketOffset = this.position - this.positionKey;
                }

                long bufferId = this.position2BufferId[key];
                PacketBuffer packet = this.bufferId2Buffer[bufferId];
                long packetSize = this.bufferId2Size[bufferId];
                long packetOffset = this.bufferId2Offset[bufferId];

                if (intPacketOffset >= packetSize)
                {
                    // nothing to read in current packet
                    continue;
                }

                int copySize = (int)Math.Min(count, packetSize - intPacketOffset);
                Array.Copy(packet.Buffer, packetOffset + intPacketOffset, buffer, offset, copySize);

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
            int actuallyWritten = 0;

            IEnumerable<long> foundKeys = this.position2BufferId.Keys.Where<long>(key => key >= this.positionKey);
            foreach (long key in foundKeys)
            {
                // update position key
                this.positionKey = key;
                long intPacketOffset = 0;
                if (this.position > this.positionKey)
                {
                    intPacketOffset = this.position - this.positionKey;
                }

                long bufferId = this.position2BufferId[key];
                PacketBuffer packet = this.bufferId2Buffer[bufferId];
                long packetSize = this.bufferId2Size[bufferId];
                long packetOffset = this.bufferId2Offset[bufferId];

                if (intPacketOffset >= packetSize)
                {
                    // nothing to write in current packet
                    continue;
                }

                int copySize = (int)Math.Min(count, packetSize - intPacketOffset);
                Array.Copy(buffer, actuallyWritten, packet.Buffer, packetOffset + intPacketOffset, copySize);

                actuallyWritten += copySize;
                if (actuallyWritten == count)
                {
                    break;
                }
            }

            this.position += actuallyWritten;
        }

        #endregion

        #region PacketBufferStream specific methods

        public void Insert(PacketBuffer buffer, int offset, int count)
        {
            if (this.position == this.totalLength)
            {
                this.position2BufferId.Add(this.totalLength, buffer.Id);
                this.bufferId2Offset.Add(buffer.Id, offset);
                this.bufferId2Size.Add(buffer.Id, count);
                this.bufferId2Buffer.Add(buffer.Id, buffer);
                buffer.AddRef();
                this.positionKey = this.totalLength;
                this.totalLength += count;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public void CopyTo(PacketBufferStream buffer, int count)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Drop all data from the beginning of the stream till current position
        /// </summary>
        public void TrimBegin()
        {
            IEnumerable<long> foundKeys = this.position2BufferId.Keys.Where<long>(key => key < this.positionKey);
            long removedLength = 0;

            foreach (long key in foundKeys)
            {
                long bufferId = this.position2BufferId[key];

                // accumulate removed length
                removedLength += this.bufferId2Size[bufferId];

                // release the buffer
                this.bufferId2Buffer[bufferId].Release();

                // clean up data
                this.position2BufferId.Remove(key);
                this.bufferId2Offset.Remove(bufferId);
                this.bufferId2Size.Remove(bufferId);
                this.bufferId2Buffer.Remove(bufferId);
            }

            // adjust offset and size of current buffer
            if (removedLength < this.position)
            {
                long gap = this.position - removedLength;
                long bufferId = this.position2BufferId[this.positionKey];
                this.bufferId2Size[bufferId] -= gap;
                this.bufferId2Offset[bufferId] += gap;

                if (this.bufferId2Size[bufferId] == 0)
                {
                    // free completely used buffer
                    this.bufferId2Buffer[bufferId].Release();
                    this.position2BufferId.Remove(this.positionKey);
                    this.bufferId2Offset.Remove(bufferId);
                    this.bufferId2Size.Remove(bufferId);
                    this.bufferId2Buffer.Remove(bufferId);
                }

                removedLength = this.position;
            }

            // adjust positions of remaining packets
            foundKeys = this.position2BufferId.Keys.Where<long>(key => key > this.positionKey);

            foreach (long key in foundKeys)
            {
                long bufferId = this.position2BufferId[key];
                this.position2BufferId.Remove(key);
                this.position2BufferId.Add(key - removedLength, bufferId);
            }

            this.totalLength -= removedLength;
            this.Position = 0;
            this.positionKey = 0;
        }

        #endregion
    }
}
