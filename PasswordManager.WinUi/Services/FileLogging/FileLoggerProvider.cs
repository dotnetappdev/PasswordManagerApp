using System;
using Microsoft.Extensions.Logging;

namespace PasswordManager.WinUi.Services.FileLogging
{
    internal class FileLoggerProvider : ILoggerProvider
    {
        private readonly string _baseLogPath;
        private readonly LogLevel _minLevel;

        public FileLoggerProvider(string baseLogPath, LogLevel minLevel = LogLevel.Information)
        {
            _baseLogPath = baseLogPath;
            _minLevel = minLevel;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new FileLogger(categoryName, _baseLogPath, _minLevel);
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
