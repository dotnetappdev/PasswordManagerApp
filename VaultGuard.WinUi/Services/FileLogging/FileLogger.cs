using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace PasswordManager.WinUi.Services.FileLogging
{
    internal class FileLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly string _baseLogPath;
        private readonly LogLevel _minLevel;
        private static readonly object _lock = new object();

        public FileLogger(string categoryName, string baseLogPath, LogLevel minLevel = LogLevel.Information)
        {
            _categoryName = categoryName;
            _baseLogPath = baseLogPath;
            _minLevel = minLevel;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NoOpDisposable.Instance;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLevel;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            try
            {
                var now = DateTime.Now;
                var year = now.Year.ToString();
                var month = now.ToString("MMMM"); // e.g., July
                var dayFile = now.ToString("dd") + ".txt"; // e.g., 29.txt

                var dir = Path.Combine(_baseLogPath, year, month);
                Directory.CreateDirectory(dir);

                var filePath = Path.Combine(dir, dayFile);

                var message = formatter(state, exception);
                var logLine = $"{now:yyyy-MM-dd HH:mm:ss.fff} [{logLevel}] {_categoryName}: {message}";
                if (exception != null)
                {
                    logLine += Environment.NewLine + exception.ToString();
                }

                lock (_lock)
                {
                    File.AppendAllText(filePath, logLine + Environment.NewLine);
                }
            }
            catch
            {
                // Swallow logging exceptions - don't crash the app for logging errors
            }
        }

        private sealed class NoOpDisposable : IDisposable
        {
            public static readonly NoOpDisposable Instance = new();
            private NoOpDisposable() { }
            public void Dispose() { }
        }
    }
}
