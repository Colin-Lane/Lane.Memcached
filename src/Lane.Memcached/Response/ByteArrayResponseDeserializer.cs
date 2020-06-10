using System.Buffers;
using Lane.Memcached.Request;

namespace Lane.Memcached.Response
{
    public struct ByteArrayResponseDeserializer: IResponseDeserializer<byte[]>
    {
        public byte[] Deserialize(MemcachedResponse response)
        {
            if (response.Header.ResponseStatus != Enums.ResponseStatus.NoError)
                return default(MemcachedErrorHandler).HandleError<byte[]>(response);

            return response.Data.ToArray();
        } 
    }
}