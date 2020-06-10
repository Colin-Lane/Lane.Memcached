using System;
using System.Buffers;
using System.Buffers.Binary;
using Bedrock.Framework.Protocols;
using Lane.Memcached.Request;

namespace Lane.Memcached.Protocol.Messaging
{
    public class MemcachedMessageWriter : IMessageWriter<MemcachedRequest>
    {
        public void WriteMessage(MemcachedRequest message, IBufferWriter<byte> output)
        {
            byte extraLength = 0;

            if (message.Flags != TypeCode.Empty)
            {
                extraLength = 8;
            }

            var messageValueLength = message.Value.Length;

            var header = new MemcachedRequestHeader
            {
                KeyLength = (ushort) message.Key.Length,
                Opaque = message.Opaque,
                ExtraLength = extraLength
            };
            header.TotalBodyLength = (uint) (extraLength + header.KeyLength + messageValueLength);

            var writeSpan = output.GetSpan(Constants.HeaderLength + (int)header.TotalBodyLength);
            Span<byte> headerSpan = writeSpan.Slice(0, Constants.HeaderLength);

            if (extraLength != 0)
            {
                header.Extras = (message.Flags, message.ExpireIn);
            }

            headerSpan[0] = MemcachedRequestHeader.Magic;
            headerSpan[1] = (byte) message.Opcode;
            BinaryPrimitives.WriteUInt16BigEndian(headerSpan.Slice(2), header.KeyLength);
            headerSpan[4] = header.ExtraLength;
            headerSpan[5] = header.DataType;
            BinaryPrimitives.WriteUInt16BigEndian(headerSpan.Slice(6), header.VBucket);
            BinaryPrimitives.WriteUInt32BigEndian(headerSpan.Slice(8), header.TotalBodyLength);
            BinaryPrimitives.WriteUInt32BigEndian(headerSpan.Slice(12), header.Opaque);
            BinaryPrimitives.WriteUInt64BigEndian(headerSpan.Slice(16), header.Cas);

            var body = writeSpan.Slice(Constants.HeaderLength, (int) header.TotalBodyLength);
            if (extraLength != 0)
            {
                BinaryPrimitives.WriteUInt32BigEndian(body.Slice(0), (uint) header.Extras.Flags);
                BinaryPrimitives.WriteUInt32BigEndian(body.Slice(4), (uint) header.Extras.Expiration.Value);
                body = body.Slice(extraLength);
            }

            message.Key.CopyTo(body);
            message.Value.CopyTo(body.Slice(header.KeyLength));
            output.Advance(Constants.HeaderLength + (int) header.TotalBodyLength);
        }
    }
}
