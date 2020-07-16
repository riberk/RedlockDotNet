using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TestUtils;
using Xunit;

namespace RedLock.Redis.Tests
{
    public class RedisRedlockIntegrationTests : RedisTestBase
    {
        private readonly RedisRedlockOptions _opt;
        private readonly RedisRedlockImplementation _5Inst;
        private readonly MemoryLogger _log;
        private readonly RedisRedlockImplementation _noQuorum;

        public RedisRedlockIntegrationTests(RedisFixture redis) : base(redis)
        {
            _opt = new RedisRedlockOptions();
            _5Inst = new RedisRedlockImplementation(new []
            {
                new RedisRedlockInstance(() => Redis.Redis1.GetDatabase()), 
                new RedisRedlockInstance(() => Redis.Redis2.GetDatabase()), 
                new RedisRedlockInstance(() => Redis.Redis3.GetDatabase()), 
                new RedisRedlockInstance(() => Redis.Redis4.GetDatabase()), 
                new RedisRedlockInstance(() => Redis.Redis5.GetDatabase()), 
            }, Options.Create(_opt));
            _noQuorum = new RedisRedlockImplementation(new []
            {
                new RedisRedlockInstance(() => Redis.Redis1.GetDatabase()), 
                new RedisRedlockInstance(() => Redis.Redis2.GetDatabase()), 
                new RedisRedlockInstance(() => Redis.Unreachable1.GetDatabase()), 
                new RedisRedlockInstance(() => Redis.Unreachable2.GetDatabase()), 
            }, Options.Create(_opt));
            _log = new MemoryLogger();
        }

        [Fact]
        public void IntegrationTest()
        {
            using var l = Redlock.Lock("r", "n", TimeSpan.FromSeconds(10), _5Inst, _log, NoopRedlockRepeater.Instance);
        }

        [Fact]
        public void MultiThread_NoOverlaps()
        {
            const string resource = nameof(MultiThread_NoOverlaps);
            var threads = new Thread[32];
            var threadWaitMs = 100;
            using var cts = new CancellationTokenSource(threads.Length * threadWaitMs + 15000);
            var repeater = new CancellationRedlockRepeater(cts.Token);
            var locksCount = 0;
            var exceptions = new ConcurrentBag<Exception>();
            for (var i = 0; i < threads.Length; i++)
            {
                
                var threadId = i;
                threads[i] = new Thread(() =>
                {
                    try
                    {
                        var nonce = threadId.ToString();
                        var ttl = TimeSpan.FromSeconds(10);
                        
                        using var l = Redlock.Lock(resource, nonce, ttl, _5Inst, _log, repeater, 50);
                        Assert.Equal(0, locksCount);
                        Interlocked.Increment(ref locksCount);
                        Thread.Sleep(threadWaitMs);
                        Assert.Equal(1, locksCount);
                        Interlocked.Decrement(ref locksCount);
                    }
                    catch (Exception e)
                    {
                        exceptions.Add(e);
                    }
                    
                });
                threads[i].Start();
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }
            
            Assert.Empty(exceptions);
        }
        
        [Fact]
        public void Sequential()
        {
            for (var i = 0; i < 10; i++)
            {
                var l = Redlock.TryLock("r", i.ToString(), TimeSpan.FromSeconds(10), _5Inst, _log);
                Assert.NotNull(l);
                l.Value.Dispose();
            }
        }

        [Fact]
        public void TimeoutExpires_ThenNewLockCanBeObtained()
        {
            using var l1 = Redlock.Lock("r", "n1", TimeSpan.FromSeconds(1), _5Inst, _log, NoopRedlockRepeater.Instance);
            Thread.Sleep(1000);
            using var l2 = Redlock.Lock("r", "n2", TimeSpan.FromSeconds(1), _5Inst, _log, NoopRedlockRepeater.Instance);
        }

        [Fact]
        public void NoQuorumDoesNotObtainLock()
        {
            var l = Redlock.TryLock("r", "n", TimeSpan.FromSeconds(1), _noQuorum, _log);
            Assert.Null(l);
            Assert.NotEmpty(_log.Logs.Where(x => x.Exception != null && x.LogLevel == LogLevel.Error));
        }

    }
}