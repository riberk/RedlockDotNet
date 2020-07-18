using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace TestUtils
{
    public class MemoryLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, MemoryLogger> _loggers = new ConcurrentDictionary<string, MemoryLogger>();

        public Action<LogEvent>? OnLog { get; set; }

        public readonly ConcurrentQueue<LogEvent> Logs = new ConcurrentQueue<LogEvent>();

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, t => new MemoryLogger(this, t, le =>
            {
                OnLog?.Invoke(le);
            }));
        }

        public ILogger<T> CreateLogger<T>()
        {
            return new MemoryLogger<T>(this, le =>
            {
                OnLog?.Invoke(le);
            });
        }

        public MemoryLogger? GetLogger(string categoryName)
        {
            return _loggers.GetValueOrDefault(categoryName);
        }

        public void Dispose()
        {
        }

        public void ClearLogs()
        {
            Logs.Clear();
            foreach (var logger in _loggers.Values)
            {
                logger.Logs.Clear();
            }
        }

        public void WriteLogs(Action<string> w, LogLevel minLevel = LogLevel.Trace)
        {
            foreach (var log in Logs)
            {
                w(log.ToString());
            }
        }
    }
}
