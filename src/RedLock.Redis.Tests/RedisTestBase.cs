using Xunit;

namespace RedLock.Redis.Tests
{
    [Collection(nameof(RedisCollection))]
    public class RedisTestBase
    {
        public RedisFixture Redis { get; }

        public RedisTestBase(RedisFixture redis)
        {
            Redis = redis;
            Redis.FlushAll();
        }
    }
}