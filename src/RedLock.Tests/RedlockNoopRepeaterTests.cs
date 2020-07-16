using Xunit;

namespace RedLock.Tests
{
    public static class RedlockNoopRepeaterTests
    {
        [Fact]
        public static void TestRepeater()
        {
            var r = new RedlockNoopRepeater();
            Assert.False(r.Next());
            Assert.False(r.Next());
            Assert.False(r.Next());
        }
        
        [Fact]
        public static void Singleton()
        {
            Assert.Same(RedlockNoopRepeater.Instance, RedlockNoopRepeater.Instance);
        }
    }
}