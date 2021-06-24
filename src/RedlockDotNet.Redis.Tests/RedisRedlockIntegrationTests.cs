using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using RedlockDotNet.Repeaters;
using TestUtils;
using Xunit;
using Xunit.Abstractions;

namespace RedlockDotNet.Redis.Tests
{
    public class RedisRedlockIntegrationTests : RedisTestBase, IDisposable
    {
        private readonly ImmutableArray<IRedlockInstance> _5Inst;
        private readonly MemoryLogger _log;
        private readonly ImmutableArray<IRedlockInstance> _noQuorum;
        private readonly ITestOutputHelper _console;

        public RedisRedlockIntegrationTests(RedisFixture redis, ITestOutputHelper console) : base(redis)
        {
            _console = console;
            _log = new MemoryLogger();
            _5Inst = new IRedlockInstance[]
            {
                new RedisRedlockInstance(() => Redis.Redis1.GetDatabase(), s => s, "1", 0.1f, _log),
                new RedisRedlockInstance(() => Redis.Redis2.GetDatabase(), s => s, "2", 0.1f, _log),
                new RedisRedlockInstance(() => Redis.Redis3.GetDatabase(), s => s, "3", 0.1f, _log),
                new RedisRedlockInstance(() => Redis.Redis4.GetDatabase(), s => s, "4", 0.1f, _log),
                new RedisRedlockInstance(() => Redis.Redis5.GetDatabase(), s => s, "5", 0.1f, _log),
            }.ToImmutableArray();
            _noQuorum = new IRedlockInstance[]
            {
                new RedisRedlockInstance(() => Redis.Redis1.GetDatabase(), s => s, "1", 0.1f, _log),
                new RedisRedlockInstance(() => Redis.Redis2.GetDatabase(), s => s, "2", 0.1f, _log),
                new RedisRedlockInstance(() => Redis.Unreachable1.GetDatabase(), s => s, "u1", 0.1f, _log),
                new RedisRedlockInstance(() => Redis.Unreachable2.GetDatabase(), s => s, "u2", 0.1f, _log),
            }.ToImmutableArray();
        }

        [Fact]
        public void IntegrationTest()
        {
            using var l = Redlock.Lock("r", "n", TimeSpan.FromSeconds(10), _5Inst, _log, NoopRedlockRepeater.Instance, 50);
        }

        [Fact]
        public void MultiThread_NoOverlaps()
        {
            const string resource = nameof(MultiThread_NoOverlaps);
            var threads = new Thread[16];
            var threadWaitMs = 100;
            using var cts = new CancellationTokenSource(threads.Length * threadWaitMs + 75000);
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
                l!.Value.Dispose();
            }
        }

        [Fact]
        public void TimeoutExpires_ThenNewLockCanBeObtained()
        {
            using var l1 = Redlock.Lock("r", "n1", TimeSpan.FromSeconds(0.5), _5Inst, _log, NoopRedlockRepeater.Instance, 50);
            Thread.Sleep(800);
            using var l2 = Redlock.Lock("r", "n2", TimeSpan.FromSeconds(1), _5Inst, _log, NoopRedlockRepeater.Instance, 50);
        }

        [Fact]
        public void NoQuorumDoesNotObtainLock()
        {
            var l = Redlock.TryLock("r", "n", TimeSpan.FromSeconds(1), _noQuorum, _log);
            Assert.Null(l);
            Assert.NotEmpty(_log.Logs.Where(x => x.Exception != null && x.LogLevel == LogLevel.Error));
        }

        public void Dispose()
        {
            _log.Provider.WriteLogs(_console.WriteLine);
            _log?.Dispose();
        }
    }
}