using System;
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
        public bool TryLock(string resource, string nonce, TimeSpan lockTimeToLive) => throw TryLockException;
        public Task<bool> TryLockAsync(string resource, string nonce, TimeSpan lockTimeToLive) => throw TryLockAsyncException;
        public void Unlock(string resource, string nonce) => throw UnlockException;
        public Task UnlockAsync(string resource, string nonce) => throw UnlockAsyncException;
        public ExtendResult TryExtend(string resource, string nonce, TimeSpan lockTimeToLive, bool tryReacquire) => throw TryExtendException;
        public Task<ExtendResult> TryExtendAsync(string resource, string nonce, TimeSpan lockTimeToLive, bool tryReacquire) => throw TryExtendAsyncException;
    }
}