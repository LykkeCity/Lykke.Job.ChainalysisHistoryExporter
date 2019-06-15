using System;
using Microsoft.Extensions.Logging;

namespace Tests
{
    public class DummyLogger<T> : ILogger<T>, IDisposable
    {
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return new DummyLogger<T>();
        }

        public void Dispose()
        {
        }
    }
}