using System;

namespace RedlockDotNet
{
    /// <summary>
    /// Options for redlock
    /// </summary>
    public class RedlockOptions
    {
        /// <summary>Default drift factor for system clock</summary>
        public const float DefaultClockDriftFactor = 0.01f;
        
        /// <summary>Drift factor for system clock (multiply with ttl of lock)</summary>
        public float ClockDriftFactor { get; set; } = DefaultClockDriftFactor;

        /// <summary>Change this for your own for tests or other purposes</summary>
        public Func<DateTime> UtcNow { get; set; } = () => DateTime.UtcNow;
    }
}