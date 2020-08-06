using System;
using System.Diagnostics;
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

        /// <summary>Resource time to live</summary>
        public TimeSpan Ttl { get; }

        /// <summary>
        /// <see cref="Stopwatch.GetTimestamp"/> after which the lock expires
        /// </summary>
        public DateTime ValidUntilUtc { get; }

        private readonly IRedlockImplementation _implementation;
        private readonly ILogger _logger;

        /// <summary>
        /// Implements Redlock (https://redis.io/topics/distlock) alg for custom lock providers
        /// </summary>
        public Redlock(
            string resource,
            string nonce,
            TimeSpan ttl,
            DateTime validUntilUtc,
            IRedlockImplementation implementation,
            ILogger logger
        )
        {
            Resource = resource;
            Nonce = nonce;
            Ttl = ttl;
            ValidUntilUtc = validUntilUtc;
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
        /// <param name="utcNow"></param>
        /// <returns>Lock object or null if it failed</returns>
        public static Redlock? TryLock(
            string resource,
            string nonce,
            TimeSpan lockTimeToLive,
            IRedlockImplementation implementation,
            ILogger logger,
            Func<DateTime>? utcNow = null
        )
        {
            logger.Locking(resource, nonce, lockTimeToLive);

            var lockResult = implementation.Instances.TryLockAll(logger, resource, nonce, lockTimeToLive);

            if (lockResult.IsLocked(lockTimeToLive, implementation, utcNow, out var validUntil))
            {
                logger.Locked(resource, nonce, lockResult);
                return new Redlock(resource, nonce, lockTimeToLive, validUntil, implementation, logger);
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
        /// <param name="utcNow"></param>
        /// <returns>Lock object or null if it failed</returns>
        public static async Task<Redlock?> TryLockAsync(
            string resource,
            string nonce,
            TimeSpan lockTimeToLive,
            IRedlockImplementation implementation,
            ILogger logger,
            Func<DateTime>? utcNow = null
        )
        {
            logger.Locking(resource, nonce, lockTimeToLive);
            var lockResult = await implementation.Instances.TryLockAllAsync(logger, resource, nonce, lockTimeToLive)
                .ConfigureAwait(false);

            if (lockResult.IsLocked(lockTimeToLive, implementation, utcNow, out var validUntil))
            {
                logger.Locked(resource, nonce, lockResult);
                return new Redlock(resource, nonce, lockTimeToLive, validUntil, implementation, logger);
            }

            logger.UnlockingOnFail(resource, nonce, lockResult);
            await implementation.Instances.UnlockAllAsync(logger, resource, nonce).ConfigureAwait(false);
            logger.UnlockedOnFail(resource, nonce);
            return null;
        }
        
        /// <summary>
        /// Try extend current lock
        /// </summary>
        /// <param name="tryReacquire">If true we try to reacquire lock when it is lost</param>
        /// <param name="utcNow"></param>
        /// <returns>New ValidUntil or null if extension failed</returns>
        public DateTime? TryExtend(
            bool tryReacquire,
            Func<DateTime>? utcNow = null
        )
        {
            _logger.Extending(Resource, Nonce, Ttl, tryReacquire);
            var lockResult = _implementation.Instances.TryExtendAll(_logger, Resource, Nonce, Ttl, tryReacquire);
            if (lockResult.IsLocked(Ttl, _implementation, utcNow, out var validUntil))
            {
                _logger.Extended(Resource, Nonce, Ttl, validUntil);
                return validUntil;
            }

            _logger.ExtendFail(Resource, Nonce, Ttl, tryReacquire);
            return null;
        }

        /// <summary>
        /// Try extend current lock
        /// </summary>
        /// <param name="tryReacquire">If true we try to reacquire lock when it is lost</param>
        /// <param name="utcNow"></param>
        /// <returns>New ValidUntil or null if extension failed</returns>
        public async Task<DateTime?> TryExtendAsync(
            bool tryReacquire,
            Func<DateTime>? utcNow = null
        )
        {
            _logger.Extending(Resource, Nonce, Ttl, tryReacquire);
            var lockResult = await _implementation.Instances.TryExtendAllAsync(_logger, Resource, Nonce, Ttl, tryReacquire)
                .ConfigureAwait(false);

            if (lockResult.IsLocked(Ttl, _implementation, utcNow, out var validUntil))
            {
                _logger.Extended(Resource, Nonce, Ttl, validUntil);
                return validUntil;
            }

            _logger.ExtendFail(Resource, Nonce, Ttl, tryReacquire);
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
        /// <param name="utcNow"></param>
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
            int maxWaitMs,
            Func<DateTime>? utcNow = null
        ) where T: IRedlockRepeater
        {
            var (redlock, attemptCount) = TryLockInternal(
                resource, nonce, lockTimeToLive, implementation, logger, repeater, maxWaitMs, utcNow
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
        /// <param name="utcNow"></param>
        /// <typeparam name="T">Type of repeater</typeparam>
        /// <returns>lock or null if unable to acquire</returns>
        public static Redlock? TryLock<T>(
            string resource,
            string nonce,
            TimeSpan lockTimeToLive,
            IRedlockImplementation implementation,
            ILogger logger,
            in T repeater,
            int maxWaitMs,
            Func<DateTime>? utcNow = null
        ) where T : IRedlockRepeater 
            => TryLockInternal(resource, nonce, lockTimeToLive, implementation, logger, repeater, maxWaitMs, utcNow).redlock;
        
        private static (Redlock? redlock, int attemptCount) TryLockInternal<T>(
            string resource,
            string nonce,
            TimeSpan lockTimeToLive,
            IRedlockImplementation implementation,
            ILogger logger,
            in T repeater,
            int maxWaitMs,
            Func<DateTime>? utcNow = null
        ) where T: IRedlockRepeater
        {
            var attemptCount = 0;
            while (true)
            {
                var @lock = TryLock(resource, nonce, lockTimeToLive, implementation, logger, utcNow);
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
        /// Extend current lock in repeater loop
        /// </summary>
        /// <param name="tryReacquire">If true we try to reacquire lock when it is lost</param>
        /// <param name="repeater"></param>
        /// <param name="maxWaitMs">Max wait time before next attempt after previous failed</param>
        /// <param name="utcNow"></param>
        /// <typeparam name="T">Type of repeater</typeparam>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">If unable to extend a lock by repeater loop</exception>
        public DateTime Extend<T>(
            bool tryReacquire,
            in T repeater,
            int maxWaitMs,
            Func<DateTime>? utcNow = null
        ) where T: IRedlockRepeater
        {
            var (newValidUntil, attemptCount) = TryExtendInternal(tryReacquire, repeater, maxWaitMs, utcNow);
            
            if (newValidUntil != null)
            {
                return newValidUntil.Value;
            }

            throw repeater.CreateException(Resource, Nonce, attemptCount);
        }

        /// <summary>
        /// Try extend current lock in repeater loop
        /// </summary>
        /// <param name="tryReacquire">If true we try to reacquire lock when it is lost</param>
        /// <param name="repeater"></param>
        /// <param name="maxWaitMs">Max wait time before next attempt after previous failed</param>
        /// <param name="utcNow"></param>
        /// <typeparam name="T">Type of repeater</typeparam>
        /// <returns>New ValidUntil or null if extension failed</returns>
        public DateTime? TryExtend<T>(
            bool tryReacquire,
            in T repeater,
            int maxWaitMs,
            Func<DateTime>? utcNow = null
        ) where T : IRedlockRepeater 
            => TryExtendInternal(tryReacquire, repeater, maxWaitMs, utcNow).newValidUntil;
        
        private (DateTime? newValidUntil, int attemptCount) TryExtendInternal<T>(
            bool tryReacquire,
            in T repeater,
            int maxWaitMs,
            Func<DateTime>? utcNow = null
        ) where T: IRedlockRepeater
        {
            var attemptCount = 0;
            while (true)
            {
                var newValidUntil = TryExtend(tryReacquire, utcNow);
                if (newValidUntil != null)
                {
                    return (newValidUntil.Value, attemptCount);
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
        /// <param name="utcNow"></param>
        /// <typeparam name="T">Type of repeater</typeparam>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">If unable to acquire a lock by repeater loop</exception>
        public static async Task<Redlock> LockAsync<T>(string resource,
            string nonce,
            TimeSpan lockTimeToLive,
            IRedlockImplementation implementation,
            ILogger logger,
            T repeater,
            int maxWaitMs,
            Func<DateTime>? utcNow = null
        ) where T: IRedlockRepeater
        {
            var (redlock, attemptCount) = await TryLockInternalAsync(
                resource, nonce, lockTimeToLive, implementation, logger, repeater, maxWaitMs, utcNow);
        
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
            int maxWaitMs,
            Func<DateTime>? utcNow = null
        ) where T: IRedlockRepeater
        {
            var attemptCount = 0;
            while (true)
            {
                var @lock = await TryLockAsync(resource, nonce, lockTimeToLive, implementation, logger, utcNow);
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
        
        
        /// <summary>
        /// Extend current lock in repeater loop
        /// </summary>
        /// <param name="tryReacquire">If true we try to reacquire lock when it is lost</param>
        /// <param name="repeater"></param>
        /// <param name="maxWaitMs">Max wait time before next attempt after previous failed</param>
        /// <param name="utcNow"></param>
        /// <typeparam name="T">Type of repeater</typeparam>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">If unable to extend a lock by repeater loop</exception>
        public async Task<DateTime> ExtendAsync<T>(
            bool tryReacquire,
            T repeater,
            int maxWaitMs,
            Func<DateTime>? utcNow = null
        ) where T: IRedlockRepeater
        {
            var (newValidUntil, attemptCount) = await TryExtendInternalAsync(tryReacquire, repeater, maxWaitMs, utcNow);
            
            if (newValidUntil != null)
            {
                return newValidUntil.Value;
            }

            throw repeater.CreateException(Resource, Nonce, attemptCount);
        }

        /// <summary>
        /// Try extend current lock in repeater loop
        /// </summary>
        /// <param name="tryReacquire">If true we try to reacquire lock when it is lost</param>
        /// <param name="repeater"></param>
        /// <param name="maxWaitMs">Max wait time before next attempt after previous failed</param>
        /// <param name="utcNow"></param>
        /// <typeparam name="T">Type of repeater</typeparam>
        /// <returns>New ValidUntil or null if extension failed</returns>
        public async Task<DateTime?> TryExtendAsync<T>(
            bool tryReacquire,
            T repeater,
            int maxWaitMs,
            Func<DateTime>? utcNow = null
        ) where T : IRedlockRepeater 
            => (await TryExtendInternalAsync(tryReacquire, repeater, maxWaitMs, utcNow)).newValidUntil;
        
        private async Task<(DateTime? newValidUntil, int attemptCount)> TryExtendInternalAsync<T>(
            bool tryReacquire,
            T repeater,
            int maxWaitMs,
            Func<DateTime>? utcNow = null
        ) where T: IRedlockRepeater
        {
            var attemptCount = 0;
            while (true)
            {
                var newValidUntil = await TryExtendAsync(tryReacquire, utcNow);
                if (newValidUntil != null)
                {
                    return (newValidUntil.Value, attemptCount);
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
