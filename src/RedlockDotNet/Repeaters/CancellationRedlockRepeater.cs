using System.Threading;

namespace RedlockDotNet.Repeaters
{
    /// <summary>Repeater by cancellation token</summary>
    public readonly struct CancellationRedlockRepeater : IRedlockRepeater
    {
        private readonly CancellationToken _cancellationToken;

        /// <summary>Repeater by cancellation token</summary>
        public CancellationRedlockRepeater(
            CancellationToken cancellationToken
        )
        {
            _cancellationToken = cancellationToken;
        }

        /// <inheritdoc />
        public bool Next()
        {
            return !_cancellationToken.IsCancellationRequested;
        }
    }
}