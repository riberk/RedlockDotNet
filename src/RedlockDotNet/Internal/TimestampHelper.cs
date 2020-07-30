using System;
using System.Diagnostics;

namespace RedlockDotNet.Internal
{
    internal static class TimestampHelper
    {
        public static readonly double TimestampToTicks = TimeSpan.TicksPerSecond / (double) Stopwatch.Frequency;

        public static TimeSpan ToTimeSpan(long stopwatchTimestamp) 
            => new TimeSpan((long) (TimestampToTicks * stopwatchTimestamp));
    }
}