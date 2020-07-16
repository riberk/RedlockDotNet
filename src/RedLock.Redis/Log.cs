using System;
using Microsoft.Extensions.Logging;

namespace RedLock.Redis
{
    internal static class Log
    {
        // ReSharper disable InconsistentNaming
        private static readonly Action<ILogger, string, string, string, TimeSpan, string, Exception?> _tryLock =
            LoggerMessage.Define<string, string, string, TimeSpan, string>(LogLevel.Trace, 1,
                "Try obtain lock ['{}'] = '{}' on '{}', ttl: {} (redis key: '{}')");
        
        private static readonly Action<ILogger, string, string, string, string, Exception?> _unlocking =
            LoggerMessage.Define<string, string, string, string>(LogLevel.Trace, 2,
                "Unlocking  ['{}'] = '{}' on '{}' (redis key: '{}')");
        
        private static readonly Action<ILogger, string, string, string, string, bool, Exception?> _unlocked =
            LoggerMessage.Define<string, string, string, string, bool>(LogLevel.Trace, 3,
                "Unlocked  ['{}'] = '{}' on '{}' (redis key: '{}'). Result: {}");
        // ReSharper restore InconsistentNaming

        public static void TryLock(
            this ILogger l, string resource, string nonce, string instanceName, TimeSpan lockTimeToLive, string redisKey
        ) => _tryLock(l, resource, nonce, instanceName, lockTimeToLive, redisKey, null);
        
        public static void Unlocking(this ILogger l, string resource, string nonce, string instanceName, string redisKey) 
            => _unlocking(l, resource, nonce, instanceName, redisKey, null);
        
        public static void Unlocked(this ILogger l, string resource, string nonce, string instanceName, string redisKey, bool result) 
            => _unlocked(l, resource, nonce, instanceName, redisKey, result, null);
    }
}