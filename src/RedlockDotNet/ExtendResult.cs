namespace RedlockDotNet
{
    /// <summary>
    /// Result of <see cref="IRedlockInstance.TryExtend"/> operation
    /// </summary>
    public enum ExtendResult
    {
        /// <summary>
        /// Lock extended
        /// </summary>
        Extend = 1,
        
        /// <summary>
        /// The lock was lost and re-acquired
        /// </summary>
        Reacquire = 2,
        
        /// <summary>
        /// The lock could not be extended because it was acquired by another owner
        /// </summary>
        AlreadyAcquiredByAnotherOwner = -1,
        
        /// <summary>
        /// The lock could not be extended it has not been taken 
        /// </summary>
        IllegalReacquire = -2,
    }
}