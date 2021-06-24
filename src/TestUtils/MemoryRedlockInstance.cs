using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using RedlockDotNet;

namespace TestUtils
{
    public delegate TimeSpan MinValidityDelegate(TimeSpan lockTimeToLive, TimeSpan lockingDuration);

    public class MemoryRedlockInstance : IRedlockInstance
    {
        private readonly MinValidityDelegate _minValidity;
        public string Name { get; }

        public MemoryRedlockInstance(string name, MinValidityDelegate minValidity)
        {
            Name = name;
            _minValidity = minValidity;
        }
        
        private readonly Dictionary<string, InstanceLockInfo> _data = new ();
        private readonly Dictionary<string, CancellationTokenSource> _unlockTaskCancellations = new();
        public TimeSpan MinValidity(TimeSpan lockTimeToLive, TimeSpan lockingDuration) => _minValidity(lockTimeToLive, lockingDuration);

        public bool TryLock(string resource, string nonce, TimeSpan lockTimeToLive, IReadOnlyDictionary<string, string>? metadata = null)
        {
            lock (this)
            {
                return TryAddInternal(resource, nonce, lockTimeToLive);
            }
        }

        private bool TryAddInternal(string resource, string nonce, TimeSpan lockTimeToLive, IReadOnlyDictionary<string, string>? metadata = null)
        {
            var cts = new CancellationTokenSource();
            _unlockTaskCancellations.Add(resource, cts);
            var _ = UnlockAfter(resource, nonce, lockTimeToLive, cts.Token);
            return _data.TryAdd(resource, new InstanceLockInfo(nonce, metadata ?? new Dictionary<string, string>(), lockTimeToLive));
        }

        private async Task UnlockAfter(string resource, string nonce, TimeSpan lockTimeToLive, CancellationToken cancellationToken)
        {
            await Task.Delay(lockTimeToLive, cancellationToken);
            // ReSharper disable once MethodHasAsyncOverload
            Unlock(resource, nonce);
        }

        public Task<bool> TryLockAsync(string resource, string nonce, TimeSpan lockTimeToLive, IReadOnlyDictionary<string, string>? metadata = null)
        {
            return Task.FromResult(TryLock(resource, nonce, lockTimeToLive, metadata));
        }

        public void Unlock(string resource, string nonce)
        {
            lock (this)
            {
                if (_data.TryGetValue(resource, out var actualNonce) && actualNonce.Nonce == nonce)
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

        public ExtendResult TryExtend(string resource, string nonce, TimeSpan lockTimeToLive)
        {
            lock (this)
            {
                if (_data.TryGetValue(resource, out var actualNonce))
                {
                    if (actualNonce.Nonce != nonce)
                    {
                        return ExtendResult.AlreadyAcquiredByAnotherOwner;
                    }

                    RemoveCancellation(resource);
                    _data.Remove(resource);
                    TryAddInternal(resource, nonce, lockTimeToLive);
                    return ExtendResult.Extend;
                }
                return ExtendResult.IllegalReacquire;
            }
        }

        public Task<ExtendResult> TryExtendAsync(string resource, string nonce, TimeSpan lockTimeToLive)
        {
            return Task.FromResult(TryExtend(resource, nonce, lockTimeToLive));
        }

        public InstanceLockInfo? GetInfo(string resource)
        {
            lock (this)
            {
                return _data.TryGetValue(resource, out var data) ? data : null;
            }
        }

        public Task<InstanceLockInfo?> GetInfoAsync(string resource)
        {
            return Task.FromResult(GetInfo(resource));
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
                return _data.TryGetValue(resource, out var actualNonce) && actualNonce.Nonce == nonce;
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

        public static void Lock(string key, string nonce, ImmutableArray<MemoryRedlockInstance> instances)
            => Lock(key, nonce, TimeSpan.FromDays(10), instances);
        
        public static void Lock(string key, string nonce, TimeSpan ttl, ImmutableArray<MemoryRedlockInstance> instances)
        {
            foreach (var instance in instances)
            {
                if (!instance.TryLock(key, nonce, ttl))
                {
                    throw new InvalidOperationException($"Already locked: ['{key}'] = '{nonce}'");
                }
            }
        }
        
        public static void Unlock(string key, ImmutableArray<MemoryRedlockInstance> instances)
        {
            foreach (var instance in instances)
            {
                instance.Unlock(key);
            }
        }

        public override string ToString() => $"mem:{Name}";

        public static MemoryRedlockInstance Create(string name) => Create(name, (ttl, duration) => ttl - duration);
        public static MemoryRedlockInstance Create(int i) => Create(i.ToString());
        
        public static MemoryRedlockInstance Create(string name, MinValidityDelegate minValidity) 
            => new MemoryRedlockInstance(name, minValidity);
    }
}