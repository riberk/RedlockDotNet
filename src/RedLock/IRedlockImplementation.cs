using System;
using System.Collections.Immutable;

namespace RedLock
{
    /// <summary>
    /// Implementations for RedLock alg (servers, MinValidity calculation)
    /// </summary>
    public interface IRedlockImplementation
    {
        /// <summary>
        /// Calculate min validity time of lock
        /// </summary>
        /// <param name="lockTimeToLive">Ttl of lock on the server</param>
        /// <param name="lockingDuration">Elapsed time from start of locking on first server to end on last server</param>
        /// <returns>Minimum validity time of acquired lock</returns>
        TimeSpan MinValidity(TimeSpan lockTimeToLive, TimeSpan lockingDuration);

        /// <summary>
        /// Array of instances for acquire lock
        /// </summary>
        ImmutableArray<IRedlockInstance> Instances { get; }
    }
}
