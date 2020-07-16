using Xunit;

namespace RedLock.Redis.Tests
{
    [CollectionDefinition(nameof(RedisCollection))]
    public class RedisCollection : ICollectionFixture<RedisFixture>
    {
        
    }
}