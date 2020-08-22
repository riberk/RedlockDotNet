using System;

namespace RedlockDotNet.Internal
{
    internal readonly struct LockResult
    {
        private readonly int _quorum;
        public static LockResult Empty => new LockResult(0, TimeSpan.Zero, TimeSpan.Zero, 0);
        
        private static readonly Func<DateTime> DefaultUtcNow = () => DateTime.UtcNow;
        public LockResult(int lockedCount, TimeSpan minValidity, TimeSpan elapsed, int instanceCount)
        {
            LockedCount = lockedCount;
            MinValidity = minValidity;
            Elapsed = elapsed;
            _quorum = instanceCount / 2 + 1;
        }

        public int LockedCount { get; }
        public TimeSpan MinValidity { get; }
        public TimeSpan Elapsed { get; }

        public bool IsLocked(Func<DateTime>? utcNow, out DateTime validUntilUtc)
        {
            var res = LockedCount >= _quorum && MinValidity > TimeSpan.Zero;
            validUntilUtc = res ? (utcNow ?? DefaultUtcNow)() + MinValidity : default;
            return res;
        }

    }
}