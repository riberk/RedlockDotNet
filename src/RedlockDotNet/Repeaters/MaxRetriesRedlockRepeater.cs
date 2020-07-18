namespace RedlockDotNet.Repeaters
{
    /// <summary>Max retries repeater</summary>
    public class MaxRetriesRedlockRepeater : IRedlockRepeater
    {
        private readonly int _maxRetryCount;
        private int _retryCount;

        /// <summary>Max retries repeater</summary>
        public MaxRetriesRedlockRepeater(int maxRetryCount)
        {
            _maxRetryCount = maxRetryCount;
            _retryCount = 0;
        }
        
        /// <inheritdoc />
        public bool Next()
        {
            return _retryCount++ < _maxRetryCount; 
        }
    }
}