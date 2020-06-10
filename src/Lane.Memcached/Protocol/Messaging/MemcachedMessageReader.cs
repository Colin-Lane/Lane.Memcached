using System;
using System.Buffers;
using System.Collections.Concurrent;
using Lane.Memcached.MessageQueue;

namespace Lane.Memcached.Protocol.Messaging
{
    public class MemcachedMessageReader: IQueuedMessageReader<IMemcachedResponse>
    {
        private readonly ConcurrentQueue<IMemcachedResponse> _readQueue;

        public MemcachedMessageReader()
        {
            _readQueue = new ConcurrentQueue<IMemcachedResponse>();
        }

        public void Enqueue(IMemcachedResponse readResult) => _readQueue.Enqueue(readResult);

        public void FailQueue(Exception ex)
        {
            while (_readQueue.TryDequeue(out var task))
            {
                task.SetException(ex);
            }
        }

        public bool IsEmpty => _readQueue.Count == 0;

        public bool TryParseMessage(in ReadOnlySequence<byte> input, ref SequencePosition consumed,
            ref SequencePosition examined, out bool message)
        {
            message = true;
            while (_readQueue.TryPeek(out var task))
            {
                try
                {
                    if (!task.TryParseMessage(input.Slice(consumed), ref consumed, ref examined))
                    {
                        return false;
                    }
                    _readQueue.TryDequeue(out _);
                }
                catch (Exception ex)
                {
                    task.SetException(ex);
                    FailQueue(ex);
                }
            }

            return true;
        }
    }
}
