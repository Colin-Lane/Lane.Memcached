using System;
using System.Buffers;
using System.Threading.Tasks;
using Lane.Memcached.Request;
using Lane.Memcached.Response;

namespace Lane.Memcached.MessageQueue
{
    public class AsyncResponse<T, TD>: IMemcachedResponse where TD: IResponseDeserializer<T>
    {
        private readonly TaskCompletionSource<T> _task;
        private readonly TD _deserializer;

        public AsyncResponse(TD deserializer)
        {
            _deserializer = deserializer;
            _task = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public Task<T> Task => _task.Task;

        public bool TryParseMessage(in ReadOnlySequence<byte> input, ref SequencePosition consumed, ref SequencePosition examined)
        {
            MemcachedResponse message;
            if (input.Length < Constants.HeaderLength)
            {
                return false;
            }

            if (input.First.Length >= Constants.HeaderLength)
            {
                message = new MemcachedResponse(new MemcachedResponseHeader(input.First.Span));
            }
            else
            {
                Span<byte> header = stackalloc byte[Constants.HeaderLength];
                input.Slice(0, Constants.HeaderLength).CopyTo(header);
                message = new MemcachedResponse(new MemcachedResponseHeader(header));
            }

            if (input.Length < message.Header.TotalBodyLength + Constants.HeaderLength)
            {
                return false;
            }

            message.ReadBody(input.Slice(Constants.HeaderLength, message.Header.TotalBodyLength));
            examined = input.Slice(0, Constants.HeaderLength + message.Header.TotalBodyLength).End;
            consumed = input.Slice(0, Constants.HeaderLength + message.Header.TotalBodyLength).End;
            try
            {
                _task.TrySetResult(_deserializer.Deserialize(message));
            }
            catch (Exception ex)
            {
                SetException(ex);
                throw;
            }
            
            return true;
        }

        public void SetException(Exception exception)
        {
            _task.TrySetException(exception);
        }
    }
}