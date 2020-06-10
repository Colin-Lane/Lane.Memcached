using System;
using Lane.Memcached.Protocol.Messaging;

namespace Lane.Memcached.Request
{
    public class MemcachedRequest
    {
        public Enums.Opcode Opcode { get; }
        public IMemoryWritable Key { get; }
        public uint Opaque { get; }
        public IMemoryWritable Value { get; set; }
        public TypeCode Flags { get; }
        public TimeSpan? ExpireIn { get; }

        public MemcachedRequest(Enums.Opcode opcode, IMemoryWritable key, uint opaque, IMemoryWritable value, TypeCode flags, TimeSpan ? expireIn=null)
        {
            Opcode = opcode;
            Key = key;
            Opaque = opaque;
            Value = value;
            Flags = flags;
            ExpireIn = expireIn;            
        }

        public MemcachedRequest(Enums.Opcode opcode, IMemoryWritable key, uint opaque)
        {
            Opcode = opcode;
            Key = key;
            Opaque = opaque;
            Value = MemoryWritable.Empty;
        }
    }

    public static class MemoryWritable
    {
        public static readonly IMemoryWritable Empty = new NoOpMemoryWritable();
    }

    public class NoOpMemoryWritable : IMemoryWritable
    {
        public int Length { get; } = 0;
        public void CopyTo(Span<byte> destination)
        {
        }
    }
}
