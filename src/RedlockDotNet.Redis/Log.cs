using System;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace RedlockDotNet.Redis
{
    internal static class Log
    {
        // ReSharper disable InconsistentNaming
        private static readonly Action<ILogger, string, string, string, TimeSpan, RedisKey, Exception?> _tryLock =
            LoggerMessage.Define<string, string, string, TimeSpan, RedisKey>(LogLevel.Trace, new EventId(1, nameof(TryLock)), 
                "Try obtain lock ['{}'] = '{}' on '{}', ttl: {} (redis key: '{}')");
        
        private static readonly Action<ILogger, string, string, string, RedisKey, Exception?> _unlocking =
            LoggerMessage.Define<string, string, string, RedisKey>(LogLevel.Trace, new EventId(2, nameof(Unlocking)), 
                "Unlocking  ['{}'] = '{}' on '{}' (redis key: '{}')");
        
        private static readonly Action<ILogger, string, string, string, RedisKey, bool, Exception?> _unlocked =
            LoggerMessage.Define<string, string, string, RedisKey, bool>(LogLevel.Trace, new EventId(3, nameof(Unlocked)),
                "Unlocked  ['{}'] = '{}' on '{}' (redis key: '{}'). Result: {}");
        
        private static readonly Action<ILogger, string, string, string, TimeSpan, RedisKey, Exception?> _tryExtendLock =
            LoggerMessage.Define<string, string, string, TimeSpan, RedisKey>(LogLevel.Trace, new EventId(4, nameof(TryExtendLock)),
                "Try extend lock ['{}'] = '{}' on '{}', ttl: {} (redis key: '{}')");
        
        private static readonly Action<ILogger, string, string, string, TimeSpan, RedisKey, string?, Exception?> _extendScriptExecuted =
            LoggerMessage.Define<string, string, string, TimeSpan, RedisKey, string?>(LogLevel.Trace, new EventId(5, nameof(Unlocking)),
                "Extend script executed ['{}'] = '{}' on '{}', ttl: {}, (redis key: '{}'). Result: '{}'");
        
        // ReSharper restore InconsistentNaming

        public static void TryLock(
            this ILogger l, string resource, string nonce, string instanceName, TimeSpan lockTimeToLive, RedisKey redisKey
        ) => _tryLock(l, resource, nonce, instanceName, lockTimeToLive, redisKey, null);
        
        public static void Unlocking(this ILogger l, string resource, string nonce, string instanceName, RedisKey redisKey) 
            => _unlocking(l, resource, nonce, instanceName, redisKey, null);
        
        public static void Unlocked(this ILogger l, string resource, string nonce, string instanceName, RedisKey redisKey, bool result) 
            => _unlocked(l, resource, nonce, instanceName, redisKey, result, null);
        
        public static void TryExtendLock(this ILogger l, string resource, string nonce, string instanceName, 
            TimeSpan lockTtl, RedisKey redisKey) 
            => _tryExtendLock(l, resource, nonce, instanceName, lockTtl, redisKey, null);
        
        public static void ExtendScriptExecuted(this ILogger l, string resource, string nonce, string instanceName, 
            TimeSpan lockTtl, RedisKey redisKey, RedisResult result) 
            => _extendScriptExecuted(l, resource, nonce, instanceName, lockTtl, redisKey, (string?)result, null);
    }
}