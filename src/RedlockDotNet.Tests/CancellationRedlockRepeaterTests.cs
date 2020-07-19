using System;
using System.Threading;
using RedlockDotNet.Repeaters;
using Xunit;

namespace RedlockDotNet
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

        [Fact]
        public static void CreateException()
        {
            using var cts = new CancellationTokenSource();
            var r = new CancellationRedlockRepeater(cts.Token);
            var exception = r.CreateException("rrrr", "nnnnn", 10000);
            var oce = Assert.IsType<OperationCanceledException>(exception);
            Assert.Equal(cts.Token, oce.CancellationToken);
            Assert.Contains("rrrr", oce.Message);
            Assert.Contains("nnnnn", oce.Message);
            Assert.Contains("10000", oce.Message);
        }
    }
}