using System;
using System.Threading.Tasks;
using RedLock.Repeaters;

namespace RedLock
{
    /// <summary><see cref="Redlock"/> factory</summary>
    public interface IRedlockFactory
    {
        /// <summary>
        /// Try acquire distributed lock with random nonce
        /// </summary>
        /// <param name="resource">Resource name for lock</param>
        /// <param name="lockTimeToLive">
        /// Time to live of acquired lock.
        /// Attention! If this ttl are expired, code that the lock uses has a safety violation
        /// </param>
        /// <returns>Lock object or null if it failed</returns>
        Redlock? TryCreate(string resource, TimeSpan lockTimeToLive);

        /// <summary>
        /// Try acquire distributed lock with random nonce in repeater loop
        /// </summary>
        /// <param name="resource">Resource name for lock</param>
        /// <param name="lockTimeToLive">
        /// Time to live of acquired lock.
        /// Attention! If this ttl are expired, code that the lock uses has a safety violation
        /// </param>
        /// <param name="repeater"></param>
        /// <param name="maxWaitMs">Max wait time before next attempt after previous failed</param>
        /// <typeparam name="T">Type of repeater</typeparam>
        /// <returns></returns>
        Redlock? TryCreate<T>(string resource, TimeSpan lockTimeToLive, T repeater, int maxWaitMs = 200)
            where T : IRedlockRepeater;

        /// <summary>
        /// Acquire distributed lock with random nonce in repeater loop
        /// </summary>
        /// <param name="resource">Resource name for lock</param>
        /// <param name="lockTimeToLive">
        /// Time to live of acquired lock.
        /// Attention! If this ttl are expired, code that the lock uses has a safety violation
        /// </param>
        /// <param name="repeater"></param>
        /// <param name="maxWaitMs">Max wait time before next attempt after previous failed</param>
        /// <typeparam name="T">Type of repeater</typeparam>
        /// <returns></returns>
        Redlock Create<T>(string resource, TimeSpan lockTimeToLive, T repeater, int maxWaitMs)
            where T : IRedlockRepeater;
        
        /// <summary>
        /// Try acquire distributed lock with random nonce
        /// </summary>
        /// <param name="resource">Resource name for lock</param>
        /// <param name="lockTimeToLive">
        /// Time to live of acquired lock.
        /// Attention! If this ttl are expired, code that the lock uses has a safety violation
        /// </param>
        /// <returns>Lock object or null if it failed</returns>
        Task<Redlock?> TryCreateAsync(string resource, TimeSpan lockTimeToLive);
        
        /// <summary>
        /// Try acquire distributed lock with random nonce in repeater loop
        /// </summary>
        /// <param name="resource">Resource name for lock</param>
        /// <param name="lockTimeToLive">
        /// Time to live of acquired lock.
        /// Attention! If this ttl are expired, code that the lock uses has a safety violation
        /// </param>
        /// <param name="repeater"></param>
        /// <param name="maxWaitMs">Max wait time before next attempt after previous failed</param>
        /// <typeparam name="T">Type of repeater</typeparam>
        /// <returns></returns>
        Task<Redlock?> TryCreateAsync<T>(string resource, TimeSpan lockTimeToLive, T repeater, int maxWaitMs)
            where T : IRedlockRepeater;

        /// <summary>
        /// Acquire distributed lock with random nonce in repeater loop
        /// </summary>
        /// <param name="resource">Resource name for lock</param>
        /// <param name="lockTimeToLive">
        /// Time to live of acquired lock.
        /// Attention! If this ttl are expired, code that the lock uses has a safety violation
        /// </param>
        /// <param name="repeater"></param>
        /// <param name="maxWaitMs">Max wait time before next attempt after previous failed</param>
        /// <typeparam name="T">Type of repeater</typeparam>
        /// <returns></returns>
        Task<Redlock> CreateAsync<T>(string resource, TimeSpan lockTimeToLive, T repeater, int maxWaitMs)
            where T : IRedlockRepeater;
        
        /// <summary>
        /// Gets default ttl for locking resource
        /// </summary>
        /// <param name="resource">Locking resource</param>
        /// <returns>Resource time to live</returns>
        TimeSpan DefaultTtl(string resource);

        /// <summary>
        /// Gets default max wait between replays for locking resource
        /// </summary>
        /// <param name="resource">Locking resource</param>
        /// <param name="lockTimeToLive">Locking resource ttl</param>
        /// <returns>Max wait between replays when we need to repeat try acquire lock</returns>
        int DefaultMaxWaitMsBetweenReplays(string resource, TimeSpan lockTimeToLive);
    }
}