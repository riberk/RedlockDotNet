using System;

namespace RedlockDotNet
{
    /// <summary>
    /// Base exception for redlock alg implementation
    /// </summary>
    public class RedlockException : Exception
    {
        /// <inheritdoc />
        public RedlockException()
        {
        }

        /// <inheritdoc />
        public RedlockException(string message) : base(message)
        {
        }

        /// <inheritdoc />
        public RedlockException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}