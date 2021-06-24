using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using RedlockDotNet.Internal;
using RedlockDotNet.Repeaters;
using TestUtils;
using Xunit;
using Xunit.Abstractions;

namespace RedlockDotNet
{
    [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
    public class RedlockTests : IDisposable
    {
        private readonly ITestOutputHelper _console;
        private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(1);
        
        private readonly MemoryLogger _log = new MemoryLogger();
        private readonly DateTime _now = DateTime.UtcNow;
        private ImmutableArray<IRedlockInstance> _emptyInstances;

        public RedlockTests(ITestOutputHelper console)
        {
            _console = console;
            _emptyInstances = ImmutableArray<IRedlockInstance>.Empty;

        }
        
        [Fact]
        public void TryLock()
        {
            var instances = MemInstances(3);

            var @lock = Redlock.TryLock("r", "n", Ttl, instances.ToInstances(), _log);
            
            Assert.NotNull(@lock);
            Assert.All(instances, i => Assert.True(i.Contains("r", "n")));
        }
        
        [Fact]
        public void DisposeTest()
        {
            var instances = MemInstances(3);
            var l = new Redlock("r", "n", Ttl, _now, instances.ToInstances(), _log);
            Lock("r", "n", instances);
            
            l.Dispose();

            Assert.All(instances, i => Assert.False(i.Contains("r", "n")));
        }

        [Fact]
        public async Task TryLockAsync()
        {
            var instances = MemInstances(3);
            
            var @lock = await Redlock.TryLockAsync("r", "n", Ttl, instances.ToInstances(), _log);
            
            Assert.NotNull(@lock);
            Assert.All(instances, i => Assert.True(i.Contains("r", "n")));
        }

        [Fact]
        public async Task DisposeAsync()
        {
            var instances = MemInstances(3);
            var l = new Redlock("r", "n", Ttl, _now, instances.ToInstances(), _log);
            Lock("r", "n", instances);
            
            await l.DisposeAsync();
         
            Assert.All(instances, i => Assert.False(i.Contains("r", "n")));
        }

        [Fact]
        public void TryLock_NoQuorum()
        {
            var instances = MemInstances(3);
            Lock("r", "n2", instances[0], instances[1]);
            
            var l = Redlock.TryLock("r", "n", Ttl, instances.ToInstances(), _log);
            
            Assert.Null(l);
            Assert.All(instances, i => Assert.False(i.Contains("r", "n")));
            Assert.True(instances[0].Contains("r", "n2"));
            Assert.True(instances[1].Contains("r", "n2"));
            Assert.False(instances[2].Contains("r", "n2"));
        }
        
        [Fact]
        public async Task TryLockAsync_NoQuorum()
        {
            var instances = MemInstances(3);
            Lock("r", "n2", instances[0], instances[1]);
            
            var l = await Redlock.TryLockAsync("r", "n", Ttl, instances.ToInstances(), _log);
            
            Assert.Null(l);
            Assert.All(instances, i => Assert.False(i.Contains("r", "n")));
            Assert.True(instances[0].Contains("r", "n2"));
            Assert.True(instances[1].Contains("r", "n2"));
            Assert.False(instances[2].Contains("r", "n2"));
        }

        [Fact]
        public void TryLock_ExceptionsOnLock_Quorum()
        {
            var mem = MemInstances(3);
            var err = ErrInstances(2);
            
            var l = Redlock.TryLock("r", "n", Ttl, TestRedlock.Instances(mem, err), _log);

            Assert.NotNull(l);
            Assert.All(mem, i => i.Contains("r", "n"));
            var errorLogs = _log.Logs.Where(x => x.LogLevel == LogLevel.Error).ToArray();
            Assert.Equal(2, errorLogs.Length);
            Assert.Contains(errorLogs, e => e.Exception == err[0].TryLockException);
            Assert.Contains(errorLogs, e => e.Exception == err[1].TryLockException);
        }
        
        [Fact]
        public void TryLock_ExceptionsOnLock_NoQuorum()
        {
            var mem = MemInstances(2);
            var err = ErrInstances(3);

            var l = Redlock.TryLock("r", "n", Ttl, TestRedlock.Instances(mem, err), _log);

            Assert.Null(l);
            Assert.All(mem, i => Assert.False(i.Contains("r", "n")));
            var errorLogs = _log.Logs.Where(x => x.LogLevel == LogLevel.Error).ToArray();
            Assert.Equal(6, errorLogs.Length);
            Assert.Contains(errorLogs, e => e.Exception == err[0].TryLockException);
            Assert.Contains(errorLogs, e => e.Exception == err[1].TryLockException);
            Assert.Contains(errorLogs, e => e.Exception == err[2].TryLockException);
            Assert.Contains(errorLogs, e => e.Exception == err[0].UnlockException);
            Assert.Contains(errorLogs, e => e.Exception == err[1].UnlockException);
            Assert.Contains(errorLogs, e => e.Exception == err[2].UnlockException);
        }
        
        [Fact]
        public async Task TryLockAsync_ExceptionsOnLock_NoQuorum()
        {
            var mem = MemInstances(2);
            var err = ErrInstances(3);

            var l = await Redlock.TryLockAsync("r", "n", Ttl, TestRedlock.Instances(mem, err), _log);

            Assert.Null(l);
            Assert.All(mem, i => Assert.False(i.Contains("r", "n")));
            var errorLogs = _log.Logs.Where(x => x.LogLevel == LogLevel.Error).ToArray();
            Assert.Equal(6, errorLogs.Length);
            Assert.Contains(errorLogs, e => e.Exception == err[0].TryLockAsyncException);
            Assert.Contains(errorLogs, e => e.Exception == err[1].TryLockAsyncException);
            Assert.Contains(errorLogs, e => e.Exception == err[2].TryLockAsyncException);
            Assert.Contains(errorLogs, e => e.Exception == err[0].UnlockAsyncException);
            Assert.Contains(errorLogs, e => e.Exception == err[1].UnlockAsyncException);
            Assert.Contains(errorLogs, e => e.Exception == err[2].UnlockAsyncException);
        }
        
        [Fact]
        public async Task TryLockAsync_ExceptionsOnLock_Quorum()
        {
            var mem = MemInstances(3);
            var err = ErrInstances(2);

            var l = await Redlock.TryLockAsync("r", "n", Ttl, TestRedlock.Instances(mem, err), _log);

            Assert.NotNull(l);
            Assert.All(mem, i => i.Contains("r", "n"));
            var errorLogs = _log.Logs.Where(x => x.LogLevel == LogLevel.Error).ToArray();
            Assert.Equal(2, errorLogs.Length);
            Assert.Contains(errorLogs, e => e.Exception == err[0].TryLockAsyncException);
            Assert.Contains(errorLogs, e => e.Exception == err[1].TryLockAsyncException);
        }
        
        [Fact]
        public void Dispose_ExceptionsOnUnlock()
        {
            var mem = MemInstances(3);
            var err = ErrInstances(2);
            var l = new Redlock("r", "n", Ttl, _now, TestRedlock.Instances(mem, err), _log);
            Lock("r", "n", mem);
            
            l.Dispose();

            Assert.All(mem, i => Assert.False(i.Contains("r", "n")));
            var errorLogs = _log.Logs.Where(x => x.LogLevel == LogLevel.Error).ToArray();
            Assert.Equal(2, errorLogs.Length);
            Assert.Contains(errorLogs, e => e.Exception == err[0].UnlockException);
            Assert.Contains(errorLogs, e => e.Exception == err[1].UnlockException);
        }
        
        [Fact]
        public async Task DisposeAsync_ExceptionsOnUnlock()
        {
            var mem = MemInstances(3);
            var err = ErrInstances(2);
            var l = new Redlock("r", "n", Ttl, _now, TestRedlock.Instances(mem, err), _log);
            Lock("r", "n", mem);
            
            await l.DisposeAsync();

            Assert.All(mem, i => Assert.False(i.Contains("r", "n")));
            var errorLogs = _log.Logs.Where(x => x.LogLevel == LogLevel.Error).ToArray();
            Assert.Equal(2, errorLogs.Length);
            Assert.Contains(errorLogs, e => e.Exception == err[0].UnlockAsyncException);
            Assert.Contains(errorLogs, e => e.Exception == err[1].UnlockAsyncException);
        }

        [Fact]
        public async Task LockWithRepeater()
        {
            using var waitAre = new AutoResetEvent(false);
            using var waitInvoked = new AutoResetEvent(false);
            var mem = MemInstances(3);
            Lock("r", "n2", mem);
            var repeater = new Mock<IRedlockRepeater>(MockBehavior.Strict);
            repeater.Setup(x => x.WaitRandom(600)).Callback(() =>
            {
                waitInvoked.Set();
                Assert.True(waitAre.WaitOne(2000));
            });
            repeater.Setup(x => x.Next()).Returns(true);
            var lockTask = Task.Run(() =>
                Redlock.Lock("r", "n", Ttl, mem.ToInstances(), _log, repeater.Object, 600)
            );
            Assert.True(waitInvoked.WaitOne(2000));
            repeater.Verify(x => x.Next(), Times.Once);
            Unlock("r", mem);
            waitAre.Set();
            await lockTask;
            Assert.All(mem, i => Assert.True(i.Contains("r", "n")));
        }
        
        [Fact]
        public void LockWithRepeater_UnableToObtainLock()
        {
            var mem = MemInstances(3);
            Lock("r", "n2", mem);
            var repeater = new Mock<IRedlockRepeater>(MockBehavior.Strict);
            repeater.Setup(x => x.Next()).Returns(false);
            var expected = new Exception();
            repeater.Setup(x => x.CreateException("r", "n", 1)).Returns(expected);
            var actual = Assert.Throws<Exception>(() => Redlock.Lock("r", "n", Ttl, mem.ToInstances(), _log, repeater.Object, 600));
            Assert.Same(expected, actual);
        }
        
        [Fact]
        public void TryLockWithRepeater_UnableToObtainLock()
        {
            var mem = MemInstances(3);
            Lock("r", "n2", mem);
            var repeater = new Mock<IRedlockRepeater>(MockBehavior.Strict);
            repeater.Setup(x => x.Next()).Returns(false);
            var l = Redlock.TryLock("r", "n", Ttl, mem.ToInstances(), _log, repeater.Object, 600);
            Assert.Null(l);
        }
        
        [Fact]
        public async Task LockWithRepeaterAsync()
        {
            using var waitAre = new AutoResetEvent(false);
            using var waitInvoked = new AutoResetEvent(false);
            var mem = MemInstances(3);
            Lock("r", "n2", mem);
            var repeater = new Mock<IRedlockRepeater>(MockBehavior.Strict);
            repeater.Setup(x => x.WaitRandomAsync(600, CancellationToken.None))
                .Returns(() =>
                {
                    waitInvoked.Set();
                    Assert.True(waitAre.WaitOne(2000));
                    return new ValueTask();
                });
            repeater.Setup(x => x.Next()).Returns(true);
            var lockTask = Task.Run(() => Redlock.LockAsync("r", "n", Ttl, mem.ToInstances(), _log, repeater.Object, 600));

            Assert.True(waitInvoked.WaitOne(2000));
            repeater.Verify(x => x.Next(), Times.Once);
            Unlock("r", mem);
            waitAre.Set();
            await lockTask;
            Assert.All(mem, i => Assert.True(i.Contains("r", "n")));
        }
        
        [Fact]
        public async Task LockWithRepeaterAsync_UnableToObtainLock()
        {
            var mem = MemInstances(3);
            Lock("r", "n2", mem);
            var repeater = new Mock<IRedlockRepeater>(MockBehavior.Strict);
            repeater.Setup(x => x.Next()).Returns(false);
            var expected = new Exception();
            repeater.Setup(x => x.CreateException("r", "n", 1)).Returns(expected);
            var actual = await Assert.ThrowsAsync<Exception>(() => Redlock.LockAsync("r", "n", Ttl, mem.ToInstances(), _log, repeater.Object, 600));
            Assert.Same(expected, actual);
        }
        
        [Fact]
        public async Task TryLockWithRepeaterAsync_UnableToObtainLock()
        {
            var mem = MemInstances(3);
            Lock("r", "n2", mem);
            var repeater = new Mock<IRedlockRepeater>(MockBehavior.Strict);
            repeater.Setup(x => x.Next()).Returns(false);
            var l = await Redlock.TryLockAsync("r", "n", Ttl, mem.ToInstances(), _log, repeater.Object, 600);
            Assert.Null(l);
        }
        
        [Fact]
        public void TryExtend()
        {
            var instances = MemInstances(3, (ttl, duration) => ttl);
            var impl = instances.ToInstances();
            Lock("r", "n", instances);
            var l = new Redlock("r", "n", Ttl, _now.AddDays(10), impl, _log);
            var actualValidUntil = l.TryExtend(() => _now);
            
            Assert.Equal(_now.Add(Ttl), actualValidUntil);
            Assert.All(instances, i => Assert.True(i.Contains("r", "n")));
        }
        
        [Fact]
        public async Task TryExtendAsync()
        {
            var instances = MemInstances(3, (ttl, duration) => ttl);
            var impl = instances.ToInstances();
            Lock("r", "n", TimeSpan.FromSeconds(10), instances);
            var l = new Redlock("r", "n", Ttl, _now.AddDays(10), impl, _log);
            
            var actualValidUntil = await l.TryExtendAsync(() => _now);
            
            Assert.Equal(_now.Add(Ttl), actualValidUntil);
            Assert.All(instances, i => Assert.True(i.Contains("r", "n")));
        }
        
        [Fact]
        public async Task ExtendWithRepeater()
        {
            using var waitAre = new AutoResetEvent(false);
            using var waitInvoked = new AutoResetEvent(false);
            var mem = MemInstances(3, (ttl, duration) => ttl);
            Lock("r", "n2", mem);
            var repeater = new Mock<IRedlockRepeater>(MockBehavior.Strict);
            repeater.Setup(x => x.WaitRandom(600)).Callback(() =>
            {
                waitInvoked.Set();
                Assert.True(waitAre.WaitOne(2000));
            });
            repeater.Setup(x => x.Next()).Returns(true);
            var task = Task.Run(() =>
            {
                var l = new Redlock("r", "n", Ttl, _now, mem.ToInstances(), _log);
                return l.Extend(repeater.Object, 600, () => _now);
            });
            Assert.True(waitInvoked.WaitOne(2000));
            repeater.Verify(x => x.Next(), Times.Once);
            Unlock("r", mem);
            Lock("r", "n", mem);
            waitAre.Set();
            var actualValidUntil = await task;
            Assert.All(mem, i => Assert.True(i.Contains("r", "n")));
            Assert.Equal(_now.Add(Ttl), actualValidUntil);
        }
        
        [Fact]
        public void ExtendWithRepeater_UnableToObtainLock()
        {
            var mem = MemInstances(3, (ttl, duration) => ttl);
            Lock("r", "n2", mem);
            var repeater = new Mock<IRedlockRepeater>(MockBehavior.Strict);
            repeater.Setup(x => x.Next()).Returns(false);
            var expected = new Exception();
            repeater.Setup(x => x.CreateException("r", "n", 1)).Returns(expected);
            var l = new Redlock("r", "n", Ttl, _now, mem.ToInstances(), _log);
            var actual = Assert.Throws<Exception>(() => l.Extend(repeater.Object, 600));
            Assert.Same(expected, actual);
        }
        
        [Fact]
        public void TryExtendWithRepeater_UnableToObtainLock()
        {
            var mem = MemInstances(3, (ttl, duration) => ttl);
            Lock("r", "n2", mem);
            var repeater = new Mock<IRedlockRepeater>(MockBehavior.Strict);
            repeater.Setup(x => x.Next()).Returns(false);
            var l = new Redlock("r", "n", Ttl, _now, mem.ToInstances(), _log);
            var actualValidUntil = l.TryExtend(repeater.Object, 600);
            Assert.Null(actualValidUntil);
        }
        
        [Fact]
        public async Task ExtendWithRepeaterAsync()
        {
            using var waitAre = new AutoResetEvent(false);
            using var waitInvoked = new AutoResetEvent(false);
            var mem = MemInstances(3, (ttl, duration) => ttl);
            Lock("r", "n2", mem);
            var repeater = new Mock<IRedlockRepeater>(MockBehavior.Strict);
            repeater.Setup(x => x.WaitRandomAsync(600, default)).Returns(new ValueTask()).Callback(() =>
            {
                waitInvoked.Set();
                Assert.True(waitAre.WaitOne(2000));
            });
            repeater.Setup(x => x.Next()).Returns(true);
            var task = Task.Run(async () =>
            {
                var l = new Redlock("r", "n", Ttl, _now, mem.ToInstances(), _log);
                return await l.ExtendAsync(repeater.Object, 600, () => _now);
            });
            Assert.True(waitInvoked.WaitOne(2000));
            repeater.Verify(x => x.Next(), Times.Once);
            Unlock("r", mem);
            Lock("r", "n", mem);
            waitAre.Set();
            var actualValidUntil = await task;
            Assert.All(mem, i => Assert.True(i.Contains("r", "n")));
            Assert.Equal(_now.Add(Ttl), actualValidUntil);
        }
        
        [Fact]
        public async Task ExtendWithRepeaterAsync_UnableToObtainLock()
        {
            var mem = MemInstances(3, (ttl, duration) => ttl);
            Lock("r", "n2", mem);
            var repeater = new Mock<IRedlockRepeater>(MockBehavior.Strict);
            repeater.Setup(x => x.Next()).Returns(false);
            var expected = new Exception();
            repeater.Setup(x => x.CreateException("r", "n", 1)).Returns(expected);
            var l = new Redlock("r", "n", Ttl, _now, mem.ToInstances(), _log);
            var actual = await Assert.ThrowsAsync<Exception>(() => l.ExtendAsync(repeater.Object, 600));
            Assert.Same(expected, actual);
        }
        
        [Fact]
        public async Task TryExtendWithRepeaterAsync_UnableToObtainLock()
        {
            var mem = MemInstances(3, (ttl, duration) => ttl);
            Lock("r", "n2", mem);
            var repeater = new Mock<IRedlockRepeater>(MockBehavior.Strict);
            repeater.Setup(x => x.Next()).Returns(false);
            var l = new Redlock("r", "n", Ttl, _now, mem.ToInstances(), _log);
            var actualValidUntil = await l.TryExtendAsync(repeater.Object, 600);
            Assert.Null(actualValidUntil);
        }
        
        [Fact]
        public void TryExtend_Quorum_Errors()
        {
            var err = ErrInstances(2, (ttl, duration) => ttl);
            var mem = MemInstances(3, (ttl, duration) => ttl);
            Lock("r", "n", mem);
            var l = new Redlock("r", "n", Ttl, _now, TestRedlock.Instances(mem, err), _log);
            var actualValidUntil = l.TryExtend(() => _now);
            Assert.NotNull(actualValidUntil);
            Assert.Equal(_now.Add(Ttl), actualValidUntil);
            var errorLogs = _log.Logs.Where(x => x.LogLevel == LogLevel.Error).ToArray();
            Assert.Equal(2, errorLogs.Length);
            Assert.Contains(errorLogs, e => e.Exception == err[0].TryExtendException);
            Assert.Contains(errorLogs, e => e.Exception == err[1].TryExtendException);
        }
        
        [Fact]
        public async Task TryExtendAsync_Quorum_Errors()
        {
            var err = ErrInstances(2, (ttl, duration) => ttl);
            var mem = MemInstances(3, (ttl, duration) => ttl);
            Lock("r", "n", mem);
            var l = new Redlock("r", "n", Ttl, _now, TestRedlock.Instances(mem, err), _log);
            var actualValidUntil = await l.TryExtendAsync(() => _now);
            Assert.NotNull(actualValidUntil);
            Assert.Equal(_now.Add(Ttl), actualValidUntil);
            var errorLogs = _log.Logs.Where(x => x.LogLevel == LogLevel.Error).ToArray();
            Assert.Equal(2, errorLogs.Length);
            Assert.Contains(errorLogs, e => e.Exception == err[0].TryExtendAsyncException);
            Assert.Contains(errorLogs, e => e.Exception == err[1].TryExtendAsyncException);
        }
        
        [Fact]
        public void TryExtend_NoQuorum_Errors()
        {
            var err = ErrInstances(3);
            var l = new Redlock("r", "n", Ttl, _now, err.ToInstances(), _log);
            var actualValidUntil = l.TryExtend(() => _now);
            Assert.Null(actualValidUntil);
            var errorLogs = _log.Logs.Where(x => x.LogLevel == LogLevel.Error).ToArray();
            Assert.Equal(3, errorLogs.Length);
            Assert.Contains(errorLogs, e => e.Exception == err[0].TryExtendException);
            Assert.Contains(errorLogs, e => e.Exception == err[1].TryExtendException);
            Assert.Contains(errorLogs, e => e.Exception == err[2].TryExtendException);
        }
        
        [Fact]
        public async Task TryExtendAsync_NoQuorum_Errors()
        {
            var err = ErrInstances(3);
            var l = new Redlock("r", "n", Ttl, _now, err.ToInstances(), _log);
            var actualValidUntil = await l.TryExtendAsync(() => _now);
            Assert.Null(actualValidUntil);
            var errorLogs = _log.Logs.Where(x => x.LogLevel == LogLevel.Error).ToArray();
            Assert.Equal(3, errorLogs.Length);
            Assert.Contains(errorLogs, e => e.Exception == err[0].TryExtendAsyncException);
            Assert.Contains(errorLogs, e => e.Exception == err[1].TryExtendAsyncException);
            Assert.Contains(errorLogs, e => e.Exception == err[2].TryExtendAsyncException);
        }
        
        [Fact]
        public static void DisposeDefaultStruct() => new Redlock().Dispose();

        [Fact]
        public static async Task DisposeAsyncDefaultStruct() => await new Redlock().DisposeAsync();

        [Fact]
        public void TryLock_ValidUntil()
        {
            var mem = MemInstances(3, (ttl, duration) => TimeSpan.FromSeconds(10));
            var impl = mem.ToInstances();
            var now = new DateTime(2020, 7, 21, 13, 00, 00, DateTimeKind.Utc);
            var l = Redlock.TryLock("r", "n", Ttl, impl, _log, () => now);
            Assert.NotNull(l);
            Assert.Equal(new DateTime(2020, 7, 21, 13, 00, 10, DateTimeKind.Utc), l!.Value.ValidUntilUtc);
        }
        
        [Fact]
        public void TryLock_EmptyInstances()
        {
            var now = new DateTime(2020, 7, 21, 13, 00, 00, DateTimeKind.Utc);
            var l = Redlock.TryLock("r", "n", Ttl, _emptyInstances, _log, () => now);
            Assert.Null(l);
        }
        
        [Fact]
        public async Task TryLockAsync_EmptyInstances()
        {
            var now = new DateTime(2020, 7, 21, 13, 00, 00, DateTimeKind.Utc);
            var l = await Redlock.TryLockAsync("r", "n", Ttl, _emptyInstances, _log, () => now);
            Assert.Null(l);
        }
        
        [Fact]
        public void TryExtend_EmptyInstances()
        {
            var l = new Redlock("r", "n", Ttl, _now, _emptyInstances, _log);
            Assert.Null(l.TryExtend());
        }
        
        [Fact]
        public async Task TryExtendAsync_EmptyInstances()
        {
            var l = new Redlock("r", "n", Ttl, _now, _emptyInstances, _log);
            Assert.Null(await l.TryExtendAsync());
        }

        private static void Lock(string key, string nonce, ImmutableArray<MemoryRedlockInstance> instances)
            => MemoryRedlockInstance.Lock(key, nonce, instances);
        
        private static void Lock(string key, string nonce, TimeSpan ttl, ImmutableArray<MemoryRedlockInstance> instances)
            => MemoryRedlockInstance.Lock(key, nonce, ttl, instances);
        
        private static void Lock(string key, string nonce, params MemoryRedlockInstance[] instances)
            => MemoryRedlockInstance.Lock(key, nonce, instances.ToImmutableArray());
        
        private static void Unlock(string key, ImmutableArray<MemoryRedlockInstance> instances)
            => MemoryRedlockInstance.Unlock(key, instances);
        private static void Unlock(string key, params MemoryRedlockInstance[] instances)
            => MemoryRedlockInstance.Unlock(key, instances.ToImmutableArray());

        private static ImmutableArray<T> Instances<T>(int count, Func<int, T> create) => TestRedlock.Instances(count, create);

        private static ImmutableArray<MemoryRedlockInstance> MemInstances(int count) 
            =>  Instances(count, MemoryRedlockInstance.Create);
        
        private static ImmutableArray<MemoryRedlockInstance> MemInstances(int count, MinValidityDelegate minValidity) 
            =>  Instances(count, (i) => MemoryRedlockInstance.Create(i.ToString(), minValidity));
        private static ImmutableArray<ExceptionRedlockInstance> ErrInstances(int count) 
            => Instances(count, (i) => ExceptionRedlockInstance.Create(i.ToString()));
        
        private static ImmutableArray<ExceptionRedlockInstance> ErrInstances(int count, MinValidityDelegate minValidity) 
            =>  Instances(count, (i) => ExceptionRedlockInstance.Create(i.ToString(), minValidity));

        public void Dispose()
        {
            _log.Provider.WriteLogs(_console.WriteLine);
            _log.Dispose();
        }
    }
}