using System.Net;

namespace Lane.Memcached.Test
{
    public static class TestConstants
    {
        public static readonly string MemcachedServerIP = "10.0.0.21";
        public static readonly int MemcachedServerPort = 11211;
        public static IPEndPoint MemcachedServer => new IPEndPoint(IPAddress.Parse(MemcachedServerIP), MemcachedServerPort);
    }
}
