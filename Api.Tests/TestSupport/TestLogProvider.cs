using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Api.Tests.TestSupport;

public sealed record CapturedLog(
    LogLevel Level,
    string Category,
    string Message,
    Exception? Exception);

public sealed class TestLogProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<CapturedLog>
        _entries = new();

    public IReadOnlyCollection<CapturedLog> Entries =>
        _entries.ToArray();

    public ILogger CreateLogger(
        string categoryName)
    {
        return new TestLogger(
            categoryName,
            _entries);
    }

    public void Clear()
    {
        while (_entries.TryDequeue(out _))
        {
        }
    }

    public void Dispose()
    {
    }

    private sealed class TestLogger : ILogger
    {
        private readonly string _category;
        private readonly ConcurrentQueue<CapturedLog>
            _entries;

        public TestLogger(
            string category,
            ConcurrentQueue<CapturedLog> entries)
        {
            _category = category;
            _entries = entries;
        }

        public IDisposable? BeginScope<TState>(
            TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(
            LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            _entries.Enqueue(
                new CapturedLog(
                    logLevel,
                    _category,
                    formatter(state, exception),
                    exception));
        }
    }
}
