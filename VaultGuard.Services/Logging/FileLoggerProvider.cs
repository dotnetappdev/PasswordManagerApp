using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace VaultGuard.Services.Logging
{
    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly string _baseLogPath;
        private readonly LogLevel _minLevel;

        public FileLoggerProvider(string? baseLogPath = null, LogLevel minLevel = LogLevel.Information)
        {
            _baseLogPath = baseLogPath ?? ResolveDefaultLogPath();
            _minLevel = minLevel;
        }

        public static string ResolveDefaultLogPath()
        {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (dir != null)
            {
                if (dir.GetFiles("*.sln").Length > 0)
                {
                    return Path.Combine(dir.FullName, "logs");
                }
                dir = dir.Parent;
            }

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
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
