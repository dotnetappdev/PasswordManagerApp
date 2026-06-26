using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace VaultGuard.Uno.Logging;

/// <summary>
/// Minimal durable file logger for the Uno (iOS/Android/desktop/WASM) head, which does not
/// reference VaultGuard.Services. Writes serial log files under the platform's local app-data
/// folder (<c>logs/{yyyy}/{MMMM}/{dd}.txt</c>) and falls back to <see cref="System.Diagnostics.Debug"/>
/// when the filesystem is unavailable (e.g. WASM).
///
/// <para>SECURITY: never pass passwords, vault contents, keys or tokens to this logger.</para>
/// </summary>
internal static class UnoLogger
{
    private static readonly object _lock = new object();

    public static void Info(string message, [CallerFilePath] string file = "") => Write("Information", Category(file), message, null);
    public static void Warning(string message, Exception? ex = null, [CallerFilePath] string file = "") => Write("Warning", Category(file), message, ex);
    public static void Error(string message, Exception? ex = null, [CallerFilePath] string file = "") => Write("Error", Category(file), message, ex);

    private static void Write(string level, string category, string message, Exception? ex)
    {
        try
        {
            var now = DateTime.Now;
            var baseDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "VaultGuard", "logs");
            var dir = Path.Combine(baseDir, now.Year.ToString(), now.ToString("MMMM"));
            Directory.CreateDirectory(dir);
            var filePath = Path.Combine(dir, now.ToString("dd") + ".txt");

            var sb = new StringBuilder();
            sb.Append(now.ToString("yyyy-MM-dd HH:mm:ss.fff"))
              .Append(" [").Append(level).Append("] ")
              .Append(category).Append(": ").Append(message);
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
        catch (Exception writeEx)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[UnoLogger] write failed: {writeEx.Message}");
                System.Diagnostics.Debug.WriteLine($"[UnoLogger] {level} {category}: {message}");
            }
            catch { }
        }
    }

    private static string Category(string file)
        => string.IsNullOrEmpty(file) ? "Uno" : Path.GetFileNameWithoutExtension(file);
}
