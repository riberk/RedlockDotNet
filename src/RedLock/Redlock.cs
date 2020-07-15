using System;
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
        public static async ValueTask<Redlock?> TryLockAsync(
            string resource,
            string nonce,
            TimeSpan lockTimeToLive,
            IRedlockImplementation implementation,
            ILogger logger
        )
        {
            var lockResult = await implementation.Instances.TryLockAllAsync(logger, resource, nonce, lockTimeToLive).ConfigureAwait(false);

            if (lockResult.IsLocked(lockTimeToLive, implementation))
            {
                return new Redlock(resource, nonce, implementation, logger);
            }

            await implementation.Instances.UnlockAllAsync(logger, resource, nonce).ConfigureAwait(false);
            return null;
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
