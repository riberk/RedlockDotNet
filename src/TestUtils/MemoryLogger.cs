using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace TestUtils
{
    public class MemoryLogger<T> : MemoryLogger, ILogger<T>
    {
        public MemoryLogger([NotNull] MemoryLoggerProvider provider, [NotNull] Action<LogEvent> onLog) : base(provider, typeof(T).FullName, onLog)
        {
        }

        public MemoryLogger() : this(new MemoryLoggerProvider(), l => {})
        {
        }
    }

    public class MemoryLogger : ILogger, IDisposable
    {
        public readonly MemoryLoggerProvider Provider;

        public readonly string? CategoryName;

        public Action<LogEvent> OnLog { get; set; }

        public readonly ConcurrentQueue<LogEvent> Logs = new ConcurrentQueue<LogEvent>();

        public MemoryLogger([NotNull] MemoryLoggerProvider provider, string? categoryName, [NotNull] Action<LogEvent> onLog)
        {
            Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            CategoryName = categoryName;
            OnLog = onLog ?? throw new ArgumentNullException(nameof(onLog));
        }

        public MemoryLogger() : this(new MemoryLoggerProvider(), "UNNAMEDLOGGER", l => { })
        {
        }

        public IDisposable BeginScope<TState>(TState state) => this;

        public void Dispose() { }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter)
        {
            var logEvent = new LogEvent<TState>
            {
                Logger = CategoryName,
                DateTime = DateTime.UtcNow,
                EventId = eventId,
                Exception = exception,
                LogLevel = logLevel,
                Message = formatter(state, exception),
                State = state,
            };
            OnLog(logEvent);
            Logs.Enqueue(logEvent);
            Provider?.Logs.Enqueue(logEvent);
        }
    }
}
