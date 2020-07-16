using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RedLock.Internal;

namespace RedLock
{
    /// <summary>
    /// Implements Redlock (https://redis.io/topics/distlock) alg for custom lock providers
    /// </summary>
    public readonly struct Redlock : IDisposable, IAsyncDisposable
    {
        private readonly string _resource;
        private readonly string _nonce;
        private readonly IRedlockImplementation _implementation;
        private readonly ILogger _logger;

        /// <summary>
        /// Implements Redlock (https://redis.io/topics/distlock) alg for custom lock providers
        /// </summary>
        public Redlock(
            string resource,
            string nonce,
            IRedlockImplementation implementation,
            ILogger logger
        )
        {
            _resource = resource;
            _nonce = nonce;
            _implementation = implementation;
            _logger = logger;
        }

        /// <summary>
        /// Try acquire distributed lock on all <see cref="IRedlockInstance"/>
        /// </summary>
        /// <param name="resource">Resource name for lock</param>
        /// <param name="nonce">Random value</param>
        /// <param name="lockTimeToLive">
        /// Time to live of acquired lock.
        /// Attention! If this ttl are expired, code that the lock uses has a safety violation
        /// </param>
        /// <param name="implementation">Options for acquire lock</param>
        /// <param name="logger"></param>
        /// <returns>Lock object or null if it failed</returns>
        public static Redlock? TryLock(
            string resource,
            string nonce,
            TimeSpan lockTimeToLive,
            IRedlockImplementation implementation,
            ILogger logger
        )
        {
            var lockResult = implementation.Instances.TryLockAll(logger, resource, nonce, lockTimeToLive);

            if (lockResult.IsLocked(lockTimeToLive, implementation))
            {
                return new Redlock(resource, nonce, implementation, logger);
            }

            implementation.Instances.UnlockAll(logger, resource, nonce);
            return null;
        }

        /// <summary>
        /// Try acquire distributed lock on all <see cref="IRedlockInstance"/>
        /// </summary>
        /// <param name="resource">Resource name for lock</param>
        /// <param name="nonce">Random value</param>
        /// <param name="lockTimeToLive">
        /// Time to live of acquired lock.
        /// Attention! If this ttl are expired, code that the lock uses has a safety violation
        /// </param>
        /// <param name="implementation">Options for acquire lock</param>
        /// <param name="logger"></param>
        /// <returns>Lock object or null if it failed</returns>
        public static async Task<Redlock?> TryLockAsync(
            string resource,
            string nonce,
            TimeSpan lockTimeToLive,
            IRedlockImplementation implementation,
            ILogger logger
        )
        {
            var lockResult = await implementation.Instances.TryLockAllAsync(logger, resource, nonce, lockTimeToLive)
                .ConfigureAwait(false);

            if (lockResult.IsLocked(lockTimeToLive, implementation))
            {
                return new Redlock(resource, nonce, implementation, logger);
            }

            await implementation.Instances.UnlockAllAsync(logger, resource, nonce).ConfigureAwait(false);
            return null;
        }


        /// <summary>
        /// Acquire distributed lock on all <see cref="IRedlockInstance"/> in repeater loop
        /// </summary>
        /// <param name="resource">Resource name for lock</param>
        /// <param name="nonce">Random value</param>
        /// <param name="lockTimeToLive">
        /// Time to live of acquired lock.
        /// Attention! If this ttl are expired, code that the lock uses has a safety violation
        /// </param>
        /// <param name="implementation">Options for acquire lock</param>
        /// <param name="logger"></param>
        /// <param name="repeater"></param>
        /// <param name="maxWaitMs">Max wait time before next attempt after previous failed</param>
        /// <typeparam name="T">Type of repeater</typeparam>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">If unable to acquire a lock by repeater loop</exception>
        public static Redlock Lock<T>(
            string resource,
            string nonce,
            TimeSpan lockTimeToLive,
            IRedlockImplementation implementation,
            ILogger logger,
            in T repeater,
            int maxWaitMs = 200
        ) where T: IRedlockRepeater
        {
            var attemptCount = 0;
            while (true)
            {
                var @lock = TryLock(resource, nonce, lockTimeToLive, implementation, logger);
                if (@lock != null)
                {
                    return @lock.Value;
                }

                attemptCount++;
                if (repeater.Next())
                {
                    repeater.WaitRandom(maxWaitMs);
                }
                else
                {
                    throw new RedlockException($"Unable to obtain lock to ['{resource}'] = '{nonce}' on {attemptCount} attempts");
                }
            }
        }
        
        
        /// <summary>
        /// Acquire distributed lock on all <see cref="IRedlockInstance"/> in repeater loop
        /// </summary>
        /// <param name="resource">Resource name for lock</param>
        /// <param name="nonce">Random value</param>
        /// <param name="lockTimeToLive">
        /// Time to live of acquired lock.
        /// Attention! If this ttl are expired, code that the lock uses has a safety violation
        /// </param>
        /// <param name="implementation">Options for acquire lock</param>
        /// <param name="logger"></param>
        /// <param name="repeater"></param>
        /// <param name="maxWaitMs">Max wait time before next attempt after previous failed</param>
        /// <typeparam name="T">Type of repeater</typeparam>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">If unable to acquire a lock by repeater loop</exception>
        public static async Task<Redlock> LockAsync<T>(string resource,
            string nonce,
            TimeSpan lockTimeToLive,
            IRedlockImplementation implementation,
            ILogger logger,
            T repeater,
            int maxWaitMs = 200
        ) where T: IRedlockRepeater
        {
            var attemptCount = 0;
            while (true)
            {
                var @lock = await TryLockAsync(resource, nonce, lockTimeToLive, implementation, logger);
                if (@lock != null)
                {
                    return @lock.Value;
                }
                
                attemptCount++;
                if (repeater.Next())
                {
                    await repeater.WaitRandomAsync(maxWaitMs);
                }
                else
                {
                    throw new RedlockException($"Unable to obtain lock to ['{resource}'] = '{nonce}' on {attemptCount} attempts");
                }
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _implementation.Instances.UnlockAll(_logger, _resource, _nonce);
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            await _implementation.Instances.UnlockAllAsync(_logger, _resource, _nonce).ConfigureAwait(false);
        }
    }
}
