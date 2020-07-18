using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RedLock;

namespace TestUtils
{
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
            
        public void Unlock(string resource)
        {
            lock (this)
            {
                _data.Remove(resource);
            }
        }
            
        public void UnlockAll()
        {
            lock (this)
            {
                _data.Clear();
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
    }
}