using System;
using Microsoft.Extensions.Logging;

namespace RedlockDotNet.Internal
{
    internal static class Log
    {
        // ReSharper disable InconsistentNaming
        private static readonly Action<ILogger, string, string, TimeSpan, Exception?> _locking =
            LoggerMessage.Define<string, string, TimeSpan>(LogLevel.Debug, 1, "Try lock ['{}'] = '{}', ttl: {}");

        private static readonly Action<ILogger, string, string, TimeSpan, int, Exception?> _locked =
            LoggerMessage.Define<string, string, TimeSpan, int>(LogLevel.Debug, 1,
                "Locked ['{}'] = '{}'. Duration: {}; Locked on: {}");
            
        private static readonly Action<ILogger, string, string, TimeSpan, int, Exception?> _unlockingOnFail =
            LoggerMessage.Define<string, string, TimeSpan, int>(LogLevel.Debug, 1,
                "Lock ['{}'] = '{}' failed. Duration: {}; Locked on: {}. Unlocking");
            
        private static readonly Action<ILogger, string, string, Exception?> _unlockedOnFail =
            LoggerMessage.Define<string, string>(LogLevel.Debug, 1,
                "['{}'] = '{}' unlocked after lock failed");
            
        // ReSharper restore InconsistentNaming

        public static void Locking(this ILogger logger, string resource, string nonce, TimeSpan ttl)
            => _locking(logger, resource, nonce, ttl, null);

        public static void Locked(this ILogger logger, string resource, string nonce, in LockResult res)
            => _locked(logger, resource, nonce, res.Elapsed, res.LockedCount, null);
        
        public static void UnlockingOnFail(this ILogger logger, string resource, string nonce, in LockResult res)
            => _unlockingOnFail(logger, resource, nonce, res.Elapsed, res.LockedCount, null);
        
        public static void UnlockedOnFail(this ILogger logger, string resource, string nonce)
            => _unlockedOnFail(logger, resource, nonce, null);
    }
}