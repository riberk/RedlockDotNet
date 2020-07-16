using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RedLock;

namespace Redlock.Internal
{
    internal static class RedlockExtensions
    {
        private static readonly double TimestampToTicks = TimeSpan.TicksPerSecond / (double) Stopwatch.Frequency;

        public static void UnlockAll(
            this ImmutableArray<IRedlockInstance> instances,
            ILogger logger,
            string resource,
            string nonce
        )
        {
            Parallel.ForEach(instances, i => i.UnlockSafe(logger, resource, nonce));
        }

        public static Task UnlockAllAsync(
            this ImmutableArray<IRedlockInstance> instances,
            ILogger logger,
            string resource,
            string nonce
        )
        {
            return Task.WhenAll(instances.Select(x => UnlockSafeAsync(x, logger, resource, nonce)));
        }

        public static LockResult TryLockAll(
            this ImmutableArray<IRedlockInstance> instances,
            ILogger logger,
            string resource,
            string nonce,
            TimeSpan lockTimeToLive
        )
        {
            var lockedCount = 0;
            var startTimestamp = Stopwatch.GetTimestamp();
            Parallel.ForEach(instances, i =>
            {
                if (i.TryLockSafe(logger, resource, nonce, lockTimeToLive))
                {
                    Interlocked.Increment(ref lockedCount);
                }
            });
            var endTimestamp = Stopwatch.GetTimestamp();
            var elapsed = new TimeSpan((long)(TimestampToTicks * (endTimestamp - startTimestamp)));
            return new LockResult(lockedCount, elapsed);
        }

        public static async Task<LockResult> TryLockAllAsync(
            this ImmutableArray<IRedlockInstance> instances,
            ILogger logger,
            string resource,
            string nonce,
            TimeSpan lockTimeToLive
        )
        {
            var startTimestamp = Stopwatch.GetTimestamp();
            var tasks = instances.Select(
                async x => await x.TryLockSafeAsync(logger, resource, nonce, lockTimeToLive).ConfigureAwait(false) ? 1 : 0
            );
            var lockedCount =(await Task.WhenAll(tasks).ConfigureAwait(false)).Sum();
            var endTimestamp = Stopwatch.GetTimestamp();
            var elapsed = new TimeSpan((long)(TimestampToTicks * (endTimestamp - startTimestamp)));
            return new LockResult(lockedCount, elapsed);
        }

        private static bool TryLockSafe(
            this IRedlockInstance instance,
            ILogger logger,
            string resource,
            string nonce,
            TimeSpan lockTimeToLive
        )
        {
            try
            {
                return instance.TryLock(resource, nonce, lockTimeToLive);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Unable to acquire lock ['{}'] = '{}' on [{}]", resource, nonce, instance);
                return false;
            }
        }

        private static async Task<bool> TryLockSafeAsync(
            this IRedlockInstance instance,
            ILogger logger,
            string resource,
            string nonce,
            TimeSpan lockTimeToLive
        )
        {
            try
            {
                return await instance.TryLockAsync(resource, nonce, lockTimeToLive).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Unable to acquire lock ['{}'] = '{}' on [{}]", resource, nonce, instance);
                return false;
            }
        }

        private static void UnlockSafe(
            this IRedlockInstance instance,
            ILogger logger,
            string resource,
            string nonce
        )
        {
            try
            {
                instance.Unlock(resource, nonce);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Unable to unlock ['{}'] = '{}' on [{}]", resource, nonce, instance);
            }
        }

        private static async Task UnlockSafeAsync(
            this IRedlockInstance instance,
            ILogger logger,
            string resource,
            string nonce
        )
        {
            try
            {
                await instance.UnlockAsync(resource, nonce).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Unable to unlock ['{}'] = '{}' on [{}]", resource, nonce, instance);
            }
        }
    }
}