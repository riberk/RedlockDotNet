using System;

namespace RedlockDotNet
{
    /// <summary>
    /// Options for redlock
    /// </summary>
    public class RedlockOptions
    {
        /// <summary>
        /// Drift factor for system clock (multiply with ttl of lock)
        /// </summary>
        public float ClockDriftFactor { get; set; } = 0.01f;

        /// <summary>Change this for your own for tests or other purposes</summary>
        public Func<DateTime> UtcNow { get; set; } = () => DateTime.UtcNow;
    }
}