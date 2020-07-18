using System;
using System.Threading.Tasks;

namespace RedlockDotNet
{
    /// <summary>
    /// Instance for distributed lock (redis or other server who can store props)
    /// </summary>
    public interface IRedlockInstance
    {
        /// <summary>
        /// Try acquire lock resource on server
        /// </summary>
        /// <param name="resource">Resource to lock</param>
        /// <param name="nonce">Value to differentiate lock owners of resource on server</param>
        /// <param name="lockTimeToLive">
        /// Time to live of acquired lock.
        /// Attention! If this ttl are expired, code that the lock uses has a safety violation
        /// </param>
        /// <returns>true if lock acquired on the current instance, otherwise false</returns>
        bool TryLock(string resource, string nonce, TimeSpan lockTimeToLive);

        /// <summary>
        /// Try acquire lock resource on server
        /// </summary>
        /// <param name="resource">Resource to lock</param>
        /// <param name="nonce">Value to differentiate lock owners of resource on server</param>
        /// <param name="lockTimeToLive">
        /// Time to live of acquired lock.
        /// Attention! If this ttl are expired, code that the lock uses has a safety violation
        /// </param>
        /// <returns>true if lock acquired on the current instance, otherwise false</returns>
        Task<bool> TryLockAsync(string resource, string nonce, TimeSpan lockTimeToLive);

        /// <summary>
        /// Unlock resource on server
        /// </summary>
        /// <param name="resource">Resource to lock</param>
        /// <param name="nonce">Value to differentiate lock owners of resource on server</param>
        void Unlock(string resource, string nonce);

        /// <summary>
        /// Unlock resource on server
        /// </summary>
        /// <param name="resource">Resource to lock</param>
        /// <param name="nonce">Value to differentiate lock owners of resource on server</param>
        Task UnlockAsync(string resource, string nonce);
    }
}