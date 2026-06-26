using System;
using System.IO;
using System.Text;

namespace VaultGuard.BrowserExtension.NativeHost;

/// <summary>
/// Minimal durable file logger for the native messaging host.
///
/// <para>
/// The host cannot reference VaultGuard.Services (it is a standalone, dependency-light exe)
/// and it must never write diagnostics to <c>stdout</c> — that channel carries the binary
/// native-messaging protocol to the browser. This logger writes serial log files under
/// <c>%LOCALAPPDATA%/VaultGuard/logs/{yyyy}/{MMMM}/{dd}.txt</c> instead.
/// </para>
///
/// <para>
/// SECURITY: this process handles master keys and decrypted secrets directly. Never pass a
/// password, decrypted field, private key, master key or token to this logger. Log the
/// operation and the failure type only.
/// </para>
/// </summary>
internal static class NativeHostLogger
{
    private static readonly object _lock = new object();
    private static readonly string _baseLogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "VaultGuard", "logs");

    public static void Info(string message) => Write("Information", message, null);
    public static void Warning(string message, Exception? ex = null) => Write("Warning", message, ex);
    public static void Error(string message, Exception? ex = null) => Write("Error", message, ex);

    private static void Write(string level, string message, Exception? ex)
    {
        try
        {
            var now = DateTime.Now;
            var dir = Path.Combine(_baseLogPath, now.Year.ToString(), now.ToString("MMMM"));
            Directory.CreateDirectory(dir);
            var filePath = Path.Combine(dir, now.ToString("dd") + ".txt");

            var sb = new StringBuilder();
            sb.Append(now.ToString("yyyy-MM-dd HH:mm:ss.fff"))
              .Append(" [").Append(level).Append("] NativeHost: ").Append(message);
            if (ex != null)
            {
                sb.AppendLine();
                sb.Append(ex);
            }
            sb.AppendLine();

            lock (_lock)
            {
                File.AppendAllText(filePath, sb.ToString());
            }
        }
        catch
        {
            // Last resort only: stderr is safe (stdout is the protocol channel); never crash on logging.
            try { Console.Error.WriteLine($"[NativeHostLogger] {level}: {message}"); } catch { }
        }
    }
}
