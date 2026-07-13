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

            // Create the logs folder under the app directory up front so it exists on first boot. Wrapped so
            // a read-only/denied location can never crash application startup (logging is best-effort).
            try
            {
                Directory.CreateDirectory(_baseLogPath);
            }
            catch
            {
                // Best-effort: if we can't create it here, FileLogger will retry per-write and swallow errors.
            }
        }

        public static string ResolveDefaultLogPath()
        {
            // Dev convenience: place logs in the repo root (next to the .sln) by walking up from the app
            // folder. On locked-down hosts (e.g. SmarterASP, where the app lives under h:\root\home\...),
            // a parent directory may not be enumerable — treat that (or any probe failure) as "stop here"
            // and fall back to the app-local logs folder, which is always writable (web.config writes its
            // stdout log there too). Never let log-path discovery crash application startup.
            try
            {
                var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
                while (dir != null)
                {
                    FileInfo[] solutions;
                    try
                    {
                        solutions = dir.GetFiles("*.sln");
                    }
                    catch (UnauthorizedAccessException)
                    {
                        break; // Not allowed to enumerate this parent — use the app-local fallback.
                    }

                    if (solutions.Length > 0)
                    {
                        return Path.Combine(dir.FullName, "logs");
                    }
                    dir = dir.Parent;
                }
            }
            catch
            {
                // Any other filesystem issue while probing — fall back to the app-local logs folder.
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
