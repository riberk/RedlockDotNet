using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RedlockDotNet.Internal;
using RedlockDotNet.Repeaters;

namespace RedlockDotNet
{
    /// <summary>
    /// Implements Redlock (https://redis.io/topics/distlock) alg for custom lock providers
    /// </summary>
    public readonly struct Redlock : IDisposable, IAsyncDisposable
    {
        /// <summary>Locked resource</summary>
        public string Resource { get; }
        
        /// <summary>Resource locked with nonce</summary>
        public string Nonce { get; }
        
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
            Resource = resource;
            Nonce = nonce;
            _implementation = implementation;
            _logger = logger;
        }

        /// <summary>
        /// Try acquire distributed lock on all <see cref="IRedlockInstance"/>
        /// </summary>
        /// <param name="resource">Resource name for lock</param>
        /// <param name="nonce">String to identify lock owners</param>
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
            logger.Locking(resource, nonce, lockTimeToLive);

            var lockResult = implementation.Instances.TryLockAll(logger, resource, nonce, lockTimeToLive);

            if (lockResult.IsLocked(lockTimeToLive, implementation))
            {
                logger.Locked(resource, nonce, lockResult);
                return new Redlock(resource, nonce, implementation, logger);
            }

            logger.UnlockingOnFail(resource, nonce, lockResult);
            implementation.Instances.UnlockAll(logger, resource, nonce);
            logger.UnlockedOnFail(resource, nonce);
            return null;
        }
        
        

        /// <summary>
        /// Try acquire distributed lock on all <see cref="IRedlockInstance"/>
        /// </summary>
        /// <param name="resource">Resource name for lock</param>
        /// <param name="nonce">String to identify lock owners</param>
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
            logger.Locking(resource, nonce, lockTimeToLive);
            var lockResult = await implementation.Instances.TryLockAllAsync(logger, resource, nonce, lockTimeToLive)
                .ConfigureAwait(false);

            if (lockResult.IsLocked(lockTimeToLive, implementation))
            {
                logger.Locked(resource, nonce, lockResult);
                return new Redlock(resource, nonce, implementation, logger);
            }

            logger.UnlockingOnFail(resource, nonce, lockResult);
            await implementation.Instances.UnlockAllAsync(logger, resource, nonce).ConfigureAwait(false);
            logger.UnlockedOnFail(resource, nonce);
            return null;
        }


        /// <summary>
        /// Acquire distributed lock on all <see cref="IRedlockInstance"/> in repeater loop
        /// </summary>
        /// <param name="resource">Resource name for lock</param>
        /// <param name="nonce">String to identify lock owners</param>
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
            int maxWaitMs
        ) where T: IRedlockRepeater
        {
            var (redlock, attemptCount) = TryLockInternal(
                resource, nonce, lockTimeToLive, implementation, logger, repeater, maxWaitMs
            );
            
            if (redlock != null)
            {
                return redlock.Value;
            }

            throw repeater.CreateException(resource, nonce, attemptCount);
        }


        /// <summary>
        /// Try acquire distributed lock on all <see cref="IRedlockInstance"/> in repeater loop
        /// </summary>
        /// <param name="resource">Resource name for lock</param>
        /// <param name="nonce">String to identify lock owners</param>
        /// <param name="lockTimeToLive">
        /// Time to live of acquired lock.
        /// Attention! If this ttl are expired, code that the lock uses has a safety violation
        /// </param>
        /// <param name="implementation">Options for acquire lock</param>
        /// <param name="logger"></param>
        /// <param name="repeater"></param>
        /// <param name="maxWaitMs">Max wait time before next attempt after previous failed</param>
        /// <typeparam name="T">Type of repeater</typeparam>
        /// <returns>lock or null if unable to acquire</returns>
        public static Redlock? TryLock<T>(
            string resource,
            string nonce,
            TimeSpan lockTimeToLive,
            IRedlockImplementation implementation,
            ILogger logger,
            in T repeater,
            int maxWaitMs
        ) where T : IRedlockRepeater 
            => TryLockInternal(resource, nonce, lockTimeToLive, implementation, logger, repeater, maxWaitMs).redlock;
        
        private static (Redlock? redlock, int attemptCount) TryLockInternal<T>(
            string resource,
            string nonce,
            TimeSpan lockTimeToLive,
            IRedlockImplementation implementation,
            ILogger logger,
            in T repeater,
            int maxWaitMs
        ) where T: IRedlockRepeater
        {
            var attemptCount = 0;
            while (true)
            {
                var @lock = TryLock(resource, nonce, lockTimeToLive, implementation, logger);
                if (@lock != null)
                {
                    return (@lock.Value, attemptCount);
                }

                attemptCount++;
                if (repeater.Next())
                {
                    repeater.WaitRandom(maxWaitMs);
                }
                else
                {
                    return (null, attemptCount);
                }
            }
        }
        
        
        /// <summary>
        /// Acquire distributed lock on all <see cref="IRedlockInstance"/> in repeater loop
        /// </summary>
        /// <param name="resource">Resource name for lock</param>
        /// <param name="nonce">String to identify lock owners</param>
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
            int maxWaitMs
        ) where T: IRedlockRepeater
        {
            var (redlock, attemptCount) = await TryLockInternalAsync(resource, nonce, lockTimeToLive, implementation, logger, repeater, maxWaitMs);
        
            if (redlock != null)
            {
                return redlock.Value;
            }

            throw repeater.CreateException(resource, nonce, attemptCount);
        }
        
        /// <summary>
        /// Try acquire distributed lock on all <see cref="IRedlockInstance"/> in repeater loop
        /// </summary>
        /// <param name="resource">Resource name for lock</param>
        /// <param name="nonce">String to identify lock owners</param>
        /// <param name="lockTimeToLive">
        /// Time to live of acquired lock.
        /// Attention! If this ttl are expired, code that the lock uses has a safety violation
        /// </param>
        /// <param name="implementation">Options for acquire lock</param>
        /// <param name="logger"></param>
        /// <param name="repeater"></param>
        /// <param name="maxWaitMs">Max wait time before next attempt after previous failed</param>
        /// <typeparam name="T">Type of repeater</typeparam>
        /// <returns>lock or null if unable to acquire</returns>
        public static async Task<Redlock?> TryLockAsync<T>(
            string resource,
            string nonce,
            TimeSpan lockTimeToLive,
            IRedlockImplementation implementation,
            ILogger logger,
            T repeater,
            int maxWaitMs
        ) where T: IRedlockRepeater 
            => (await TryLockInternalAsync(resource, nonce, lockTimeToLive, implementation, logger, repeater, maxWaitMs)).redlock;

        private static async Task<(Redlock? redlock, int attemptCount)> TryLockInternalAsync<T>(
            string resource,
            string nonce,
            TimeSpan lockTimeToLive,
            IRedlockImplementation implementation,
            ILogger logger,
            T repeater,
            int maxWaitMs
        ) where T: IRedlockRepeater
        {
            var attemptCount = 0;
            while (true)
            {
                var @lock = await TryLockAsync(resource, nonce, lockTimeToLive, implementation, logger);
                if (@lock != null)
                {
                    return (@lock.Value, attemptCount);
                }
                
                attemptCount++;
                if (repeater.Next())
                {
                    await repeater.WaitRandomAsync(maxWaitMs);
                }
                else
                {
                    return (null, attemptCount);
                }
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _implementation?.Instances.UnlockAll(_logger, Resource, Nonce);
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (_implementation == null)
            {
                return;
            }
            await _implementation.Instances.UnlockAllAsync(_logger, Resource, Nonce).ConfigureAwait(false);
        }
    }
}
