using System;
using System.Threading;
using RedlockDotNet.Repeaters;
using Timer = System.Timers.Timer;

namespace RedlockDotNet
{
    public interface IRedlockAutoExpander : IDisposable
    {
        public int ExtendCount { get; }
        
        public int SuccessCount { get; }
        
        public int FailCount { get; }

        public bool? LastResult { get; }
        
        public DateTime ValidUntilUtc { get; }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RedlockSyncAutoExpander<T> : IRedlockAutoExpander
        where T: IRedlockRepeater
    {
        private readonly Redlock _redlock;
        private readonly bool _tryReacquire;
        private readonly Func<IRedlockAutoExpander, T> _createRepeater;
        private readonly int _maxWaitMs;
        private readonly Func<DateTime>? _utcNow;
        private readonly Timer _timer;

        public RedlockAutoExpander(
            in Redlock redlock,
            int interval,
            bool tryReacquire,
            Func<IRedlockAutoExpander, T> createRepeater,
            int maxWaitMs,
            Func<DateTime>? utcNow = null
        )
        {
            _redlock = redlock;
            _tryReacquire = tryReacquire;
            _createRepeater = createRepeater;
            _maxWaitMs = maxWaitMs;
            _utcNow = utcNow;
            _timer = new Timer(interval);
            _semaphore = new SemaphoreSlim(1);
            _validUntilUtc = redlock.ValidUntilUtc;
        }

        public void Start()
        {
            _timer.Elapsed += (sender, args) =>
            {
                _semaphore.Wait();
                try
                {
                    var newValidUntil = _redlock.TryExtend(_tryReacquire, _createRepeater(this), _maxWaitMs, _utcNow);
                    if (newValidUntil.HasValue)
                    {
                        Interlocked.Exchange(ref _validUntilUtcTicks, newValidUntil.Value.Ticks);
                        
                    }
                }
                finally
                {
                
                }

            }; 
            _timer.Enabled = true;
        }


        public void Dispose()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public int ExtendCount { get; private set; }
        
        /// <inheritdoc />
        public int SuccessCount { get; private set; }
        
        /// <inheritdoc />
        public int FailCount { get; private set; }

        /// <inheritdoc />
        public bool? LastResult { get; private set; }

        /// <inheritdoc />
        public DateTime ValidUntilUtc => new DateTime(_validUntilUtcTicks, DateTimeKind.Utc);

        private long _validUntilUtcTicks;
        private readonly SemaphoreSlim _semaphore;
    }
}