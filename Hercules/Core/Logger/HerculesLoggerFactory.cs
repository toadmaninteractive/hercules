using Microsoft.Extensions.Logging;
using System;

namespace Hercules
{
    public class HerculesLogger : ILogger
    {
        private readonly string categoryName;

        public HerculesLogger(string categoryName)
        {
            this.categoryName = categoryName;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel)
        {
            return true;
            // return logLevel > Microsoft.Extensions.Logging.LogLevel.Trace;
        }

        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            var herculesLogLevel = ConvertLogLevel(logLevel);
            var logEvent = new LogEvent(herculesLogLevel, DateTime.Now, message, Exception: exception);
            Logger.AddEvent(logEvent);
        }

        private static LogLevel ConvertLogLevel(Microsoft.Extensions.Logging.LogLevel logLevel)
        {
            return logLevel switch
            {
                Microsoft.Extensions.Logging.LogLevel.Trace => LogLevel.Debug,
                Microsoft.Extensions.Logging.LogLevel.Debug => LogLevel.Debug,
                Microsoft.Extensions.Logging.LogLevel.Information => LogLevel.Info,
                Microsoft.Extensions.Logging.LogLevel.Warning => LogLevel.Warning,
                Microsoft.Extensions.Logging.LogLevel.Error => LogLevel.Error,
                Microsoft.Extensions.Logging.LogLevel.Critical => LogLevel.Error,
                _ => LogLevel.Info,
            };
        }
    }

    public class HerculesLoggerFactory : ILoggerFactory
    {
        public void AddProvider(ILoggerProvider provider)
        {
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new HerculesLogger(categoryName);
        }

        public void Dispose()
        {
        }
    }
}
