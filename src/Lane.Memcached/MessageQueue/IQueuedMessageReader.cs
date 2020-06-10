using System;
using Bedrock.Framework.Protocols;

namespace Lane.Memcached.MessageQueue
{
    public interface IQueuedMessageReader<in TQueueItem> : IMessageReader<bool>
    {
        bool IsEmpty { get; }
        void FailQueue(Exception ex);
        void Enqueue(TQueueItem queuedItem);
    }
}