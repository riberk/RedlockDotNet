namespace Redlock.Repeaters
{
    /// <summary>Noop repeater</summary>
    public class NoopRedlockRepeater : IRedlockRepeater
    {
        /// <summary>Singleton</summary>
        public static NoopRedlockRepeater Instance { get; } = new NoopRedlockRepeater();

        /// <inheritdoc />
        public bool Next() => false;
    }
}