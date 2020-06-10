using System;
using System.Buffers;
using System.Buffers.Binary;

namespace Lane.Memcached.Response
{
    public ref struct MemcachedResponse
    {
        public ReadOnlySequence<byte> Data { get; private set; }
        public TypeCode Flags { get; set; }
        public MemcachedResponseHeader Header { get; private set; }

        public MemcachedResponse(MemcachedResponseHeader header) : this()
        {
            Header = header;
        }

        public void ReadHeader(ReadOnlySpan<byte> buffer)
        {
            Header = new MemcachedResponseHeader(buffer);
        }

        public static MemcachedResponse Parse(ref ReadOnlySpan<byte> buffer)
        {
            return default;
        }

        public void ReadBody(ReadOnlySequence<byte> sequence)
        {
            if (sequence.Length == 0)
            {
                return;
            }

            Data = sequence.Slice(Header.KeyLength + Header.ExtraLength);

            Span<byte> buffer = stackalloc byte[4];
            sequence.Slice(0, 4).CopyTo(buffer);
            Flags = (TypeCode)BinaryPrimitives.ReadUInt32BigEndian(buffer);            
        }
    }
}
