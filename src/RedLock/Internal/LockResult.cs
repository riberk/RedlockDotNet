using System;

namespace RedLock.Internal
{
    internal readonly struct LockResult
    {
        public LockResult(int lockedCount, TimeSpan elapsed)
        {
            LockedCount = lockedCount;
            Elapsed = elapsed;
        }

        public int LockedCount { get; }

        public TimeSpan Elapsed { get; }

        public bool IsLocked(TimeSpan lockTimeToLive, IRedlockImplementation implementation)
        {
            var quorum = implementation.Instances.Length / 2 + 1;
            var minValidity = implementation.MinValidity(lockTimeToLive, Elapsed);
            return LockedCount >= quorum && minValidity > TimeSpan.Zero;
        }

    }
}