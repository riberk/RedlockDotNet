using System;
using System.Collections.Generic;

namespace RedlockDotNet
{
    /// <summary>Acquired lock information from instance</summary>
    /// <param name="Nonce">Value to differentiate lock owners of resource on server</param>
    /// <param name="Metadata">Metadata for lock</param>
    /// <param name="Ttl">Time to live</param>
    public record InstanceLockInfo(string Nonce, IReadOnlyDictionary<string, string> Metadata, TimeSpan? Ttl);
}