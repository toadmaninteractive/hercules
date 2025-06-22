using Hercules.Shortcuts;
using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Hercules
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        User,
    }

    public record LogEvent(LogLevel Level, DateTime Time, string Message, IShortcut? Shortcut = null, Exception? Exception = null)
    {
        public override string ToString()
        {
            if (Exception != null)
                return $"[{Time:yyyy-MM-dd HH:mm:ss}] {Message}";
            else
                return $"[{Time:yyyy-MM-dd HH:mm:ss}] {Message}";
        }

        public string Copy()
        {
            if (Exception != null)
                return $"[{Time:yyyy-MM-dd HH:mm:ss}] [{Level}] {Message}{Environment.NewLine}{Exception}";
            else
                return $"[{Time:yyyy-MM-dd HH:mm:ss}] [{Level}] {Message}";
        }
    }

    public static class Logger
    {
        private static readonly ISubject<LogEvent, LogEvent> subject = Subject.Synchronize(new ReplaySubject<LogEvent>());

        public static IObservable<LogEvent> Events => subject.AsObservable();

        public static void AddEvent(LogEvent e)
        {
            subject.OnNext(e);
        }

        public static void LogDebug(string message)
        {
            AddEvent(new LogEvent(LogLevel.Debug, DateTime.Now, message));
        }

        public static void LogUser(string message)
        {
            AddEvent(new LogEvent(LogLevel.User, DateTime.Now, message));
        }

        public static void Log(string message, IShortcut? shortcut = null)
        {
            AddEvent(new LogEvent(LogLevel.Info, DateTime.Now, message, shortcut));
        }

        public static void LogWarning(string message, IShortcut? shortcut = null)
        {
            AddEvent(new LogEvent(LogLevel.Warning, DateTime.Now, message, shortcut));
        }

        public static void LogError(string message, IShortcut? shortcut = null)
        {
            AddEvent(new LogEvent(LogLevel.Error, DateTime.Now, message, shortcut));
        }

        public static void LogException(Exception exception, IShortcut? shortcut = null)
        {
            AddEvent(new LogEvent(LogLevel.Error, DateTime.Now, exception.Message, shortcut, exception));
        }

        public static void LogException(string message, Exception exception, IShortcut? shortcut = null)
        {
            AddEvent(new LogEvent(LogLevel.Error, DateTime.Now, message + ": " + exception.Message, shortcut, exception));
        }
    }
}
