using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Bedrock.Framework.Protocols;
using Lane.Memcached.MessageQueue;
using Microsoft.AspNetCore.Connections;

namespace Lane.Memcached.Protocol
{
    public class LowLatencyMessageProtocol<TMessage, TResponse>: IMessageProtocol<TMessage, TResponse>
    {
        private readonly ConnectionContext _connection;
        private readonly IMessageWriter<TMessage> _messageWriter;
        private readonly IQueuedMessageReader<TResponse> _messageReader;
        private readonly ProtocolReader _protocolReader;
        private readonly ProtocolWriter _protocolWriter;

        private readonly BufferBlock<(TMessage, TResponse)> _requestQueue = new BufferBlock<(TMessage, TResponse)>();

        public LowLatencyMessageProtocol(ConnectionContext connection, IMessageWriter<TMessage> messageWriter, IQueuedMessageReader<TResponse> messageReader)
        {
            _connection = connection;
            _messageWriter = messageWriter;
            _messageReader = messageReader;
            _protocolReader = connection.CreateReader();
            _protocolWriter = connection.CreateWriter();
        }

        public ValueTask DisposeAsync()
        {
            _requestQueue.Complete();
            _messageReader.FailQueue(new ObjectDisposedException("connection has been disposed"));
            return _connection.DisposeAsync();
        }

        public void StartProcessingQueue()
        {
            Task.Run(WriteTask);
            Task.Run(ReadTask);
        }

        private async Task WriteTask()
        {
            try
            {
                var list = new List<(TMessage, TResponse)>();
                var list2 = new List<(TMessage, TResponse)>();
                var activeList = list;
                ValueTask writeTask = new ValueTask(Task.CompletedTask);

                var isCompleted = _requestQueue.Completion;
                while (!isCompleted.IsCompletedSuccessfully)
                {
                    while (_requestQueue.TryReceive(out var item))
                    {
                        activeList.Add(item);
                    }

                    if (activeList.Count == 0)
                    {
                        activeList.Add(await _requestQueue.ReceiveAsync());
                    }

                    foreach (var item in activeList)
                    {
                        // enqueue reads
                        _messageReader.Enqueue(item.Item2);
                    }
                    if (!writeTask.IsCompletedSuccessfully)
                        await writeTask;
                    writeTask = _protocolWriter.WriteManyAsync(_messageWriter, activeList.Select(x => x.Item1));
                    activeList = activeList == list ? list2 : list;
                    activeList.Clear();
                }
            }
            catch (Exception ex)
            {
                _requestQueue.Complete();
                _messageReader.FailQueue(ex);
                await _connection.DisposeAsync();
            }
        }

        private async Task ReadTask()
        {
            var isCompleted = _requestQueue.Completion;
            while (!isCompleted.IsCompletedSuccessfully)
            {
                try
                {
                    var result = _protocolReader.ReadAsync<bool>(_messageReader);
                    if (!result.IsCompletedSuccessfully)
                    {
                        await result;
                    }
                    _protocolReader.Advance();
                }
                catch (Exception ex)
                {
                    await _connection.DisposeAsync();
                    _messageReader.FailQueue(ex);
                }
            }
        }

        public void Enqueue(TMessage request, TResponse response) => _requestQueue.Post((request, response));
    }
}