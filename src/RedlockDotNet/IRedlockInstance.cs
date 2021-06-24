using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RedlockDotNet
{
    /// <summary>
    /// Instance for distributed lock (redis or other server who can store props)
    /// </summary>
    public interface IRedlockInstance
    {
        /// <summary>
        /// Calculate min validity time of lock
        /// </summary>
        /// <param name="lockTimeToLive">Ttl of lock on the server</param>
        /// <param name="lockingDuration">Elapsed time from start of locking on first server to end on last server</param>
        /// <returns>Minimum validity time of acquired lock</returns>
        TimeSpan MinValidity(TimeSpan lockTimeToLive, TimeSpan lockingDuration);

        /// <summary>
        /// Try acquire lock resource on server
        /// </summary>
        /// <param name="resource">Resource to lock</param>
        /// <param name="nonce">Value to differentiate lock owners of resource on server</param>
        /// <param name="lockTimeToLive">
        /// Time to live of acquired lock.
        /// Attention! If this ttl are expired, code that the lock uses has a safety violation
        /// </param>
        /// <param name="metadata">Metadata for lock</param>
        /// <returns>true if lock acquired on the current instance, otherwise false</returns>
        bool TryLock(string resource, string nonce, TimeSpan lockTimeToLive, IReadOnlyDictionary<string, string>? metadata = null);

        /// <summary>
        /// Try acquire lock resource on server
        /// </summary>
        /// <param name="resource">Resource to lock</param>
        /// <param name="nonce">Value to differentiate lock owners of resource on server</param>
        /// <param name="lockTimeToLive">
        /// Time to live of acquired lock.
        /// Attention! If this ttl are expired, code that the lock uses has a safety violation
        /// </param>
        /// <param name="metadata">Metadata for lock</param>
        /// <returns>true if lock acquired on the current instance, otherwise false</returns>
        Task<bool> TryLockAsync(string resource, string nonce, TimeSpan lockTimeToLive, IReadOnlyDictionary<string, string>? metadata = null);

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

        /// <summary>
        /// Try extend lock resource on server
        /// </summary>
        /// <param name="resource">Resource to lock</param>
        /// <param name="nonce">Value to differentiate lock owners of resource on server</param>
        /// <param name="lockTimeToLive">
        /// Time to live of acquired lock.
        /// Attention! If this ttl are expired, code that the lock uses has a safety violation
        /// </param>
        /// <returns>Result of extend operation</returns>
        ExtendResult TryExtend(string resource, string nonce, TimeSpan lockTimeToLive);
        
        /// <summary>
        /// Try extend lock resource on server
        /// </summary>
        /// <param name="resource">Resource to lock</param>
        /// <param name="nonce">Value to differentiate lock owners of resource on server</param>
        /// <param name="lockTimeToLive">
        /// Time to live of acquired lock.
        /// Attention! If this ttl are expired, code that the lock uses has a safety violation
        /// </param>
        /// <returns>Result of extend operation</returns>
        Task<ExtendResult> TryExtendAsync(string resource, string nonce, TimeSpan lockTimeToLive);

        /// <summary>Resolve lock info or null if lock not acquired</summary>
        InstanceLockInfo? GetInfo(string resource);

        /// <summary>Resolve lock info or null if lock not acquired</summary>
        Task<InstanceLockInfo?> GetInfoAsync(string resource);
    }
}