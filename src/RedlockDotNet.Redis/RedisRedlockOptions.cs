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
    }
}