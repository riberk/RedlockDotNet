using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;
using TestUtils;
using Xunit;
using Xunit.Abstractions;

namespace RedlockDotNet.Redis.Tests
{
    public class RedisRedlockInstanceTests : RedisTestBase, IDisposable
    {
        private readonly ITestOutputHelper _console;
        private readonly RedisRedlockInstance _instance;
        private readonly MemoryLogger _logger;

        public RedisRedlockInstanceTests(RedisFixture redis, ITestOutputHelper console) : base(redis)
        {
            _console = console;
            _logger = new MemoryLogger();
            _instance = new RedisRedlockInstance(Db, s => s, "i", 0.1f, _logger);
        }
        
        [Fact]
        public void MinValidity_NoClockDriftFactor()
        {
            var instance = new RedisRedlockInstance(Db, s => s, "i", 0, _logger);
            var minValidity = instance.MinValidity(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(1));
            Assert.Equal(TimeSpan.FromMilliseconds(8998), minValidity);
        }
        
        [Fact]
        public void MinValidity_ClockDriftFactor()
        {
            var instance = new RedisRedlockInstance(Db, s => s, "i", 0.01f, _logger);
            var minValidity = instance.MinValidity(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(1));
            Assert.Equal(TimeSpan.FromMilliseconds(8898), minValidity);
        }
        
        [Fact]
        public void MinValidity_ClockDriftFactor03()
        {
            var instance = new RedisRedlockInstance(Db, s => s, "i", 0.5f, _logger);
            var minValidity = instance.MinValidity(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(1));
            Assert.Equal(TimeSpan.FromMilliseconds(3998), minValidity);
        }

        [Fact]
        public void TryLock()
        {
            Assert.True(_instance.TryLock("r", "n", TimeSpan.FromSeconds(10)));
            var result = Get("r");
            Assert.Equal("n", result!.Nonce);
            Assert.NotNull(result.Ttl);
            Assert.Empty(result.Metadata);
            Assert.InRange(result.Ttl!.Value, TimeSpan.FromSeconds(9.5), TimeSpan.FromSeconds(10));
        }
        
        [Fact]
        public async Task TryLockAsync()
        {
            Assert.True(await _instance.TryLockAsync("r", "n", TimeSpan.FromSeconds(10)));
            var result = Get("r");
            Assert.Equal("n", result!.Nonce);
            Assert.NotNull(result.Ttl);
            Assert.Empty(result.Metadata);
            Assert.InRange(result.Ttl!.Value, TimeSpan.FromSeconds(9.5), TimeSpan.FromSeconds(10));
        }
        
        [Fact]
        public void TryLock_Meta()
        {
            Assert.True(_instance.TryLock("r", "n", TimeSpan.FromSeconds(10), new Dictionary<string, string>{["a"] = "b"}));
            var result = Get("r");
            Assert.Equal("n", result!.Nonce);
            Assert.NotNull(result.Ttl);
            Assert.Equal("b", result.Metadata["a"]);
            Assert.InRange(result.Ttl!.Value, TimeSpan.FromSeconds(9.5), TimeSpan.FromSeconds(10));
        }
        
        [Fact]
        public async Task TryLockAsync_Meta()
        {
            Assert.True(await _instance.TryLockAsync("r", "n", TimeSpan.FromSeconds(10), new Dictionary<string, string>{["a"] = "b"}));
            var result = Get("r");
            Assert.Equal("n", result!.Nonce);
            Assert.NotNull(result.Ttl);
            Assert.Equal("b", result.Metadata["a"]);
            Assert.InRange(result.Ttl!.Value, TimeSpan.FromSeconds(9.5), TimeSpan.FromSeconds(10));
        }

        [Fact]
        public void Unlock_Owner()
        {
            Set("r", "n");
            _instance.Unlock("r", "n");
            Assert.False(Db().KeyExists("r"));
        }
        
        [Fact]
        public void Unlock_NotOwner()
        {
            Set("r", "n");
            _instance.Unlock("r", "n111");
            Assert.True(Db().KeyExists("r"));
        }
        
        
        [Fact]
        public async Task UnlockAsync_Owner()
        {
            Set("r", "n");
            await _instance.UnlockAsync("r", "n");
            Assert.False(Db().KeyExists("r"));
        }
        
