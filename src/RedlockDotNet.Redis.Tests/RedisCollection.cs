using Xunit;

namespace RedlockDotNet.Redis.Tests
{
    [CollectionDefinition(nameof(RedisCollection))]
    public class RedisCollection : ICollectionFixture<RedisFixture>
    {
        
    }
}