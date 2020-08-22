using System;

namespace RedlockDotNet.Redis
{
    /// <summary>
    /// Options for <see cref="RedisRedlockInstance"/>
    /// </summary>
    public class RedisRedlockOptions
    {
        /// <summary>Creates redis key from name of locking resource</summary>
        public Func<string, string> RedisKeyFromResourceName { get; set; } = k => k;
    }
}