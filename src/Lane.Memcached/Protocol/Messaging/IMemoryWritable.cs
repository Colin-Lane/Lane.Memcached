using System;

namespace Lane.Memcached.Protocol.Messaging
{
    public interface IMemoryWritable
    {
        int Length { get; }
        void CopyTo(Span<byte> destination);
    }
}