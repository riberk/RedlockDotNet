using RedlockDotNet.Repeaters;
using Xunit;

namespace RedlockDotNet
{
    public static class RedlockNoopRepeaterTests
    {
        [Fact]
        public static void TestRepeater()
        {
            var r = new NoopRedlockRepeater();
            Assert.False(r.Next());
            Assert.False(r.Next());
            Assert.False(r.Next());
        }
        
        [Fact]
        public static void Singleton()
        {
            Assert.Same(NoopRedlockRepeater.Instance, NoopRedlockRepeater.Instance);
        }
    }
}