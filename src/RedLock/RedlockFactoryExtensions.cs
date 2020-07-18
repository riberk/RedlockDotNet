using System;
using System.Threading;
using System.Threading.Tasks;
using RedLock.Repeaters;

namespace RedLock
{
    /// <summary>Extensions on <see cref="IRedlockFactory"/></summary>
    public static class RedlockFactoryExtensions
    {
        /// <summary>
        /// Acquire distributed lock with random nonce and default max wait between attempts
        /// </summary>
        /// <param name="f"></param>
        /// <param name="resource">Resource name for lock</param>
        /// <param name="lockTimeToLive">
        /// Time to live of acquired lock.
        /// Attention! If this ttl are expired, code that the lock uses has a safety violation
        /// </param>
        /// <param name="repeater"></param>
        /// <typeparam name="T">Type of repeater</typeparam>
        /// <returns></returns>
        public static Redlock Create<T>(this IRedlockFactory f, string resource, TimeSpan lockTimeToLive, T repeater) 
            where T : IRedlockRepeater => f.Create(resource, lockTimeToLive, repeater, f.DefaultMaxWaitMsBetweenReplays(resource, lockTimeToLive));

        /// <summary>
        /// Acquire distributed lock with random nonce in repeater loop
        /// </summary>
        /// <param name="f"></param>
        /// <param name="resource">Resource name for lock</param>
        /// <param name="lockTimeToLive">
        /// Time to live of acquired lock.
        /// Attention! If this ttl are expired, code that the lock uses has a safety violation
        /// </param>
        /// <param name="maxRetryCount">Max retries if lock unable to acquire</param>
        /// <returns></returns>
        public static Redlock Create(this IRedlockFactory f, string resource, TimeSpan lockTimeToLive, int maxRetryCount) 
            => f.Create(resource, lockTimeToLive, new MaxRetriesRedlockRepeater(maxRetryCount));
        
        /// <summary>
        /// Acquire distributed lock with random nonce in repeater loop
        /// </summary>
        /// <param name="f"></param>
        /// <param name="resource">Resource name for lock</param>
        /// <param name="lockTimeToLive">
        /// Time to live of acquired lock.
        /// Attention! If this ttl are expired, code that the lock uses has a safety violation
        /// </param>
        /// <returns></returns>
        public static Redlock Create(this IRedlockFactory f, string resource, TimeSpan lockTimeToLive) 
            => f.Create(resource, lockTimeToLive, 3);

        /// <summary>
        /// Acquire distributed lock with random nonce in repeater loop
        /// </summary>
        /// <param name="f"></param>
        /// <param name="resource">Resource name for lock</param>
        /// <returns></returns>
        public static Redlock Create(this IRedlockFactory f, string resource) 
            => f.Create(resource, f.DefaultTtl(resource));

        /// <summary>
        /// Acquire distributed lock with random nonce in repeater loop
        /// </summary>
        /// <param name="f"></param>
        /// <param name="resource">Resource name for lock</param>
        /// <param name="lockTimeToLive">
        /// Time to live of acquired lock.
        /// Attention! If this ttl are expired, code that the lock uses has a safety violation
        /// </param>
        /// <param name="cancellationToken">Token to cancel repeater loop</param>
        /// <returns></returns>
        public static Redlock Create(
            this IRedlockFactory f, string resource, TimeSpan lockTimeToLive, CancellationToken cancellationToken
        ) => f.Create(resource, lockTimeToLive, new CancellationRedlockRepeater(cancellationToken));

        /// <summary>
        /// Acquire distributed lock with random nonce in repeater loop
        /// </summary>
        /// <param name="f"></param>
        /// <param name="resource">Resource name for lock</param>
        /// <param name="cancellationToken">Token to cancel repeater loop</param>
        /// <returns></returns>
        public static Redlock Create(this IRedlockFactory f, string resource, CancellationToken cancellationToken) 
            => f.Create(resource, f.DefaultTtl(resource), cancellationToken);
        
