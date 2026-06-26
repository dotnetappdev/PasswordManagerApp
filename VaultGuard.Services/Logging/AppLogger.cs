using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;

namespace VaultGuard.Services.Logging
{
    /// <summary>
    /// Centralized application logging facade used across every VaultGuard host
    /// (WPF, MAUI, Blazor/Web, Web API, WinUI and console tools).
    ///
    /// <para>
    /// It writes durable, serial log files laid out as <c>logs/{yyyy}/{MMMM}/{dd}.txt</c>
    /// so diagnostics survive process exit and are available even in Release builds where
    /// <see cref="System.Diagnostics.Debug"/> output is stripped. When a host wires up
    /// <see cref="Initialize(ILoggerFactory, string, LogLevel)"/> the same entries are also
    /// forwarded to the standard <see cref="ILogger"/> pipeline (console, Sentry, etc.).
    /// </para>
    ///
    /// <para>
    /// SECURITY — DO NOT log secrets. Never pass master passwords, vault item passwords,
    /// derived keys, private keys, TOTP secrets, recovery codes, session tokens, API keys
    /// or full PII to this logger. Describe the action instead of the value
    /// (e.g. "updated password entry {id}" rather than the password itself). Use
    /// <see cref="Redact(string)"/> for any value that may contain user data.
    /// </para>
    /// </summary>
    public static class AppLogger
    {
        private static readonly object _fileLock = new object();
        private static readonly ConcurrentDictionary<string, ILogger> _loggerCache = new();
        private static ILoggerFactory? _loggerFactory;
        private static string _baseLogPath = FileLoggerProvider.ResolveDefaultLogPath();
        private static LogLevel _minLevel = LogLevel.Debug;

        /// <summary>
        /// Wires the facade to a host's logging pipeline. Optional — file logging works
        /// without it. Call once during application startup.
        /// </summary>
        public static void Initialize(ILoggerFactory? loggerFactory, string? baseLogPath = null, LogLevel minLevel = LogLevel.Debug)
        {
            _loggerFactory = loggerFactory;
            if (!string.IsNullOrWhiteSpace(baseLogPath))
            {
                _baseLogPath = baseLogPath!;
            }
            _minLevel = minLevel;
            _loggerCache.Clear();
        }

        /// <summary>Absolute path of the directory log files are written to.</summary>
        public static string LogDirectory => _baseLogPath;

        public static void Debug(string message, [CallerFilePath] string file = "", [CallerMemberName] string member = "")
            => Write(LogLevel.Debug, Category(file), message, null, member);

        public static void Info(string message, [CallerFilePath] string file = "", [CallerMemberName] string member = "")
            => Write(LogLevel.Information, Category(file), message, null, member);

        public static void Warning(string message, Exception? exception = null, [CallerFilePath] string file = "", [CallerMemberName] string member = "")
            => Write(LogLevel.Warning, Category(file), message, exception, member);

        public static void Error(string message, Exception? exception = null, [CallerFilePath] string file = "", [CallerMemberName] string member = "")
            => Write(LogLevel.Error, Category(file), message, exception, member);

        public static void Critical(string message, Exception? exception = null, [CallerFilePath] string file = "", [CallerMemberName] string member = "")
            => Write(LogLevel.Critical, Category(file), message, exception, member);

        /// <summary>
        /// Masks a potentially-sensitive value so it can be referenced in a log line without
        /// exposing it. Keeps only a short prefix and the length, e.g. <c>"se***(11)"</c>.
        /// Prefer omitting the value entirely; use this only when a hint genuinely aids triage.
        /// </summary>
        public static string Redact(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "(empty)";
            }
            var visible = value.Length <= 2 ? 0 : 2;
            return $"{value.Substring(0, visible)}***({value.Length})";
        }

        private static void Write(LogLevel level, string category, string message, Exception? exception, string member)
        {
            if (level < _minLevel)
            {
                return;
            }

            var qualified = string.IsNullOrEmpty(member) ? category : $"{category}.{member}";

            // 1) Forward to the standard logging pipeline when a host has configured one.
            try
            {
                var factory = _loggerFactory;
                if (factory != null)
                {
                    var logger = _loggerCache.GetOrAdd(category, c => factory.CreateLogger(c));
                    if (exception != null)
                    {
                        logger.Log(level, exception, "{Member}: {Message}", member, message);
                    }
                    else
                    {
                        logger.Log(level, "{Member}: {Message}", member, message);
                    }
                }
            }
            catch
            {
                // Never let a misconfigured pipeline suppress the durable file log below.
            }

            // 2) Always write the durable serial file log.
            WriteFile(level, qualified, message, exception);
        }

        private static void WriteFile(LogLevel level, string qualifiedCategory, string message, Exception? exception)
        {
            try
            {
                var now = DateTime.Now;
                var dir = Path.Combine(_baseLogPath, now.Year.ToString(), now.ToString("MMMM"));
                Directory.CreateDirectory(dir);
                var filePath = Path.Combine(dir, now.ToString("dd") + ".txt");

                var sb = new StringBuilder();
                sb.Append(now.ToString("yyyy-MM-dd HH:mm:ss.fff"))
                  .Append(" [").Append(level).Append("] ")
                  .Append(qualifiedCategory).Append(": ").Append(message);
                if (exception != null)
                {
                    sb.AppendLine();
                    sb.Append(exception);
                }
                sb.AppendLine();

                lock (_fileLock)
                {
                    File.AppendAllText(filePath, sb.ToString());
                }
            }
            catch (Exception ex)
            {
                // Logging must never crash the app. As an absolute last resort, surface the
                // failure to the debugger/console without losing the original message.
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[AppLogger] file write failed: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"[AppLogger] dropped {level} {qualifiedCategory}: {message}");
                }
                catch
                {
                    // Give up silently — nothing more can be done safely here.
                }
            }
        }

        private static string Category(string file)
        {
            if (string.IsNullOrEmpty(file))
            {
                return "App";
            }
            return Path.GetFileNameWithoutExtension(file);
        }
    }
}
