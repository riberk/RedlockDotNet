using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace RedlockDotNet
{
    /// <summary>Implementation for RedLock alg</summary>
    public class RedlockImplementation : IRedlockImplementation
    {
        
        /// <summary>Implementation for RedLock alg</summary>
        public RedlockImplementation(
            IEnumerable<IRedlockInstance> instances
        )
        {
            Instances = instances.ToImmutableArray();
            if (Instances.Length < 1)
            {
                throw new ArgumentException($"{nameof(instances)} must not be an empty collection", nameof(instances));
            }
        }
        
        /// <inheritdoc />
        public ImmutableArray<IRedlockInstance> Instances { get; }
    }
}