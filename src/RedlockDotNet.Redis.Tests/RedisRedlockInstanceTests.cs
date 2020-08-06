using System;
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
            _instance = new RedisRedlockInstance(Db, s => s, "i", _logger);
        }

        [Fact]
        public void TryLock()
        {
            Assert.True(_instance.TryLock("r", "n", TimeSpan.FromSeconds(10)));
            var result = Db().StringGetWithExpiry("r");
            Assert.Equal("n", result.Value);
            Assert.NotNull(result.Expiry);
            Assert.InRange(result.Expiry.Value, TimeSpan.FromSeconds(9.5), TimeSpan.FromSeconds(10));
        }
        
        [Fact]
        public async Task TryLockAsync()
        {
            Assert.True(await _instance.TryLockAsync("r", "n", TimeSpan.FromSeconds(10)));
            var result = Db().StringGetWithExpiry("r");
            Assert.Equal("n", result.Value);
            Assert.NotNull(result.Expiry);
            Assert.InRange(result.Expiry.Value, TimeSpan.FromSeconds(9.5), TimeSpan.FromSeconds(10));
        }

        [Fact]
        public void Unlock_Owner()
        {
            Db().StringSet("r", "n");
            _instance.Unlock("r", "n");
            Assert.False(Db().KeyExists("r"));
        }
        
        [Fact]
        public void Unlock_NotOwner()
        {
            Db().StringSet("r", "n");
            _instance.Unlock("r", "n111");
            Assert.True(Db().KeyExists("r"));
        }
        
        
        [Fact]
        public async Task UnlockAsync_Owner()
        {
            Db().StringSet("r", "n");
            await _instance.UnlockAsync("r", "n");
            Assert.False(Db().KeyExists("r"));
        }
        
        [Fact]
        public async Task UnlockAsync_NotOwner()
        {
            Db().StringSet("r", "n");
            await _instance.UnlockAsync("r", "n111");
            Assert.True(Db().KeyExists("r"));
        }

        [Fact]
        public void TryExtend_Owner()
        {
            Db().StringSet("r", "n", TimeSpan.FromSeconds(5));
            Assert.Equal(ExtendResult.Extend, _instance.TryExtend("r", "n", TimeSpan.FromSeconds(10), false));
            var result = Db().StringGetWithExpiry("r");
            Assert.Equal("n", result.Value);
            Assert.NotNull(result.Expiry);
            Assert.InRange(result.Expiry.Value, TimeSpan.FromSeconds(9.5), TimeSpan.FromSeconds(10));
        }
        
        [Fact]
        public void TryExtend_Owner_Reacquire()
        {
            Assert.Equal(ExtendResult.Reacquire, _instance.TryExtend("r", "n", TimeSpan.FromSeconds(10), true));
            var result = Db().StringGetWithExpiry("r");
            Assert.Equal("n", result.Value);
            Assert.NotNull(result.Expiry);
            Assert.InRange(result.Expiry.Value, TimeSpan.FromSeconds(9.5), TimeSpan.FromSeconds(10));
        }
        
        [Fact]
        public void TryExtend_Owner_ReacquireFail()
        {
            Assert.Equal(ExtendResult.IllegalReacquire, _instance.TryExtend("r", "n", TimeSpan.FromSeconds(10), false));
            var result = Db().StringGetWithExpiry("r");
            Assert.False(result.Value.HasValue);
        }
        
        [Fact]
        public void TryExtend_NotOwner()
        {
            Db().StringSet("r", "nnnnn");
            Assert.Equal(ExtendResult.AlreadyAcquiredByAnotherOwner, _instance.TryExtend("r", "n", TimeSpan.FromSeconds(10), false));
            var result = Db().StringGetWithExpiry("r");
            Assert.Equal("nnnnn", result.Value);
            Assert.Null(result.Expiry);
        }
        
        
        [Fact]
        public async Task TryExtendAsync_Owner()
        {
            Db().StringSet("r", "n", TimeSpan.FromSeconds(5));
            Assert.Equal(ExtendResult.Extend, await _instance.TryExtendAsync("r", "n", TimeSpan.FromSeconds(10), false));
            var result = Db().StringGetWithExpiry("r");
            Assert.Equal("n", result.Value);
            Assert.NotNull(result.Expiry);
            Assert.InRange(result.Expiry.Value, TimeSpan.FromSeconds(9.5), TimeSpan.FromSeconds(10));
        }
        
        [Fact]
        public async Task TryExtendAsync_Owner_Reacquire()
        {
            Assert.Equal(ExtendResult.Reacquire, await _instance.TryExtendAsync("r", "n", TimeSpan.FromSeconds(10), true));
            var result = Db().StringGetWithExpiry("r");
            Assert.Equal("n", result.Value);
            Assert.NotNull(result.Expiry);
            Assert.InRange(result.Expiry.Value, TimeSpan.FromSeconds(9.5), TimeSpan.FromSeconds(10));
        }
        
        [Fact]
        public async Task TryExtendAsync_Owner_ReacquireFail()
        {
            Assert.Equal(ExtendResult.IllegalReacquire, await _instance.TryExtendAsync("r", "n", TimeSpan.FromSeconds(10), false));
            var result = Db().StringGetWithExpiry("r");
            Assert.False(result.Value.HasValue);
        }
        
        [Fact]
        public async Task TryExtendAsync_NotOwner()
        {
            Db().StringSet("r", "nnnnn");
            Assert.Equal(ExtendResult.AlreadyAcquiredByAnotherOwner, await _instance.TryExtendAsync("r", "n", TimeSpan.FromSeconds(10), false));
            var result = Db().StringGetWithExpiry("r");
            Assert.Equal("nnnnn", result.Value);
            Assert.Null(result.Expiry);
        }

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