using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace RedlockDotNet.Internal
{
    internal static class RedlockExtensions
    {
        public static void UnlockAll(
            this ImmutableArray<IRedlockInstance> instances,
            ILogger logger,
            string resource,
            string nonce
        )
        {
            if (instances.IsDefaultOrEmpty)
            {
                return;
            }
            Parallel.ForEach(instances, i => i.UnlockSafe(logger, resource, nonce));
        }

        public static Task UnlockAllAsync(
            this ImmutableArray<IRedlockInstance> instances,
            ILogger logger,
            string resource,
            string nonce
        )
        {
            return instances.IsDefaultOrEmpty 
                ? Task.CompletedTask 
                : Task.WhenAll(instances.Select(x => UnlockSafeAsync(x, logger, resource, nonce)));
        }

        public static LockResult TryLockAll(
            this ImmutableArray<IRedlockInstance> instances,
            ILogger logger,
            string resource,
            string nonce,
            TimeSpan lockTimeToLive,
            IReadOnlyDictionary<string, string>? metadata = null
        )
        {
            if (instances.IsDefaultOrEmpty)
            {
                return LockResult.Empty;
            }
            
            var lockedCount = 0;
            var lockResultBuilder = LockResultBuilder.Start(instances, lockTimeToLive);
            Parallel.ForEach(instances, instance =>
            {
                if (instance.TryLockSafe(logger, resource, nonce, lockTimeToLive, metadata))
                {
                    Interlocked.Increment(ref lockedCount);
                }
            });
            return lockResultBuilder.End(lockedCount);
        }

        public static async Task<LockResult> TryLockAllAsync(
            this ImmutableArray<IRedlockInstance> instances,
            ILogger logger,
            string resource,
            string nonce,
            TimeSpan lockTimeToLive,
            IReadOnlyDictionary<string, string>? metadata = null
        )
        {
            if (instances.IsDefaultOrEmpty)
            {
                return LockResult.Empty;
            }

            var lockResultBuilder = LockResultBuilder.Start(instances, lockTimeToLive);
            var tasks = instances.Select(
                async x => await x.TryLockSafeAsync(logger, resource, nonce, lockTimeToLive, metadata).ConfigureAwait(false) ? 1 : 0
            );
            return lockResultBuilder.End((await Task.WhenAll(tasks).ConfigureAwait(false)).Sum());
        }
        
        public static LockResult TryExtendAll(
            this ImmutableArray<IRedlockInstance> instances,
            ILogger logger,
            string resource,
            string nonce,
            TimeSpan lockTimeToLive
        )
        {
            if (instances.IsDefaultOrEmpty)
            {
                return LockResult.Empty;
            }
            
            var lockedCount = 0;
            var lockResultBuilder = LockResultBuilder.Start(instances, lockTimeToLive);
            Parallel.ForEach(instances, i =>
            {
                if (i.TryExtendSafe(logger, resource, nonce, lockTimeToLive))
                {
                    Interlocked.Increment(ref lockedCount);
                }
            });
            return lockResultBuilder.End(lockedCount);
        }

        public static async Task<LockResult> TryExtendAllAsync(
            this ImmutableArray<IRedlockInstance> instances,
            ILogger logger,
            string resource,
            string nonce,
            TimeSpan lockTimeToLive
        )
        {
            if (instances.IsDefaultOrEmpty)
            {
                return LockResult.Empty;
            }
            
            var lockResultBuilder = LockResultBuilder.Start(instances, lockTimeToLive);
            var tasks = instances.Select(
                async x => await x.TryExtendSafeAsync(logger, resource, nonce, lockTimeToLive).ConfigureAwait(false) ? 1 : 0
            );
            return lockResultBuilder.End((await Task.WhenAll(tasks).ConfigureAwait(false)).Sum());
        }

        private static bool TryLockSafe(
            this IRedlockInstance instance,
            ILogger logger,
            string resource,
            string nonce,
            TimeSpan lockTimeToLive,
            IReadOnlyDictionary<string, string>? metadata = null
        )
        {
            try
            {
                return instance.TryLock(resource, nonce, lockTimeToLive, metadata);
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
            TimeSpan lockTimeToLive,
            IReadOnlyDictionary<string, string>? metadata = null
        )
        {
            try
            {
                return await instance.TryLockAsync(resource, nonce, lockTimeToLive, metadata).ConfigureAwait(false);
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
        
        private static bool TryExtendSafe(
            this IRedlockInstance instance,
            ILogger logger,
            string resource,
            string nonce,
            TimeSpan lockTimeToLive
        )
        {
            try
            {
                return instance.TryExtend(resource, nonce, lockTimeToLive).IsSuccess();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Unable to extend lock ['{}'] = '{}' on [{}]", resource, nonce, instance);
                return false;
            }
        }

        private static async Task<bool> TryExtendSafeAsync(
            this IRedlockInstance instance,
            ILogger logger,
            string resource,
            string nonce,
            TimeSpan lockTimeToLive
        )
        {
            try
            {
                return (await instance.TryExtendAsync(resource, nonce, lockTimeToLive).ConfigureAwait(false)).IsSuccess();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Unable to extend lock ['{}'] = '{}' on [{}]", resource, nonce, instance);
                return false;
            }
        }
        
        private readonly struct LockResultBuilder
        {
            private readonly ImmutableArray<IRedlockInstance> _instances;
            private readonly TimeSpan _lockTimeToLive;
            private readonly long _startTimestamp;

            public LockResultBuilder(
                ImmutableArray<IRedlockInstance> instances,
                TimeSpan lockTimeToLive,
                long startTimestamp
            )
            {
                if(instances.Length == 0) throw new ArgumentOutOfRangeException(nameof(instances), "instances must not be empty");
                _instances = instances;
                _lockTimeToLive = lockTimeToLive;
                _startTimestamp = startTimestamp;
            }
            
            public static LockResultBuilder Start(ImmutableArray<IRedlockInstance> instances, TimeSpan lockTimeToLive)
            {
                return new LockResultBuilder(instances, lockTimeToLive, Stopwatch.GetTimestamp());
            }

            public LockResult End(int lockedCount)
            {
                var elapsed = TimestampHelper.ToTimeSpan(Stopwatch.GetTimestamp() - _startTimestamp);
                TimeSpan? minValidity = null;
                
                // ReSharper disable once ForCanBeConvertedToForeach
                for (var i = 0; i < _instances.Length; i++)
                {
                    var newValidity = _instances[i].MinValidity(_lockTimeToLive, elapsed);
                    
                    if (newValidity < (minValidity ??= newValidity))
                    {
                        minValidity = newValidity;
                    }
                }

                return new LockResult(lockedCount, minValidity ?? TimeSpan.Zero, elapsed, _instances.Length);
            }
        }
    }
}