using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

        private readonly ImmutableArray<IRedlockInstance> _instances;
        private readonly ILogger _logger;

        /// <summary>
        /// Implements Redlock (https://redis.io/topics/distlock) alg for custom lock providers
        /// </summary>
        public Redlock(
            string resource,
            string nonce,
            TimeSpan ttl,
            DateTime validUntilUtc,
            ImmutableArray<IRedlockInstance> instances,
            ILogger logger
        )
        {
            Resource = resource;
            Nonce = nonce;
            Ttl = ttl;
            ValidUntilUtc = validUntilUtc;
            _instances = instances;
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
        /// <param name="instances">Instances for acquirement of a lock</param>

        /// <param name="logger"></param>
        /// <param name="utcNow"></param>
        /// <param name="metadata"></param>
        /// <returns>Lock object or null if it failed</returns>
        public static Redlock? TryLock(
            string resource,
            string nonce,
            TimeSpan lockTimeToLive,
            ImmutableArray<IRedlockInstance> instances,
            ILogger logger,
            Func<DateTime>? utcNow = null,
            IReadOnlyDictionary<string, string>? metadata = null
        )
        {
            logger.Locking(resource, nonce, lockTimeToLive);

            var lockResult = instances.TryLockAll(logger, resource, nonce, lockTimeToLive, metadata);

            if (lockResult.IsLocked(utcNow, out var validUntil))
            {
                logger.Locked(resource, nonce, lockResult);
                return new Redlock(resource, nonce, lockTimeToLive, validUntil, instances, logger);
            }

            logger.UnlockingOnFail(resource, nonce, lockResult);
            instances.UnlockAll(logger, resource, nonce);
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
        /// <param name="instances">Instances for acquirement of a lock</param>
        /// <param name="logger"></param>
        /// <param name="utcNow"></param>
        /// <param name="metadata"></param>
        /// <returns>Lock object or null if it failed</returns>
        public static async Task<Redlock?> TryLockAsync(
            string resource,
            string nonce,
            TimeSpan lockTimeToLive,
            ImmutableArray<IRedlockInstance> instances,
            ILogger logger,
            Func<DateTime>? utcNow = null,
            IReadOnlyDictionary<string, string>? metadata = null
        )
        {
            logger.Locking(resource, nonce, lockTimeToLive);
            var lockResult = await instances.TryLockAllAsync(logger, resource, nonce, lockTimeToLive, metadata)
                .ConfigureAwait(false);

            if (lockResult.IsLocked(utcNow, out var validUntil))
            {
                logger.Locked(resource, nonce, lockResult);
                return new Redlock(resource, nonce, lockTimeToLive, validUntil, instances, logger);
            }

            logger.UnlockingOnFail(resource, nonce, lockResult);
            await instances.UnlockAllAsync(logger, resource, nonce).ConfigureAwait(false);
            logger.UnlockedOnFail(resource, nonce);
            return null;
        }
        
        /// <summary>
        /// Try extend current lock
        /// </summary>
        /// <param name="utcNow"></param>
        /// <returns>New ValidUntil or null if extension failed</returns>
        public DateTime? TryExtend(Func<DateTime>? utcNow = null)
        {
            _logger.Extending(Resource, Nonce, Ttl);
            var lockResult = _instances.TryExtendAll(_logger, Resource, Nonce, Ttl);
            if (lockResult.IsLocked(utcNow, out var validUntil))
            {
                _logger.Extended(Resource, Nonce, Ttl, validUntil);
                return validUntil;
            }

            _logger.ExtendFail(Resource, Nonce, Ttl);
            return null;
        }

        /// <summary>
        /// Try extend current lock
        /// </summary>
        /// <param name="utcNow"></param>
        /// <returns>New ValidUntil or null if extension failed</returns>
        public async Task<DateTime?> TryExtendAsync(Func<DateTime>? utcNow = null)
        {
            _logger.Extending(Resource, Nonce, Ttl);
            var lockResult = await _instances.TryExtendAllAsync(_logger, Resource, Nonce, Ttl)
                .ConfigureAwait(false);

            if (lockResult.IsLocked(utcNow, out var validUntil))
            {
                _logger.Extended(Resource, Nonce, Ttl, validUntil);
                return validUntil;
            }

            _logger.ExtendFail(Resource, Nonce, Ttl);
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
        /// <param name="instances">Instances for acquirement of a lock</param>
        /// <param name="logger"></param>
        /// <param name="repeater"></param>
        /// <param name="maxWaitMs">Max wait time before next attempt after previous failed</param>
        /// <param name="utcNow"></param>
        /// <param name="metadata"></param>
        /// <typeparam name="T">Type of repeater</typeparam>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">If unable to acquire a lock by repeater loop</exception>
        public static Redlock Lock<T>(
            string resource,
            string nonce,
            TimeSpan lockTimeToLive,
            ImmutableArray<IRedlockInstance> instances,
            ILogger logger,
            in T repeater,
            int maxWaitMs,
            Func<DateTime>? utcNow = null,
            IReadOnlyDictionary<string, string>? metadata = null
        ) where T: IRedlockRepeater
        {
            var (redlock, attemptCount) = TryLockInternal(
                resource, nonce, lockTimeToLive, instances, logger, repeater, maxWaitMs, utcNow, metadata
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
        /// <param name="instances">Instances for acquirement of a lock</param>
        /// <param name="logger"></param>
        /// <param name="repeater"></param>
        /// <param name="maxWaitMs">Max wait time before next attempt after previous failed</param>
        /// <param name="utcNow"></param>
        /// <param name="metadata"></param>
        /// <typeparam name="T">Type of repeater</typeparam>
        /// <returns>lock or null if unable to acquire</returns>
        public static Redlock? TryLock<T>(
            string resource,
            string nonce,
            TimeSpan lockTimeToLive,
            ImmutableArray<IRedlockInstance> instances,
            ILogger logger,
            in T repeater,
            int maxWaitMs,
            Func<DateTime>? utcNow = null,
            IReadOnlyDictionary<string, string>? metadata = null
        ) where T : IRedlockRepeater 
            => TryLockInternal(resource, nonce, lockTimeToLive, instances, logger, repeater, maxWaitMs, utcNow, metadata).redlock;
        
        private static (Redlock? redlock, int attemptCount) TryLockInternal<T>(
            string resource,
            string nonce,
            TimeSpan lockTimeToLive,
            ImmutableArray<IRedlockInstance> instances,
            ILogger logger,
            in T repeater,
            int maxWaitMs,
            Func<DateTime>? utcNow = null,
            IReadOnlyDictionary<string, string>? metadata = null
        ) where T: IRedlockRepeater
        {
            var attemptCount = 0;
            while (true)
            {
                var @lock = TryLock(resource, nonce, lockTimeToLive, instances, logger, utcNow, metadata);
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
        /// <param name="repeater"></param>
        /// <param name="maxWaitMs">Max wait time before next attempt after previous failed</param>
        /// <param name="utcNow"></param>
        /// <typeparam name="T">Type of repeater</typeparam>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">If unable to extend a lock by repeater loop</exception>
        public DateTime Extend<T>(
            in T repeater,
            int maxWaitMs,
            Func<DateTime>? utcNow = null
        ) where T: IRedlockRepeater
        {
            var (newValidUntil, attemptCount) = TryExtendInternal(repeater, maxWaitMs, utcNow);
            
            if (newValidUntil != null)
            {
                return newValidUntil.Value;
            }

            throw repeater.CreateException(Resource, Nonce, attemptCount);
        }

        /// <summary>
        /// Try extend current lock in repeater loop
        /// </summary>
        /// <param name="repeater"></param>
        /// <param name="maxWaitMs">Max wait time before next attempt after previous failed</param>
        /// <param name="utcNow"></param>
        /// <typeparam name="T">Type of repeater</typeparam>
        /// <returns>New ValidUntil or null if extension failed</returns>
        public DateTime? TryExtend<T>(
            in T repeater,
            int maxWaitMs,
            Func<DateTime>? utcNow = null
        ) where T : IRedlockRepeater 
            => TryExtendInternal(repeater, maxWaitMs, utcNow).newValidUntil;
        
        private (DateTime? newValidUntil, int attemptCount) TryExtendInternal<T>(
            in T repeater,
            int maxWaitMs,
            Func<DateTime>? utcNow = null
        ) where T: IRedlockRepeater
        {
            var attemptCount = 0;
            while (true)
            {
                var newValidUntil = TryExtend(utcNow);
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
        /// <param name="instances">Instances for acquirement of a lock</param>
        /// <param name="logger"></param>
        /// <param name="repeater"></param>
        /// <param name="maxWaitMs">Max wait time before next attempt after previous failed</param>
        /// <param name="utcNow"></param>
        /// <param name="metadata"></param>
        /// <typeparam name="T">Type of repeater</typeparam>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">If unable to acquire a lock by repeater loop</exception>
        public static async Task<Redlock> LockAsync<T>(string resource,
            string nonce,
            TimeSpan lockTimeToLive,
            ImmutableArray<IRedlockInstance> instances,
            ILogger logger,
            T repeater,
            int maxWaitMs,
            Func<DateTime>? utcNow = null,
            IReadOnlyDictionary<string, string>? metadata = null
        ) where T: IRedlockRepeater
        {
            var (redlock, attemptCount) = await TryLockInternalAsync(
                resource, nonce, lockTimeToLive, instances, logger, repeater, maxWaitMs, utcNow, metadata);
        
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
        /// <param name="instances">Instances for acquirement of a lock</param>
        /// <param name="logger"></param>
        /// <param name="repeater"></param>
        /// <param name="maxWaitMs">Max wait time before next attempt after previous failed</param>
        /// <param name="utcNow"></param>
        /// <param name="metadata"></param>
        /// <typeparam name="T">Type of repeater</typeparam>
        /// <returns>lock or null if unable to acquire</returns>
        public static async Task<Redlock?> TryLockAsync<T>(
            string resource,
            string nonce,
            TimeSpan lockTimeToLive,
            ImmutableArray<IRedlockInstance> instances,
            ILogger logger,
            T repeater,
            int maxWaitMs,
            Func<DateTime>? utcNow = null,
            IReadOnlyDictionary<string, string>? metadata = null
        ) where T: IRedlockRepeater 
            => (await TryLockInternalAsync(resource, nonce, lockTimeToLive, instances, logger, repeater, maxWaitMs, utcNow, metadata)).redlock;

        private static async Task<(Redlock? redlock, int attemptCount)> TryLockInternalAsync<T>(
            string resource,
            string nonce,
            TimeSpan lockTimeToLive,
            ImmutableArray<IRedlockInstance> instances,
            ILogger logger,
            T repeater,
            int maxWaitMs,
            Func<DateTime>? utcNow = null,
            IReadOnlyDictionary<string, string>? metadata = null
        ) where T: IRedlockRepeater
        {
            var attemptCount = 0;
            while (true)
            {
                var @lock = await TryLockAsync(resource, nonce, lockTimeToLive, instances, logger, utcNow, metadata);
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
        /// <param name="repeater"></param>
        /// <param name="maxWaitMs">Max wait time before next attempt after previous failed</param>
        /// <param name="utcNow"></param>
        /// <typeparam name="T">Type of repeater</typeparam>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">If unable to extend a lock by repeater loop</exception>
        public async Task<DateTime> ExtendAsync<T>(
            T repeater,
            int maxWaitMs,
            Func<DateTime>? utcNow = null
        ) where T: IRedlockRepeater
        {
            var (newValidUntil, attemptCount) = await TryExtendInternalAsync(repeater, maxWaitMs, utcNow);
            
            if (newValidUntil != null)
            {
                return newValidUntil.Value;
            }

            throw repeater.CreateException(Resource, Nonce, attemptCount);
        }

        /// <summary>
        /// Try extend current lock in repeater loop
        /// </summary>
        /// <param name="repeater"></param>
        /// <param name="maxWaitMs">Max wait time before next attempt after previous failed</param>
        /// <param name="utcNow"></param>
        /// <typeparam name="T">Type of repeater</typeparam>
        /// <returns>New ValidUntil or null if extension failed</returns>
        public async Task<DateTime?> TryExtendAsync<T>(
            T repeater,
            int maxWaitMs,
            Func<DateTime>? utcNow = null
        ) where T : IRedlockRepeater 
            => (await TryExtendInternalAsync(repeater, maxWaitMs, utcNow)).newValidUntil;
        
        private async Task<(DateTime? newValidUntil, int attemptCount)> TryExtendInternalAsync<T>(
            T repeater,
            int maxWaitMs,
            Func<DateTime>? utcNow = null
        ) where T: IRedlockRepeater
        {
            var attemptCount = 0;
            while (true)
            {
                var newValidUntil = await TryExtendAsync(utcNow);
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
            _instances.UnlockAll(_logger, Resource, Nonce);
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (_instances == null)
            {
                return;
            }
            await _instances.UnlockAllAsync(_logger, Resource, Nonce).ConfigureAwait(false);
        }
    }
}
