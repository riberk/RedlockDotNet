using System.Threading;
using Xunit;

namespace RedLock.Tests
{
    public static class CancellationRedlockRepeaterTests
    {
        [Fact]
        public static void NextTest_NoCancel()
        {
            var r = new CancellationRedlockRepeater(CancellationToken.None);
            Assert.True(r.Next());
            Assert.True(r.Next());
            Assert.True(r.Next());
        }
        
        [Fact]
        public static void NextTest_Canceled()
        {
            var r = new CancellationRedlockRepeater(new CancellationToken(true));
            Assert.False(r.Next());
            Assert.False(r.Next());
            Assert.False(r.Next());
        }
        
        [Fact]
        public static void NextTest_ChangedCancellation()
        {
            using var cts = new CancellationTokenSource();
            var r = new CancellationRedlockRepeater(cts.Token);
            Assert.True(r.Next());
            Assert.True(r.Next());
            cts.Cancel();
            Assert.False(r.Next());
        }
    }
}