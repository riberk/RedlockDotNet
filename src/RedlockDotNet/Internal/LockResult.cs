using System;

namespace RedlockDotNet.Internal
{
    internal readonly struct LockResult
    {
        private static readonly Func<DateTime> DefaultUtcNow = () => DateTime.UtcNow;
        public LockResult(int lockedCount, long startTimestamp, long endTimestamp)
        {
            LockedCount = lockedCount;
            StartTimestamp = startTimestamp;
            EndTimestamp = endTimestamp;
        }

        public int LockedCount { get; }
        public long StartTimestamp { get; }
        public long EndTimestamp { get; }

        public TimeSpan Elapsed => TimestampHelper.ToTimeSpan(StartTimestamp - EndTimestamp);

        public bool IsLocked(TimeSpan lockTimeToLive, IRedlockImplementation implementation, Func<DateTime>? utcNow, out DateTime validUntilUtc)
        {
            var quorum = implementation.Instances.Length / 2 + 1;
            var minValidity = implementation.MinValidity(lockTimeToLive, Elapsed);
            var res = LockedCount >= quorum && minValidity > TimeSpan.Zero;
            validUntilUtc = res ? (utcNow ?? DefaultUtcNow)() + minValidity : default;
            return res;
        }

    }
}