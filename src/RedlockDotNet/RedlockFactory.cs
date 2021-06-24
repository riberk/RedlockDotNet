using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RedlockDotNet.Repeaters;

namespace RedlockDotNet
{
    /// <summary><see cref="Redlock"/> factory</summary>
    public class RedlockFactory : IRedlockFactory
    {
        private readonly IRedlockImplementation _impl;
        private readonly IOptions<RedlockOptions> _opt;
        private readonly ILogger<RedlockFactory> _logger;

        /// <summary><see cref="Redlock"/> factory</summary>
        public RedlockFactory(
            IRedlockImplementation impl,
            IOptions<RedlockOptions> opt,
            ILogger<RedlockFactory> logger
        )
        {
            _impl = impl;
            _opt = opt;
            _logger = logger;
        }

        /// <inheritdoc />
        public Redlock? TryCreate(string resource, TimeSpan lockTimeToLive, IReadOnlyDictionary<string, string>? meta)
        {
            return Redlock.TryLock(resource, Nonce(resource, lockTimeToLive), lockTimeToLive, _impl.Instances, _logger,
                _opt.Value.UtcNow, meta);
        }

        /// <inheritdoc />
        public Redlock? TryCreate<T>(string resource, TimeSpan lockTimeToLive, T repeater, IReadOnlyDictionary<string, string>? meta, int maxWaitMs) 
            where T : IRedlockRepeater
        {
            return Redlock.TryLock(
                resource,
                Nonce(resource, lockTimeToLive),
                lockTimeToLive,
                _impl.Instances,
                _logger,
                repeater,
                maxWaitMs,
                _opt.Value.UtcNow, 
                meta
            );
        }

        /// <inheritdoc />
        public Redlock Create<T>(string resource, TimeSpan lockTimeToLive, T repeater, int maxWaitMs, IReadOnlyDictionary<string, string>? meta = null) 
            where T : IRedlockRepeater
        {
            return Redlock.Lock(
                resource,
                Nonce(resource, lockTimeToLive),
                lockTimeToLive,
                _impl.Instances,
                _logger,
                repeater,
                maxWaitMs,
                _opt.Value.UtcNow,
                meta
            );
        }

        /// <inheritdoc />
        public Task<Redlock?> TryCreateAsync(string resource, TimeSpan lockTimeToLive, IReadOnlyDictionary<string, string>? meta = null)
        {
            return Redlock.TryLockAsync(
                resource,
                Nonce(resource, lockTimeToLive),
                lockTimeToLive,
                _impl.Instances,
                _logger,
                _opt.Value.UtcNow, 
                meta
            );
        }

        /// <inheritdoc />
        public Task<Redlock?> TryCreateAsync<T>(string resource, TimeSpan lockTimeToLive, T repeater, int maxWaitMs, IReadOnlyDictionary<string, string>? meta = null)
            where T : IRedlockRepeater
        {
            return Redlock.TryLockAsync(
                resource,
                Nonce(resource, lockTimeToLive),
                lockTimeToLive,
                _impl.Instances,
                _logger, 
                repeater,
                maxWaitMs,
                _opt.Value.UtcNow,
                meta
            );
        }

        /// <inheritdoc />
        public Task<Redlock> CreateAsync<T>(string resource, TimeSpan lockTimeToLive, T repeater, int maxWaitMs, IReadOnlyDictionary<string, string>? meta = null)
            where T : IRedlockRepeater
        {
            return Redlock.LockAsync(
                resource,
                Nonce(resource, lockTimeToLive),
                lockTimeToLive,
                _impl.Instances,
                _logger,
                repeater,
                maxWaitMs,
                _opt.Value.UtcNow,
                meta
            );
        }

        /// <summary>
        /// Gets nonce for locking resource
        /// </summary>
        /// <param name="resource">Locking resource</param>
        /// <param name="lockTimeToLive">Locking resource ttl</param>
        /// <returns>String to identify lock owners</returns>
        protected virtual string Nonce(string resource, TimeSpan lockTimeToLive) => Guid.NewGuid().ToString("N");

        /// <inheritdoc />
        public virtual TimeSpan DefaultTtl(string resource) => TimeSpan.FromSeconds(30);

        /// <inheritdoc />
        public int DefaultMaxWaitMsBetweenReplays(string resource, TimeSpan lockTimeToLive) => 200;
    }
}