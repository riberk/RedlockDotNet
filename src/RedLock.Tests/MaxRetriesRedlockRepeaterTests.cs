using Xunit;

namespace RedLock.Tests
{
    public static class MaxRetriesRedlockRepeaterTests
    {
        [Fact]
        public static void TestRepeater()
        {
            var r = new MaxRetriesRedlockRepeater(3);
            Assert.True(r.Next());
            Assert.True(r.Next());
            Assert.True(r.Next());
            Assert.False(r.Next());
            Assert.False(r.Next());
        }
    }
}