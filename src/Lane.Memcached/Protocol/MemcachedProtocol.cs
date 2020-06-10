using System;
using System.Threading;
using System.Threading.Tasks;
using Lane.Memcached.MessageQueue;
using Lane.Memcached.Protocol.Messaging;
using Lane.Memcached.Request;
using Lane.Memcached.Response;
using Microsoft.AspNetCore.Connections;

namespace Lane.Memcached.Protocol
{
    public class MemcachedProtocol: IAsyncDisposable
    {
        private readonly IMessageProtocol<MemcachedRequest, IMemcachedResponse> _messageProtocol;

        private int _previousOpaque;
        private uint NextOpaque => (uint)Interlocked.Increment(ref _previousOpaque);

        public MemcachedProtocol(ConnectionContext connection) : this(
            new LowLatencyMessageProtocol<MemcachedRequest, IMemcachedResponse>(connection,
                new MemcachedMessageWriter(), new MemcachedMessageReader()))
        {}

        public MemcachedProtocol(IMessageProtocol<MemcachedRequest, IMemcachedResponse> protocol)
        {
            _messageProtocol = protocol;
        }

        private Task CommandWithNoResult(MemcachedRequest request)
        {
            return ExecuteCommand<bool, NoResponseDeserializer>(request, new NoResponseDeserializer());
        }

        public Task<T> Get<T, TD>(string key, TD deserializer) where TD: IResponseDeserializer<T>
        {
            return ExecuteCommand<T, TD>(new MemcachedRequest(Enums.Opcode.Get, new StringWritable(key), NextOpaque), deserializer);
        }

        public Task Delete(string key)
        {
            var request = new MemcachedRequest(Enums.Opcode.Delete, new StringWritable(key), NextOpaque);

            return CommandWithNoResult(request);
        }

        public Task Set(string key, byte[] value, TimeSpan? expireIn)
        {
            var request = new MemcachedRequest(Enums.Opcode.Set, new StringWritable(key), NextOpaque, new ByteArrayWritable(value), TypeCode.Object, expireIn);

            return CommandWithNoResult(request);          
        }

        public Task Set(string key, string value, TimeSpan? expireIn)
        {
            var request = new MemcachedRequest(Enums.Opcode.Set, new StringWritable(key), NextOpaque, new StringWritable(value), TypeCode.Object, expireIn);

            return CommandWithNoResult(request);
        }

        public Task Add(string key, byte[] value, TimeSpan? expireIn)
        {
            var request = new MemcachedRequest(Enums.Opcode.Add, new StringWritable(key), NextOpaque, new ByteArrayWritable(value), TypeCode.Object, expireIn);

            return CommandWithNoResult(request);
        }

        public Task Replace(IMemoryWritable key, IMemoryWritable value, TimeSpan? expireIn)
        {
            var request = new MemcachedRequest(Enums.Opcode.Replace, key, NextOpaque, value, TypeCode.Object, expireIn);

            return CommandWithNoResult(request);
        }

        public Task Replace(string key, byte[] value, TimeSpan? expireIn)
        {
            return Replace(new StringWritable(key), new ByteArrayWritable(value), expireIn);
        }

        private Task<T> ExecuteCommand<T, TD>(MemcachedRequest request, TD deserializer) where TD: IResponseDeserializer<T>
        {
            var response = new AsyncResponse<T, TD>(deserializer);
            _messageProtocol.Enqueue(request, response);
            return response.Task;
        }

        public void Start() => _messageProtocol.StartProcessingQueue();

        public ValueTask DisposeAsync()
        {
            return _messageProtocol.DisposeAsync();
        }
    }
}
