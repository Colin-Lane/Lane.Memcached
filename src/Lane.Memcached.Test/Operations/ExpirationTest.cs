using System;
using System.Collections.Generic;
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
    public class ExpirationTest
    {
        [Fact]
        public async Task ExpireQuickly()
        {
            var builder = new ClientBuilder().UseSockets().Build();
            await using var connection = await builder.ConnectAsync(TestConstants.MemcachedServer);
            await using var client = new MemcachedProtocol(new LowLatencyMessageProtocol<MemcachedRequest, IMemcachedResponse>(connection, new MemcachedMessageWriter(), new MemcachedMessageReader()));
            client.Start();

            var key = "ToExpire";

            var expirationTime = TimeSpan.FromSeconds(2);

            await client.Set(key, "firstValue", expirationTime);
            var value = await client.Get<string, StringResponseDeserializer>(key, new StringResponseDeserializer());
            Assert.Equal("firstValue", value);

            // memcached expires entries on 1-second boundaries, so setting this to n+1 seconds expiration may cause the test to fail some of the time
            await Task.Delay(expirationTime.Add(TimeSpan.FromSeconds(2)));

            await Assert.ThrowsAsync<KeyNotFoundException>(() => client.Get<string, StringResponseDeserializer>(key, new StringResponseDeserializer()));
        }
    }
}
