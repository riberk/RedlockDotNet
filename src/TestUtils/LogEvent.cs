using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace TestUtils
{
    public class LogEvent
    {
        public string? Logger { get; set; }

        public DateTime DateTime { get; set; }

        public LogLevel LogLevel { get; set; }

        public EventId EventId { get; set; }

        public string? Message { get; set; }

        public Exception? Exception { get; set; }

        public override string ToString()
        {
            return Exception != null
                ? $"{DateTime:HH:mm:ss.fff} | {LogLevel} | {Logger} | {Message} | {Exception}"
                : $"{DateTime:HH:mm:ss.fff} | {LogLevel} | {Logger} | {Message}";
        }
    }

    public class LogEvent<TState> : LogEvent
    {
        [MaybeNull, AllowNull]
        public TState State { get; set; } = default!;
    }
}
