using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Bedrock.Framework;
using Lane.Memcached.MessageQueue;
using Lane.Memcached.Protocol;
using Lane.Memcached.Protocol.Messaging;
using Lane.Memcached.Request;
using Lane.Memcached.Response;
using MoreLinq.Extensions;

namespace Lane.Memcached.Console
{
    class Program
    {
        public const int Sockets = 2;
        public const int Threads = 250;
        public const int Iterations = 5_000;
        private static readonly string Key = string.Concat(Enumerable.Repeat("key", 20));
        private static readonly string Value = string.Concat(Enumerable.Repeat("value", 100));
        private static readonly string Destination = "10.0.0.21";

        static async Task Main(string[] args)
        {
            await LaneCaching();
        }

        static Task LaneCaching()
        {
            return Task.WhenAll(Enumerable.Range(0, Sockets).Select(async x =>
            {
                var builder = new ClientBuilder().UseSockets().Build();
                var connection = builder.ConnectAsync(new IPEndPoint(IPAddress.Parse(Destination), 11211)).GetAwaiter().GetResult();
                await using (var client = new MemcachedProtocol(new LowLatencyMessageProtocol<MemcachedRequest, IMemcachedResponse>(connection, new MemcachedMessageWriter(), new MemcachedMessageReader())))
                {
                    client.Start();

                    var list = new List<Task>();
                    for (var i = 0; i < Threads; i++)
                    {
                        list.Add(Task.Run(async () => {
                            await client.Set(Key, Value, null);
                            foreach (var batch in Enumerable.Range(0, Iterations).Batch(200))
                            {
                                await Task.WhenAll(batch.Select(x =>
                                    client.Get<string, StringResponseDeserializer>(Key,
                                        new StringResponseDeserializer())));
                            }
                        }));
                    }
                    await Task.WhenAll(list);
                }
            }));
        }
    }
}
