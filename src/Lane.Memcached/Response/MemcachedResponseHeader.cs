using System;
using System.Buffers.Binary;
using Lane.Memcached.Request;

namespace Lane.Memcached.Response
{
    public readonly struct MemcachedResponseHeader
    {
        public const byte Magic = 0x81;
        public readonly Enums.Opcode Opcode;
        public readonly ushort KeyLength;
        public readonly byte ExtraLength;
        public readonly byte DataType;
        public readonly Enums.ResponseStatus ResponseStatus;
        public readonly uint TotalBodyLength;
        public readonly uint Opaque;
        public readonly ulong Cas;

        public MemcachedResponseHeader(Enums.Opcode opcode, ushort keyLength, byte extraLength, byte dataType, Enums.ResponseStatus responseStatus, uint totalBodyLength, uint opaque, ulong cas)
        {
            Opcode = opcode;
            KeyLength = keyLength;
            ExtraLength = extraLength;
            DataType = dataType;
            ResponseStatus = responseStatus;
            TotalBodyLength = totalBodyLength;
            Opaque = opaque;
            Cas = cas;
        }

        public MemcachedResponseHeader(ReadOnlySpan<byte> buffer) : this(
            opcode: (Enums.Opcode) buffer[1],
            keyLength: BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(2)),
            extraLength: buffer[4],
            dataType: buffer[5],
            responseStatus: (Enums.ResponseStatus) BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(6)),
            totalBodyLength: BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(8)),
            opaque: BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(12)),
            cas: BinaryPrimitives.ReadUInt64BigEndian(buffer.Slice(16)))
        {
            if (buffer[0] != Magic)
            {
                ThrowMagicMismatchException();
            }
        }

        private static void ThrowMagicMismatchException()
        {
            throw new ArgumentException("Magic mismatch");
        }
    }
}
