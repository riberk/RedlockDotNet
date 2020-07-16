using System.Threading;
using System.Threading.Tasks;
using Redlock.Internal;

namespace Redlock.Repeaters
{
    /// <summary>Repeater for acquire lock (for wait lock)</summary>
    public interface IRedlockRepeater
    {
        /// <summary>Has next loop iteration</summary>
        bool Next();

        /// <summary>Wait time synchronously</summary>
        /// <param name="maxWaitMs">Max time to wait before next attempt</param>
        public void WaitRandom(int maxWaitMs) => Thread.Sleep(ThreadSafeRandom.Next(maxWaitMs));

        /// <summary>Wait time asynchronously</summary>
        /// <param name="maxWaitMs">Max time to wait before next attempt</param>
        /// <param name="cancellationToken"></param>
        public async ValueTask WaitRandomAsync(int maxWaitMs, CancellationToken cancellationToken = default) 
            => await Task.Delay(ThreadSafeRandom.Next(maxWaitMs), cancellationToken);
    }
}