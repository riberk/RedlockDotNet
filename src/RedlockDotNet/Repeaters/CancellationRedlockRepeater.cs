using System;
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
        public bool Next() => !_cancellationToken.IsCancellationRequested;

        /// <inheritdoc />
        public Exception CreateException(string resource, string nonce, int attemptCount)
        {
            return new OperationCanceledException(
                $"Unable to obtain lock to ['{resource}'] = '{nonce}' on {attemptCount} attempts - operation canceled",
                _cancellationToken
            );
        }
    }
}