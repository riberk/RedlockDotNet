using System;
using Xunit;

namespace RedlockDotNet
{
    public static class RedlockOptionsTests
    {
        [Fact]
        public static void DefaultUtcNowIsDateTimeUtcNow()
        {
            var now = new RedlockOptions().UtcNow;
            
            var now1 = now();
            Assert.InRange(now1, DateTime.UtcNow.AddMilliseconds(-100), DateTime.UtcNow);
            
            var now2 = now();
            Assert.InRange(now2, DateTime.UtcNow.AddMilliseconds(-100), DateTime.UtcNow);
            
            var now3 = now();
            Assert.InRange(now3, DateTime.UtcNow.AddMilliseconds(-100), DateTime.UtcNow);
            
            Assert.Equal(DateTimeKind.Utc, now1.Kind);
            Assert.Equal(DateTimeKind.Utc, now2.Kind);
            Assert.Equal(DateTimeKind.Utc, now3.Kind);
            Assert.NotEqual(now1, now2);
            Assert.NotEqual(now2, now3);
        }
    }
}