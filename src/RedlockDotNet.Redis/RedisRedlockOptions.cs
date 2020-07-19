using System;

namespace RedlockDotNet.Redis
{
    /// <summary>
    /// Options for <see cref="RedisRedlockImplementation"/>
    /// </summary>
    public class RedisRedlockOptions
    {
        /// <summary>
        /// Drift factor for system clock (multiply with ttl of lock)
        /// </summary>
        public float ClockDriftFactor { get; set; } = 0.01f;

        /// <summary>Creates redis key from name of locking resource</summary>
        public Func<string, string> RedisKeyFromResourceName { get; set; } = k => k;
    }
}