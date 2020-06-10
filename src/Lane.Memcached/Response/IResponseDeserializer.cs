namespace Lane.Memcached.Response
{
    public interface IResponseDeserializer<out T>
    {
        T Deserialize(MemcachedResponse response);
    }
}