        /// <summary>
        /// Acquire distributed lock with random nonce and default max wait between attempts
        /// </summary>
        /// <param name="f"></param>
        /// <param name="resource">Resource name for lock</param>
        /// <param name="lockTimeToLive">
        /// Time to live of acquired lock.
        /// Attention! If this ttl are expired, code that the lock uses has a safety violation
        /// </param>
        /// <param name="repeater"></param>
        /// <typeparam name="T">Type of repeater</typeparam>
        /// <returns></returns>
        public static Task<Redlock> CreateAsync<T>(this IRedlockFactory f, string resource, TimeSpan lockTimeToLive, T repeater) 
            where T : IRedlockRepeater => f.CreateAsync(resource, lockTimeToLive, repeater, f.DefaultMaxWaitMsBetweenReplays(resource, lockTimeToLive));

        /// <summary>
        /// Acquire distributed lock with random nonce in repeater loop
        /// </summary>
        /// <param name="f"></param>
        /// <param name="resource">Resource name for lock</param>
        /// <param name="lockTimeToLive">
        /// Time to live of acquired lock.
        /// Attention! If this ttl are expired, code that the lock uses has a safety violation
        /// </param>
        /// <param name="maxRetryCount">Max retries if lock unable to acquire</param>
        /// <returns></returns>
        public static Task<Redlock> CreateAsync(this IRedlockFactory f, string resource, TimeSpan lockTimeToLive, int maxRetryCount) 
            => f.CreateAsync(resource, lockTimeToLive, new MaxRetriesRedlockRepeater(maxRetryCount));
        
        /// <summary>
        /// Acquire distributed lock with random nonce in repeater loop
        /// </summary>
        /// <param name="f"></param>
        /// <param name="resource">Resource name for lock</param>
        /// <param name="lockTimeToLive">
        /// Time to live of acquired lock.
        /// Attention! If this ttl are expired, code that the lock uses has a safety violation
        /// </param>
        /// <returns></returns>
        public static Task<Redlock> CreateAsync(this IRedlockFactory f, string resource, TimeSpan lockTimeToLive) 
            => f.CreateAsync(resource, lockTimeToLive, 3);

        /// <summary>
        /// Acquire distributed lock with random nonce in repeater loop
        /// </summary>
        /// <param name="f"></param>
        /// <param name="resource">Resource name for lock</param>
        /// <returns></returns>
        public static Task<Redlock> CreateAsync(this IRedlockFactory f, string resource) 
            => f.CreateAsync(resource, f.DefaultTtl(resource));

        /// <summary>
        /// Acquire distributed lock with random nonce in repeater loop
        /// </summary>
        /// <param name="f"></param>
        /// <param name="resource">Resource name for lock</param>
        /// <param name="lockTimeToLive">
        /// Time to live of acquired lock.
        /// Attention! If this ttl are expired, code that the lock uses has a safety violation
        /// </param>
        /// <param name="cancellationToken">Token to cancel repeater loop</param>
        /// <returns></returns>
        public static Task<Redlock> CreateAsync(
            this IRedlockFactory f, string resource, TimeSpan lockTimeToLive, CancellationToken cancellationToken
        ) => f.CreateAsync(resource, lockTimeToLive, new CancellationRedlockRepeater(cancellationToken));

        /// <summary>
        /// Acquire distributed lock with random nonce in repeater loop
        /// </summary>
        /// <param name="f"></param>
        /// <param name="resource">Resource name for lock</param>
        /// <param name="cancellationToken">Token to cancel repeater loop</param>
        /// <returns></returns>
        public static Task<Redlock> CreateAsync(this IRedlockFactory f, string resource, CancellationToken cancellationToken) 
            => f.CreateAsync(resource, f.DefaultTtl(resource), cancellationToken);
    }
}