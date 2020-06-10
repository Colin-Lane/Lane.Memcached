using System;
using System.Buffers;

namespace Lane.Memcached.MessageQueue
{
    public interface IMemcachedResponse
    {
        bool TryParseMessage(in ReadOnlySequence<byte> input, ref SequencePosition consumed,
            ref SequencePosition examined);

        void SetException(Exception exception);
    }
}