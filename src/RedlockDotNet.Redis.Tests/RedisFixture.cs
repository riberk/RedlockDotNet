using System;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace RedlockDotNet.Redis.Tests
{
    public class RedisFixture : IDisposable
    {
        public RedisFixture()
        {
            Redis1 = ConnectionMultiplexer.Connect(TestConfig.Instance.GetConnectionString("redis1"));
            Redis2 = ConnectionMultiplexer.Connect(TestConfig.Instance.GetConnectionString("redis2"));
            Redis3 = ConnectionMultiplexer.Connect(TestConfig.Instance.GetConnectionString("redis3"));
            Redis4 = ConnectionMultiplexer.Connect(TestConfig.Instance.GetConnectionString("redis4"));
            Redis5 = ConnectionMultiplexer.Connect(TestConfig.Instance.GetConnectionString("redis5"));
            Redis6 = ConnectionMultiplexer.Connect(TestConfig.Instance.GetConnectionString("redis6"));

            var unreachableConfig = new ConfigurationOptions
            {
                AbortOnConnectFail = false,
                EndPoints = { "0.1.2.3:6000"},
                ConnectTimeout = 1
            };
            Unreachable1 = ConnectionMultiplexer.Connect(unreachableConfig);
            Unreachable2 = ConnectionMultiplexer.Connect(unreachableConfig);
            Unreachable3 = ConnectionMultiplexer.Connect(unreachableConfig);
        }

        public void FlushAll()
        {
            FlushAll(Redis1);
            FlushAll(Redis2);
            FlushAll(Redis3);
            FlushAll(Redis4);
            FlushAll(Redis5);
            FlushAll(Redis6);
        }

        public static void FlushAll(ConnectionMultiplexer c)
        {
            foreach (var endPoint in c.GetEndPoints())
            {
                c.GetServer(endPoint).FlushAllDatabases();
            }
        }


        public ConnectionMultiplexer Redis1 { get; }
        public ConnectionMultiplexer Redis2 { get; }
        public ConnectionMultiplexer Redis3 { get; }
        public ConnectionMultiplexer Redis4 { get; }
        public ConnectionMultiplexer Redis5 { get; }
        public ConnectionMultiplexer Redis6 { get; }
        public ConnectionMultiplexer Unreachable1 { get; }
        public ConnectionMultiplexer Unreachable2 { get; }
        public ConnectionMultiplexer Unreachable3 { get; }

        public void Dispose()
        {
            Redis1?.Dispose();
            Redis2?.Dispose();
            Redis3?.Dispose();
            Redis4?.Dispose();
            Redis5?.Dispose();
            Redis6?.Dispose();
        }
    }
}