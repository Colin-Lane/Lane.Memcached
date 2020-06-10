using System.Threading.Tasks;
using Bedrock.Framework;
using Lane.Memcached.MessageQueue;
using Lane.Memcached.Protocol;
using Lane.Memcached.Protocol.Messaging;
using Lane.Memcached.Request;
using Lane.Memcached.Response;
using Xunit;

namespace Lane.Memcached.Test.Operations
{
    public class SetValueTest
    {
        [Fact]
        public async Task SetAndGetValue()
        {
            var builder = new ClientBuilder().UseSockets().Build();
            await using var connection = await builder.ConnectAsync(TestConstants.MemcachedServer);
            await using var client = new MemcachedProtocol(new LowLatencyMessageProtocol<MemcachedRequest, IMemcachedResponse>(connection, new MemcachedMessageWriter(), new MemcachedMessageReader()));
            client.Start();
            await client.Set("key", "value", null);
            var value = await client.Get<string, StringResponseDeserializer>("key", new StringResponseDeserializer());
            Assert.Equal("value", value);
        }
    }
}
