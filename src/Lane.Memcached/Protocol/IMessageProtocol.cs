using System;

namespace Lane.Memcached.Protocol
{
    public interface IMessageProtocol<in TMessage, in TResponse> : IAsyncDisposable
    {
        void StartProcessingQueue();
        void Enqueue(TMessage request, TResponse response);
    }
}