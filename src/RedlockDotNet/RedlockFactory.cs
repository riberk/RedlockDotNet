using System;
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
        public Redlock? TryCreate(string resource, TimeSpan lockTimeToLive) 
            => Redlock.TryLock(resource, Nonce(resource, lockTimeToLive), lockTimeToLive, _impl, _logger, _opt.Value.UtcNow);

        /// <inheritdoc />
        public Redlock? TryCreate<T>(string resource, TimeSpan lockTimeToLive, T repeater, int maxWaitMs) 
            where T : IRedlockRepeater => Redlock.TryLock(resource, Nonce(resource, lockTimeToLive), lockTimeToLive, _impl, _logger, repeater, maxWaitMs, _opt.Value.UtcNow);

        /// <inheritdoc />
        public Redlock Create<T>(string resource, TimeSpan lockTimeToLive, T repeater, int maxWaitMs) 
            where T : IRedlockRepeater => Redlock.Lock(resource, Nonce(resource, lockTimeToLive), lockTimeToLive, _impl, _logger, repeater, maxWaitMs, _opt.Value.UtcNow);

        /// <inheritdoc />
        public Task<Redlock?> TryCreateAsync(string resource, TimeSpan lockTimeToLive) 
            => Redlock.TryLockAsync(resource, Nonce(resource, lockTimeToLive), lockTimeToLive, _impl, _logger, _opt.Value.UtcNow);

        /// <inheritdoc />
        public Task<Redlock?> TryCreateAsync<T>(string resource, TimeSpan lockTimeToLive, T repeater, int maxWaitMs)
            where T : IRedlockRepeater => Redlock.TryLockAsync(resource, Nonce(resource, lockTimeToLive), lockTimeToLive, _impl, _logger, _opt.Value.UtcNow);

        /// <inheritdoc />
        public Task<Redlock> CreateAsync<T>(string resource, TimeSpan lockTimeToLive, T repeater, int maxWaitMs)
            where T : IRedlockRepeater => Redlock.LockAsync(resource, Nonce(resource, lockTimeToLive), lockTimeToLive, _impl, _logger, repeater, maxWaitMs, _opt.Value.UtcNow);
        
        /// <summary>
        /// Gets nonce for locking resource
        /// </summary>
        /// <param name="resource">Locking resource</param>
        /// <param name="lockTimeToLive">Locking resource ttl</param>
        /// <returns>String to identify lock owners</returns>
        protected virtual string Nonce(string resource, TimeSpan lockTimeToLive)
        {
            return Guid.NewGuid().ToString("N");
        }

        /// <inheritdoc />
        public virtual TimeSpan DefaultTtl(string resource)
        {
            return TimeSpan.FromSeconds(30);
        }

        /// <inheritdoc />
        public int DefaultMaxWaitMsBetweenReplays(string resource, TimeSpan lockTimeToLive)
        {
            return 200;
        }
    }
}