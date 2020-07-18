using System;
using System.Threading.Tasks;
using RedLock;

namespace TestUtils
{
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
}