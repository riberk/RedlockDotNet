using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using RedlockDotNet.Repeaters;
using TestUtils;
using Xunit;

namespace RedlockDotNet
{
    public class RedlockTests
    {
        private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(1);
        
        private readonly MemoryLogger _log = new MemoryLogger();

        [Fact]
        public void TryLock()
        {
            var instances = MemInstances(3);

            var @lock = Redlock.TryLock("r", "n", Ttl, TestRedlockImpl.Create(instances), _log);
            
            Assert.NotNull(@lock);
            Assert.All(instances, i => Assert.True(i.Contains("r", "n")));
        }
        
        [Fact]
        public void Dispose()
        {
            var instances = MemInstances(3);
            var l = new Redlock("r", "n", TestRedlockImpl.Create(instances), _log);
            Lock("r", "n", instances);
            
            l.Dispose();

            Assert.All(instances, i => Assert.False(i.Contains("r", "n")));
        }

        [Fact]
        public async Task TryLockAsync()
        {
            var instances = MemInstances(3);
            
            var @lock = await Redlock.TryLockAsync("r", "n", Ttl, TestRedlockImpl.Create(instances), _log);
            
            Assert.NotNull(@lock);
            Assert.All(instances, i => Assert.True(i.Contains("r", "n")));
        }

        [Fact]
        public async Task DisposeAsync()
        {
            var instances = MemInstances(3);
            var l = new Redlock("r", "n", TestRedlockImpl.Create(instances), _log);
            Lock("r", "n", instances);
            
            await l.DisposeAsync();
         
            Assert.All(instances, i => Assert.False(i.Contains("r", "n")));
        }

        [Fact]
        public void TryLock_NoQuorum()
        {
            var instances = MemInstances(3);
            Lock("r", "n2", instances[0], instances[1]);
            
            var l = Redlock.TryLock("r", "n", Ttl, TestRedlockImpl.Create(instances), _log);
            
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
            
            var l = await Redlock.TryLockAsync("r", "n", Ttl, TestRedlockImpl.Create(instances), _log);
            
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

            var l = Redlock.TryLock("r", "n", Ttl, TestRedlockImpl.Create(mem, err), _log);

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

            var l = Redlock.TryLock("r", "n", Ttl, TestRedlockImpl.Create(mem, err), _log);

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

            var l = await Redlock.TryLockAsync("r", "n", Ttl, TestRedlockImpl.Create(mem, err), _log);

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

            var l = await Redlock.TryLockAsync("r", "n", Ttl, TestRedlockImpl.Create(mem, err), _log);

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
            var l = new Redlock("r", "n", TestRedlockImpl.Create(mem, err), _log);
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
            var l = new Redlock("r", "n", TestRedlockImpl.Create(mem, err), _log);
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
                Redlock.Lock("r", "n", Ttl, TestRedlockImpl.Create(mem), _log, repeater.Object, 600)
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
            Assert.Throws<RedlockException>(() => Redlock.Lock("r", "n", Ttl, TestRedlockImpl.Create(mem), _log, repeater.Object, 600));
        }
        
        [Fact]
        public void TryLockWithRepeater_UnableToObtainLock()
        {
            var mem = MemInstances(3);
            Lock("r", "n2", mem);
            var repeater = new Mock<IRedlockRepeater>(MockBehavior.Strict);
            repeater.Setup(x => x.Next()).Returns(false);
            var l = Redlock.TryLock("r", "n", Ttl, TestRedlockImpl.Create(mem), _log, repeater.Object, 600);
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
            var lockTask = Task.Run(() => Redlock.LockAsync("r", "n", Ttl, TestRedlockImpl.Create(mem), _log, repeater.Object, 600));
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
            await Assert.ThrowsAsync<RedlockException>(() => Redlock.LockAsync("r", "n", Ttl, TestRedlockImpl.Create(mem), _log, repeater.Object, 600));
        }
        
        [Fact]
        public async Task TryLockWithRepeaterAsync_UnableToObtainLock()
        {
            var mem = MemInstances(3);
            Lock("r", "n2", mem);
            var repeater = new Mock<IRedlockRepeater>(MockBehavior.Strict);
            repeater.Setup(x => x.Next()).Returns(false);
            var l = await Redlock.TryLockAsync("r", "n", Ttl, TestRedlockImpl.Create(mem), _log, repeater.Object, 600);
            Assert.Null(l);
        }

        private static void Lock(string key, string nonce, params MemoryRedlockInstance[] instances)
            => MemoryRedlockInstance.Lock(key, nonce, instances);
        
        private static void Unlock(string key, params MemoryRedlockInstance[] instances)
            => MemoryRedlockInstance.Unlock(key, instances);

        private static T[] Instances<T>(int count, Func<T> create) => TestRedlockImpl.CreateInstances(count, create);

        private static MemoryRedlockInstance[] MemInstances(int count) 
            => Instances(count, () => new MemoryRedlockInstance());
        
        private static ExceptionRedlockInstance[] ErrInstances(int count) 
            => Instances(count, () => new ExceptionRedlockInstance());
    }
}