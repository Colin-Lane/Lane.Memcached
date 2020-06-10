using System;
using System.Collections.Generic;
using Lane.Memcached.Request;

namespace Lane.Memcached.Response
{
    public struct MemcachedErrorHandler
    {
        public T HandleError<T>(MemcachedResponse response)
        {
            switch (response.Header.ResponseStatus)
            {
                case Enums.ResponseStatus.KeyNotFound:
                    throw new KeyNotFoundException("key");
                default:
                    throw new Exception(response.Header.ResponseStatus.ToString());
            }
        }
    }
}