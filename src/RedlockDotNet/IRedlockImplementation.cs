using System.Collections.Immutable;

namespace RedlockDotNet
{
    /// <summary>
    /// Implementations for RedLock alg (servers, MinValidity calculation)
    /// </summary>
    public interface IRedlockImplementation
    {
        /// <summary>
        /// Array of instances for acquire lock
        /// </summary>
        ImmutableArray<IRedlockInstance> Instances { get; }
    }
}
