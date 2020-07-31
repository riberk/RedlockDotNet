using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Extensions.Options;

namespace RedlockDotNet.Redis
{
    /// <summary>
    /// Implementations for RedLock alg for redis
    /// </summary>
    public class RedisRedlockImplementation : IRedlockImplementation
    {
        private readonly IOptions<RedlockOptions> _redlockOptions;
        
        /// <summary>
        /// Redis expire error is from 0 to 1 milliseconds. (https://redis.io/commands/expire#expire-accuracy)
        /// </summary>
        private static readonly TimeSpan RedisResolution = TimeSpan.FromMilliseconds(1);
        
        /// <summary>
        /// We cast <see cref="TimeSpan.TotalMilliseconds"/> from double to long then 10.99 = 10
        /// </summary>
        private static readonly TimeSpan SharpTimeSpanCastMaxError = TimeSpan.FromMilliseconds(1);
        
        private static readonly TimeSpan ConstDrift = RedisResolution + SharpTimeSpanCastMaxError;

        /// <summary>
        /// Implementations for RedLock alg for redis
        /// </summary>
        public RedisRedlockImplementation(
            IEnumerable<IRedlockInstance> instances,
            IOptions<RedlockOptions> redlockOptions
        )
        {
            Instances = instances.ToImmutableArray();
            if (Instances.Length < 1)
            {
                throw new ArgumentException($"{nameof(instances)} must not be an empty collection", nameof(instances));
            }
            _redlockOptions = redlockOptions;
        }
        
        /// <inheritdoc />
        public TimeSpan MinValidity(TimeSpan lockTimeToLive, TimeSpan lockingDuration)
        {
            var drift = lockTimeToLive * _redlockOptions.Value.ClockDriftFactor;
            return lockTimeToLive - lockingDuration - drift - ConstDrift;
        }

        /// <inheritdoc />
        public ImmutableArray<IRedlockInstance> Instances { get; }
    }
}