        [Fact]
        public async Task UnlockAsync_NotOwner()
        {
            Set("r", "n");
            await _instance.UnlockAsync("r", "n111");
            Assert.True(Db().KeyExists("r"));
        }

        [Fact]
        public void TryExtend_Owner()
        {
            Set("r", "n", TimeSpan.FromSeconds(5));
            Assert.Equal(ExtendResult.Extend, _instance.TryExtend("r", "n", TimeSpan.FromSeconds(10)));
            var result = Get("r");
            Assert.Equal("n", result!.Nonce);
            Assert.NotNull(result.Ttl);
            Assert.InRange(result.Ttl!.Value, TimeSpan.FromSeconds(9.5), TimeSpan.FromSeconds(10));
        }
        
        [Fact]
        public void TryExtend_Owner_Fail()
        {
            Assert.Equal(ExtendResult.IllegalReacquire, _instance.TryExtend("r", "n", TimeSpan.FromSeconds(10)));
            var result = Get("r");
            Assert.Null(result);
        }
        
        [Fact]
        public void TryExtend_NotOwner()
        {
            Set("r", "nnnnn");
            Assert.Equal(ExtendResult.AlreadyAcquiredByAnotherOwner, _instance.TryExtend("r", "n", TimeSpan.FromSeconds(10)));
            var result = Get("r");
            Assert.Equal("nnnnn", result!.Nonce);
            Assert.Null(result.Ttl);
        }
        
        
        [Fact]
        public async Task TryExtendAsync_Owner()
        {
            Set("r", "n", TimeSpan.FromSeconds(5));
            Assert.Equal(ExtendResult.Extend, await _instance.TryExtendAsync("r", "n", TimeSpan.FromSeconds(10)));
            var result = Get("r");
            Assert.Equal("n", result!.Nonce);
            Assert.NotNull(result.Ttl);
            Assert.InRange(result.Ttl!.Value, TimeSpan.FromSeconds(9.5), TimeSpan.FromSeconds(10));
        }
        
        [Fact]
        public async Task TryExtendAsync_Owner_Fail()
        {
            Assert.Equal(ExtendResult.IllegalReacquire, await _instance.TryExtendAsync("r", "n", TimeSpan.FromSeconds(10)));
            var result = Get("r");
            Assert.Null(result);
        }
        
        [Fact]
        public async Task TryExtendAsync_NotOwner()
        {
            Set("r", "nnnnn");
            Assert.Equal(ExtendResult.AlreadyAcquiredByAnotherOwner, await _instance.TryExtendAsync("r", "n", TimeSpan.FromSeconds(10)));
            var result = Get("r");
            Assert.Equal("nnnnn", result!.Nonce);
            Assert.Null(result.Ttl);
        }
        
        [Fact]
        public void GetInfo()
        {
            Set("r", "nnnnn", TimeSpan.FromMinutes(10), new Dictionary<string, string>{["a"] = "b"});
            var info = _instance.GetInfo("r");
            Assert.Equal("nnnnn", info!.Nonce);
            Assert.InRange(info.Ttl!.Value, TimeSpan.FromMinutes(9.5), TimeSpan.FromMinutes(10));
            Assert.Equal("b", info.Metadata["a"]);
        }
        
        [Fact]
        public async Task GetInfoAsync()
        {
            Set("r", "nnnnn", TimeSpan.FromMinutes(10), new Dictionary<string, string>{["a"] = "b"});
            var info = await _instance.GetInfoAsync("r");
            Assert.Equal("nnnnn", info!.Nonce);
            Assert.InRange(info.Ttl!.Value, TimeSpan.FromMinutes(9.5), TimeSpan.FromMinutes(10));
            Assert.Equal("b", info.Metadata["a"]);
        }

        private void Set(string resource, string nonce, TimeSpan? expiry = null, IReadOnlyDictionary<string, string>? meta = null)
        {
            Db().HashSet(resource, "nonce", nonce);
            if (meta != null)
            {
                Db().HashSet(resource, meta.Select(x => new HashEntry(x.Key, x.Value)).ToArray());
            }
            Db().KeyExpire(resource, expiry);
        }
        
        private InstanceLockInfo? Get(string resource) => _instance.GetInfo(resource);

        private IDatabase Db()
        {
            return Redis.Redis1.GetDatabase();
        }

        public void Dispose()
        {
            _logger.Provider.WriteLogs(_console.WriteLine);
        }
    }
}