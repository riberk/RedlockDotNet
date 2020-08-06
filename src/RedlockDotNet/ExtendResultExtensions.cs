using System;

namespace RedlockDotNet
{
    /// <summary>Extensions on <see cref="ExtendResult"/></summary>
    public static class ExtendResultExtensions
    {
        /// <summary>The extension failed</summary>
        public static bool IsFail(this ExtendResult r) => r switch
        {
            ExtendResult.Extend => false,
            ExtendResult.Reacquire => false,
            ExtendResult.AlreadyAcquiredByAnotherOwner => true,
            ExtendResult.IllegalReacquire => true,
            _ => throw new ArgumentOutOfRangeException(nameof(r), r, null)
        };

        /// <summary>The extension success</summary>
        public static bool IsSuccess(this ExtendResult r) => !r.IsFail();

    }
}