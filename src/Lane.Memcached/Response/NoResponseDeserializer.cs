using System;
using Lane.Memcached.Request;

namespace Lane.Memcached.Response
{
    internal struct NoResponseDeserializer : IResponseDeserializer<bool>
    {
        public bool Deserialize(MemcachedResponse response)
        {
            if (response.Header.ResponseStatus != Enums.ResponseStatus.NoError)
            {
                throw new Exception(response.Header.ResponseStatus.ToString());
            }

            return true;
        }
    }
}