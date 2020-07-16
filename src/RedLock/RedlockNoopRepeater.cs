namespace RedLock
{
    /// <summary>Noop repeater</summary>
    public class RedlockNoopRepeater : IRedlockRepeater
    {
        /// <summary>Singleton</summary>
        public static RedlockNoopRepeater Instance { get; } = new RedlockNoopRepeater();

        /// <inheritdoc />
        public bool Next() => false;
    }
}