using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Extensions.Options;

namespace RedLock.Redis
{
    /// <summary>
    /// Implementations for RedLock alg for redis
    /// </summary>
    public class RedisRedlockImplementation : IRedlockImplementation
    {
        private readonly IOptions<RedisRedlockOptions> _options;
        private static readonly TimeSpan RedisResolution = TimeSpan.FromMilliseconds(2);

        /// <summary>
        /// Implementations for RedLock alg for redis
        /// </summary>
        public RedisRedlockImplementation(
            IEnumerable<IRedlockInstance> instances,
            IOptions<RedisRedlockOptions> options
        )
        {
            Instances = instances.ToImmutableArray();
            if (Instances.Length < 1)
            {
                throw new ArgumentException($"{nameof(instances)} must not be an empty collection", nameof(instances));
            }
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }
        
        /// <inheritdoc />
        public TimeSpan MinValidity(TimeSpan lockTimeToLive, TimeSpan lockingDuration)
        {
            var drift = lockTimeToLive * _options.Value.ClockDriftFactor;
            return lockTimeToLive - lockingDuration - drift - RedisResolution;
        }

        /// <inheritdoc />
        public ImmutableArray<IRedlockInstance> Instances { get; }
    }
}