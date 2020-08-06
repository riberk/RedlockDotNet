using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RedlockDotNet;

namespace TestUtils
{
    public class MemoryRedlockInstance : IRedlockInstance
    {
        public string Name { get; }

        public MemoryRedlockInstance(string name)
        {
            Name = name;
        }
        private readonly Dictionary<string, string> _data = new Dictionary<string, string>();
        private readonly Dictionary<string, CancellationTokenSource> _unlockTaskCancellations = new Dictionary<string, CancellationTokenSource>();
            
        public bool TryLock(string resource, string nonce, TimeSpan lockTimeToLive)
        {
            lock (this)
            {
                return TryAddInternal(resource, nonce, lockTimeToLive);
            }
        }

        private bool TryAddInternal(string resource, string nonce, TimeSpan lockTimeToLive)
        {
            var cts = new CancellationTokenSource();
            _unlockTaskCancellations.Add(resource, cts);
            var _ = UnlockAfter(resource, nonce, lockTimeToLive, cts.Token);
            return _data.TryAdd(resource, nonce);
        }

        private async Task UnlockAfter(string resource, string nonce, TimeSpan lockTimeToLive, CancellationToken cancellationToken)
        {
            await Task.Delay(lockTimeToLive, cancellationToken);
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
                    RemoveCancellation(resource);
                }
            }
        }

        public Task UnlockAsync(string resource, string nonce)
        {
            Unlock(resource, nonce);
            return Task.CompletedTask;
        }

        public ExtendResult TryExtend(string resource, string nonce, TimeSpan lockTimeToLive, bool tryReacquire)
        {
            lock (this)
            {
                if (_data.TryGetValue(resource, out var actualNonce))
                {
                    if (actualNonce != nonce)
                    {
                        return ExtendResult.AlreadyAcquiredByAnotherOwner;
                    }

                    RemoveCancellation(resource);
                    _data.Remove(resource);
                    TryAddInternal(resource, nonce, lockTimeToLive);
                    return ExtendResult.Extend;
                }

                if (!tryReacquire)
                {
                    return ExtendResult.IllegalReacquire;
                }
                TryAddInternal(resource, nonce, lockTimeToLive);
                return ExtendResult.Reacquire;
            }
        }

        public Task<ExtendResult> TryExtendAsync(string resource, string nonce, TimeSpan lockTimeToLive, bool tryReacquire)
        {
            return Task.FromResult(TryExtend(resource, nonce, lockTimeToLive, tryReacquire));
        }

        private void RemoveCancellation(string resource)
        {
            lock (this)
            {
                if (!_unlockTaskCancellations.Remove(resource, out var cts))
                {
                    return;
                }
                cts.Cancel();
                cts.Dispose();
            }
        }

        public bool Contains(string resource, string nonce)
        {
            lock (this)
            {
                return _data.TryGetValue(resource, out var actualNonce) && actualNonce == nonce;
            }
        }
            
        public void Unlock(string resource)
        {
            lock (this)
            {
                _data.Remove(resource);
                RemoveCancellation(resource);
            }
        }
            
        public static void Lock(string key, string nonce, params MemoryRedlockInstance[] instances)
        {
            foreach (var instance in instances)
            {
                if (!instance.TryLock(key, nonce, TimeSpan.FromDays(10)))
                {
                    throw new InvalidOperationException($"Already locked: ['{key}'] = '{nonce}'");
                }
            }
        }
        
        public static void Unlock(string key, params MemoryRedlockInstance[] instances)
        {
            foreach (var instance in instances)
            {
                instance.Unlock(key);
            }
        }

        public override string ToString()
        {
            return $"mem:{Name}";
        }
    }
}