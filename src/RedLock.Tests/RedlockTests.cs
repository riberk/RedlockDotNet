using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TestUtils;
using Xunit;

namespace RedLock.Tests
{
    public class RedlockTests
    {
        private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(1);
        
        private readonly MemoryLogger _log = new MemoryLogger();

        [Fact]
        public void TryLock()
        {
            var instances = MemInstances(3);

            var @lock = Redlock.TryLock("r", "n", Ttl, MemoryRedlockImpl.Create(instances), _log);
            
            Assert.NotNull(@lock);
            Assert.All(instances, i => Assert.True(i.Contains("r", "n")));
        }
        
        [Fact]
        public void Dispose()
        {
            var instances = MemInstances(3);
            var l = new Redlock("r", "n", MemoryRedlockImpl.Create(instances), _log);
            Lock("r", "n", instances);
            
            l.Dispose();

            Assert.All(instances, i => Assert.False(i.Contains("r", "n")));
        }

        [Fact]
        public async Task TryLockAsync()
        {
            var instances = MemInstances(3);
            
            var @lock = await Redlock.TryLockAsync("r", "n", Ttl, MemoryRedlockImpl.Create(instances), _log);
            
            Assert.NotNull(@lock);
            Assert.All(instances, i => Assert.True(i.Contains("r", "n")));
        }

        [Fact]
        public async Task DisposeAsync()
        {
            var instances = MemInstances(3);
            var l = new Redlock("r", "n", MemoryRedlockImpl.Create(instances), _log);
            Lock("r", "n", instances);
            
            await l.DisposeAsync();
         
            Assert.All(instances, i => Assert.False(i.Contains("r", "n")));
        }

        [Fact]
        public void TryLock_NoQuorum()
        {
            var instances = MemInstances(3);
            Lock("r", "n2", instances[0], instances[1]);
            
            var l = Redlock.TryLock("r", "n", Ttl, MemoryRedlockImpl.Create(instances), _log);
            
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
            
            var l = await Redlock.TryLockAsync("r", "n", Ttl, MemoryRedlockImpl.Create(instances), _log);
            
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

            var l = Redlock.TryLock("r", "n", Ttl, MemoryRedlockImpl.Create(mem, err), _log);

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

            var l = Redlock.TryLock("r", "n", Ttl, MemoryRedlockImpl.Create(mem, err), _log);

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

            var l = await Redlock.TryLockAsync("r", "n", Ttl, MemoryRedlockImpl.Create(mem, err), _log);

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

            var l = await Redlock.TryLockAsync("r", "n", Ttl, MemoryRedlockImpl.Create(mem, err), _log);

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
            var l = new Redlock("r", "n", MemoryRedlockImpl.Create(mem, err), _log);
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
            var l = new Redlock("r", "n", MemoryRedlockImpl.Create(mem, err), _log);
            Lock("r", "n", mem);
            
            await l.DisposeAsync();

            Assert.All(mem, i => Assert.False(i.Contains("r", "n")));
            var errorLogs = _log.Logs.Where(x => x.LogLevel == LogLevel.Error).ToArray();
            Assert.Equal(2, errorLogs.Length);
            Assert.Contains(errorLogs, e => e.Exception == err[0].UnlockAsyncException);
            Assert.Contains(errorLogs, e => e.Exception == err[1].UnlockAsyncException);
        }

        private static void Lock(string key, string nonce, params MemoryRedlockInstance[] instances)
        {
            if (instances.Any(instance => !instance.TryLock(key, nonce, TimeSpan.FromDays(10))))
            {
                throw new InvalidOperationException($"Already locked: ['{key}'] = '{nonce}'");
            }
        }

        private static T[] Instances<T>(int count, Func<T> create)
        {
            var arr = new T[count];
            for (int i = 0; i < count; i++)
            {
                arr[i] = create();
            }

            return arr;
        }

        private static MemoryRedlockInstance[] MemInstances(int count) 
            => Instances(count, () => new MemoryRedlockInstance());
        
        private static ExceptionRedlockInstance[] ErrInstances(int count) 
            => Instances(count, () => new ExceptionRedlockInstance());
        
        public class MemoryRedlockImpl : IRedlockImplementation
        {
            public MemoryRedlockImpl(ImmutableArray<IRedlockInstance> instances)
            {
                Instances = instances;
            }

            public TimeSpan MinValidity(TimeSpan lockTimeToLive, TimeSpan lockingDuration)
            {
                return lockTimeToLive - lockingDuration - lockTimeToLive * 0.01;
            }

            public ImmutableArray<IRedlockInstance> Instances { get; }

            public static MemoryRedlockImpl Create<T>(IEnumerable<T> instances)
                where T: IRedlockInstance
            {
                return new MemoryRedlockImpl(instances.Cast<IRedlockInstance>().ToImmutableArray());
            }
            
            public static MemoryRedlockImpl Create(params IEnumerable<IRedlockInstance>[] instances)
            {
                return new MemoryRedlockImpl(instances.SelectMany(s => s).ToImmutableArray());
            }
        }
        
        public class ExceptionRedlockInstance : IRedlockInstance
        {
            public readonly Exception TryLockException = new Exception("lock"); 
            public readonly Exception TryLockAsyncException = new Exception("lock async"); 
            public readonly Exception UnlockException = new Exception("unlock"); 
            public readonly Exception UnlockAsyncException = new Exception("unlock async"); 
            public bool TryLock(string resource, string nonce, TimeSpan lockTimeToLive)
            {
                throw TryLockException;
            }

            public Task<bool> TryLockAsync(string resource, string nonce, TimeSpan lockTimeToLive)
            {
                throw TryLockAsyncException;
            }

            public void Unlock(string resource, string nonce)
            {
                throw UnlockException;
            }

            public Task UnlockAsync(string resource, string nonce)
            {
                throw UnlockAsyncException;
            }
        }
        
        public class MemoryRedlockInstance : IRedlockInstance
        {
            private readonly Dictionary<string, string> _data = new Dictionary<string, string>();
            
            public bool TryLock(string resource, string nonce, TimeSpan lockTimeToLive)
            {
                lock (this)
                {
                    var _ = UnlockAfter(resource, nonce, lockTimeToLive);
                    return _data.TryAdd(resource, nonce);
                }
            }

            private async Task UnlockAfter(string resource, string nonce, TimeSpan lockTimeToLive)
            {
                await Task.Delay(lockTimeToLive);
                // ReSharper disable once MethodHasAsyncOverload
                Unlock(resource, nonce);
            }

            public Task<bool> TryLockAsync(string resource, string nonce, TimeSpan lockTimeToLive)
            {
                return Task.FromResult(TryLock(resource, nonce, lockTimeToLive));
            }

            public void Unlock(string resource, string nonce)
            {
                lock (this)
                {
                    if (_data.TryGetValue(resource, out var actualNonce) && actualNonce == nonce)
                    {
                        _data.Remove(resource);
                    }
                }
            }

            public Task UnlockAsync(string resource, string nonce)
            {
                Unlock(resource, nonce);
                return Task.CompletedTask;
            }

            public bool Contains(string resource, string nonce)
            {
                lock (this)
                {
                    return _data.TryGetValue(resource, out var actualNonce) && actualNonce == nonce;
                }
            }
        }
    }
}