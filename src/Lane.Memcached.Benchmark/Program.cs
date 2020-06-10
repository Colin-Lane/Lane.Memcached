using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Bedrock.Framework;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Enyim.Caching.Memcached;
using Lane.Memcached.MessageQueue;
using Lane.Memcached.Protocol;
using Lane.Memcached.Protocol.Messaging;
using Lane.Memcached.Request;
using Lane.Memcached.Response;
using Microsoft.AspNetCore.Connections;
using MoreLinq.Extensions;

namespace Lane.Memcached.Benchmark
{
    [SimpleJob(RuntimeMoniker.NetCoreApp31)]
    [RPlotExporter]
    [HtmlExporter]
    [MemoryDiagnoser]
    public class Benchmark
    {
        [Params(1)]
        public int Sockets { get; set; }
        [Params(20)]
        public int Threads {get;set;}
        [Params(2000)]
        public int Iterations { get; set; }
        private readonly string Key = string.Concat(Enumerable.Repeat("key", 25));
        private readonly string Value = string.Concat(Enumerable.Repeat("value", 100));


        [Benchmark]
        public Task LaneCacheLowLatency()
        {
            return Task.WhenAll(Enumerable.Range(0, Sockets).Select(x => RunLaneCache(connection => new LowLatencyMessageProtocol<MemcachedRequest, IMemcachedResponse>(connection, new MemcachedMessageWriter(), new MemcachedMessageReader()))));
        }

        private async Task RunLaneCache(Func<ConnectionContext, IMessageProtocol<MemcachedRequest, IMemcachedResponse>> protocol)
        {
            var builder = new ClientBuilder().UseSockets().Build();
            var connection = await builder.ConnectAsync(new IPEndPoint(IPAddress.Parse("10.0.0.21"), 11211));
            await using var client = new MemcachedProtocol(protocol(connection));
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

        [Benchmark]
        public Task Enyim()
        {
            return Task.WhenAll(Enumerable.Range(0, Sockets).Select(x => RunEnyim()));
        }

        private async Task RunEnyim()
        {
            using var _enyim = new MemcachedCluster("10.0.0.21:11211");
            _enyim.Start();

            var _enyimClient = _enyim.GetClient();

            var list = new List<Task>();
            for (var i = 0; i < Threads; i++)
            {
                list.Add(Task.Run(async () => {
                    await _enyimClient.SetAsync(Key, Value);
                    foreach (var batch in Enumerable.Range(0, Iterations).Batch(200))
                    {
                        await Task.WhenAll(batch.Select(x => _enyimClient.GetAsync(Key)));
                    }
                }));
            }
            await Task.WhenAll(list);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<Benchmark>();
        }
    }
}
