using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RedlockDotNet;

namespace TestUtils
{
    public class ExceptionRedlockInstance : IRedlockInstance
    {
        public readonly Exception TryLockException = new Exception("lock"); 
        public readonly Exception TryLockAsyncException = new Exception("lock async"); 
        public readonly Exception UnlockException = new Exception("unlock"); 
        public readonly Exception UnlockAsyncException = new Exception("unlock async");
        public readonly Exception TryExtendException = new Exception("extend"); 
        public readonly Exception TryExtendAsyncException = new Exception("extend async");
        public readonly Exception GetInfoException = new Exception("get info"); 
        public readonly Exception GetInfoAsyncException = new Exception("get info async");
        public ExceptionRedlockInstance(string name, MinValidityDelegate minValidity)
        {
            Name = name;
            _minValidity = minValidity;
        }

        public string Name { get; }
        private readonly MinValidityDelegate _minValidity;

        public TimeSpan MinValidity(TimeSpan lockTimeToLive, TimeSpan lockingDuration)  => _minValidity(lockTimeToLive, lockingDuration);
        public bool TryLock(string resource, string nonce, TimeSpan lockTimeToLive, IReadOnlyDictionary<string, string>? metadata = null)
            => throw TryLockException;
        public Task<bool> TryLockAsync(string resource, string nonce, TimeSpan lockTimeToLive, IReadOnlyDictionary<string, string>? metadata = null)
            => throw TryLockAsyncException;
        public void Unlock(string resource, string nonce) => throw UnlockException;
        public Task UnlockAsync(string resource, string nonce) => throw UnlockAsyncException;
        public ExtendResult TryExtend(string resource, string nonce, TimeSpan lockTimeToLive) => throw TryExtendException;
        public Task<ExtendResult> TryExtendAsync(string resource, string nonce, TimeSpan lockTimeToLive) => throw TryExtendAsyncException;
        public InstanceLockInfo? GetInfo(string resource) => throw GetInfoException;
        public Task<InstanceLockInfo?> GetInfoAsync(string resource) => throw GetInfoAsyncException;

        public override string ToString() => $"ex:{Name}";
        
        public static ExceptionRedlockInstance Create(string name) => Create(name, (ttl, duration) => ttl - duration);
        
        public static ExceptionRedlockInstance Create(string name, MinValidityDelegate minValidity) 
            => new ExceptionRedlockInstance(name, minValidity);

    }
}