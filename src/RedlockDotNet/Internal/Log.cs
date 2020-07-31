using System;
using Microsoft.Extensions.Logging;

namespace RedlockDotNet.Internal
{
    internal static class Log
    {
        // ReSharper disable InconsistentNaming
        private static readonly Action<ILogger, string, string, TimeSpan, Exception?> _locking =
            LoggerMessage.Define<string, string, TimeSpan>(LogLevel.Debug, new EventId(1, nameof(Locking)), 
                "Try lock ['{}'] = '{}', ttl: {}");

        private static readonly Action<ILogger, string, string, TimeSpan, int, Exception?> _locked =
            LoggerMessage.Define<string, string, TimeSpan, int>(LogLevel.Debug, new EventId(2, nameof(Locked)),
                "Locked ['{}'] = '{}'. Duration: {}; Locked on: {}");
            
        private static readonly Action<ILogger, string, string, TimeSpan, int, Exception?> _unlockingOnFail =
            LoggerMessage.Define<string, string, TimeSpan, int>(LogLevel.Debug, new EventId(3, nameof(UnlockingOnFail)),
                "Lock ['{}'] = '{}' failed. Duration: {}; Locked on: {}. Unlocking");
            
        private static readonly Action<ILogger, string, string, Exception?> _unlockedOnFail =
            LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId(4, nameof(UnlockedOnFail)),
                "['{}'] = '{}' unlocked after lock failed");
        
        private static readonly Action<ILogger, string, string, TimeSpan, bool, Exception?> _extending =
            LoggerMessage.Define<string, string, TimeSpan, bool>(LogLevel.Debug, new EventId(5, nameof(Extending)),
                "Try extend lock ['{}'] = '{}', ttl: {}, tryReacquire: {}");
        
        private static readonly Action<ILogger, string, string, TimeSpan, DateTime, Exception?> _extended =
            LoggerMessage.Define<string, string, TimeSpan, DateTime>(LogLevel.Debug, new EventId(6, nameof(Extended)),
                "Lock ['{}'] = '{}' extended, ttl: {}. Valid until {}");
        
        private static readonly Action<ILogger, string, string, TimeSpan, bool, Exception?> _extendFail =
            LoggerMessage.Define<string, string, TimeSpan, bool>(LogLevel.Debug, new EventId(7, nameof(ExtendFail)),
                "Fail extend lock ['{}'] = '{}', ttl: {}, tryReacquire: {}");
            
        // ReSharper restore InconsistentNaming

        public static void Locking(this ILogger logger, string resource, string nonce, TimeSpan ttl)
            => _locking(logger, resource, nonce, ttl, null);

        public static void Locked(this ILogger logger, string resource, string nonce, in LockResult res)
            => _locked(logger, resource, nonce, res.Elapsed, res.LockedCount, null);
        
        public static void UnlockingOnFail(this ILogger logger, string resource, string nonce, in LockResult res)
            => _unlockingOnFail(logger, resource, nonce, res.Elapsed, res.LockedCount, null);
        
        public static void UnlockedOnFail(this ILogger logger, string resource, string nonce)
            => _unlockedOnFail(logger, resource, nonce, null);
        
        public static void Extending(this ILogger logger, string resource, string nonce, TimeSpan ttl, bool tryReacquire)
            => _extending(logger, resource, nonce, ttl, tryReacquire, null);
        public static void Extended(this ILogger logger, string resource, string nonce, TimeSpan ttl, DateTime newValidUntil)
            => _extended(logger, resource, nonce, ttl, newValidUntil, null);
        public static void ExtendFail(this ILogger logger, string resource, string nonce, TimeSpan ttl, bool tryReacquire)
            => _extendFail(logger, resource, nonce, ttl, tryReacquire, null);
    }
}