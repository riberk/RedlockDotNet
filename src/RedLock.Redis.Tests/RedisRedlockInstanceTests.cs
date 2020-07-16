using System;
using System.Threading.Tasks;
using StackExchange.Redis;
using Xunit;

namespace RedLock.Redis.Tests
{
    public class RedisRedlockInstanceTests : RedisTestBase
    {
        private readonly RedisRedlockInstance _instance;

        public RedisRedlockInstanceTests(RedisFixture redis) : base(redis)
        {
            _instance = new RedisRedlockInstance(Db);
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

        private IDatabase Db()
        {
            return Redis.Redis1.GetDatabase();
        }
    }
}