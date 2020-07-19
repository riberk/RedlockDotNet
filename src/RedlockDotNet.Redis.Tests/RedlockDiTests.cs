using System;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace RedlockDotNet.Redis.Tests
{
    public class RedlockDiTests : RedisTestBase
    {
        private readonly IServiceCollection _services;

        private const string Connection = "0.0.0.1:6001,connectTimeout=1,abortConnect=false";
        private static readonly ConfigurationOptions Options = new ConfigurationOptions
        {
            AbortOnConnectFail = false,
            ConnectTimeout = 1,
            EndPoints =
            {
                IPEndPoint.Parse("0.0.0.1:6001")
            },
        };
        public RedlockDiTests(RedisFixture fixture) : base(fixture)
        {
            _services = new ServiceCollection().AddRedlock().AddRedisStorage(b =>
            {
                b.AddInstance(TestConfig.Instance.GetConnectionString("redis1"), "1");
                b.AddInstance(TestConfig.Instance.GetConnectionString("redis2"), "2");
                b.AddInstance(TestConfig.Instance.GetConnectionString("redis3"), "3");
            });
        }

        [Fact]
        public void BuildWithConstruction()
        {
            _services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateScopes = true,
                ValidateOnBuild = true,
            });
        }

        [Fact]
        public void GetFromDi_ThenLock_ThenUnlock()
        {
            var f = _services.BuildServiceProvider().GetRequiredService<IRedlockFactory>();
            using (var l = f.Create("r"))
            {
                Assert.Equal(l.Nonce, Redis.Redis1.GetDatabase().StringGet("r"));
                Assert.Equal(l.Nonce, Redis.Redis2.GetDatabase().StringGet("r"));
                Assert.Equal(l.Nonce, Redis.Redis3.GetDatabase().StringGet("r"));
            }

            Assert.False(Redis.Redis1.GetDatabase().KeyExists("r"));
            Assert.False(Redis.Redis2.GetDatabase().KeyExists("r"));
            Assert.False(Redis.Redis3.GetDatabase().KeyExists("r"));
        }

        [Fact]
        public static void AddRedisStorage()
        {
            var p = new ServiceCollection().AddRedlock()
                .AddRedisStorage(b => { }, o => o.ClockDriftFactor = 0.5f)
                .AddSingleton(new Mock<IRedlockInstance>().Object)
                .BuildServiceProvider();
            Assert.Equal(0.5f, p.GetRequiredService<IOptions<RedisRedlockOptions>>().Value.ClockDriftFactor);
            Assert.NotNull(p.GetService<IRedlockFactory>());
        }
        
        [Fact]
        public static void AddInstanceByConnectionString() => AssertAddInstance(b => b.AddInstance(Connection, 5, "i1"), "i1", 5);

        [Fact]
        public static void AddInstanceByConnectionString_DefaultName() => AssertAddInstance(b => b.AddInstance(Connection, 5), "0.0.0.1:6001", 5);

        [Fact]
        public static void AddInstanceByConnectionString_DefaultDb() => AssertAddInstance(b => b.AddInstance(Connection, "i"), "i", 0);

        [Fact]
        public static void AddInstanceByConnectionString_DefaultName_DefaultDb() => AssertAddInstance(b => b.AddInstance(Connection), "0.0.0.1:6001", 0);

        [Fact]
        public static void AddInstanceByConf() => AssertAddInstance(b => b.AddInstance(Options, 5, "i1"), "i1", 5);

        [Fact]
        public static void AddInstanceByConf_DefaultName() => AssertAddInstance(b => b.AddInstance(Options, 5), "0.0.0.1:6001", 5);

        [Fact]
        public static void AddInstanceByConf_DefaultDb() => AssertAddInstance(b => b.AddInstance(Options, "i"), "i", 0);

        [Fact]
        public static void AddInstanceByConf_DefaultName_DefaultDb() => AssertAddInstance(b => b.AddInstance(Options), "0.0.0.1:6001", 0);

        private static void AssertAddInstance(Action<IRedisRedlockBuilder> addInstance, string expectedName, int expectedDb)
        {
            var p = new ServiceCollection().AddRedlock().AddRedisStorage(addInstance).BuildServiceProvider();
            var instance = p.GetRequiredService<IRedlockInstance>();
            var redisInstance = Assert.IsType<RedisRedlockInstance>(instance);
            Assert.Equal(expectedName, redisInstance.ToString());
            Assert.Equal(expectedDb, redisInstance.SelectDb().Database);
        }
    }
}