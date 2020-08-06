using Xunit;

namespace RedlockDotNet.Redis.Tests
{
    public static class RedisRedlockOptionsTests
    {
        [Fact]
        public static void DefaultMakeKeyAreIdentity()
        {
            var makeKey = new RedisRedlockOptions().RedisKeyFromResourceName;
            
            Assert.Equal("", makeKey(""));
            Assert.Equal("1", makeKey("1"));
            Assert.Equal("awfas;egjopiaerhgieg", makeKey("awfas;egjopiaerhgieg"));
        }
    }
}