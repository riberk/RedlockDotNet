using System;
using System.Collections.Generic;
using System.Threading;
using Redlock.Internal;
using Xunit;

namespace RedLock.Tests.Internal
{
    public static class ThreadSafeRandomTests
    {
        [Fact]
        public static void LocalIsThreadStatic()
        {
            Random? t1R1 = null;
            Random? t1R2 = null;
            Random? t2R1 = null;
            Random? t2R2 = null;
            
            var t1 = new Thread(() =>
            {
                Interlocked.Exchange(ref t1R1, ThreadSafeRandom.Local);
                Interlocked.Exchange(ref t1R2, ThreadSafeRandom.Local);
            });

            var t2 = new Thread(() =>
            {
                Interlocked.Exchange(ref t2R1, ThreadSafeRandom.Local);
                Interlocked.Exchange(ref t2R2, ThreadSafeRandom.Local);
            });

            t1.Start();
            t2.Start();
            t1.Join();
            t2.Join();
            
            Assert.NotNull(t1R1);
            Assert.NotNull(t1R2);
            Assert.NotNull(t2R1);
            Assert.NotNull(t2R2);
            
            Assert.Same(t1R1, t1R2);
            Assert.Same(t2R1, t2R2);
            Assert.NotSame(t1R1, t2R1);
        }
    }
}