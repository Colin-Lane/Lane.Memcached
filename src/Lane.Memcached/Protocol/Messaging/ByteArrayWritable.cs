using System;

namespace Lane.Memcached.Protocol.Messaging
{
    public class ByteArrayWritable: IMemoryWritable
    {
        private readonly byte[] _bytes;

        public ByteArrayWritable(byte[] bytes)
        {
            _bytes = bytes ?? Array.Empty<byte>();
            Length = _bytes.Length;
        }

        public int Length { get; }
        public void CopyTo(Span<byte> destination)
        {
            _bytes.CopyTo(destination);
        }
    }
}