using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PasswordManager.Services.Utilities;

public class FileLogger : IDisposable
{
    private static readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly string _logDirectory;
    private bool _disposed = false;

    public FileLogger()
    {
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        _logDirectory = Path.Combine(baseDirectory, "logs");
    }

    public async Task LogAsync(string category, string message, LogLevel level = LogLevel.Info)
    {
        if (_disposed) return;

        await _semaphore.WaitAsync();
        try
        {
            var now = DateTime.Now;
            var monthFolder = Path.Combine(_logDirectory, now.ToString("yyyy-MM"));
            var dayFile = Path.Combine(monthFolder, $"{now:yyyy-MM-dd}.log");

            // Ensure directory exists
            Directory.CreateDirectory(monthFolder);

            var logEntry = $"[{now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] [{category}] {message}{Environment.NewLine}";

            await File.AppendAllTextAsync(dayFile, logEntry, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            // Fallback to debug output if file logging fails
            System.Diagnostics.Debug.WriteLine($"FileLogger Error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Original log: [{category}] {message}");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task LogErrorAsync(string category, string message, Exception exception)
    {
        var errorDetails = new StringBuilder();
        errorDetails.AppendLine(message);
        errorDetails.AppendLine($"Exception Type: {exception.GetType().FullName}");
        errorDetails.AppendLine($"Exception Message: {exception.Message}");
        errorDetails.AppendLine($"Stack Trace: {exception.StackTrace}");

        if (exception.InnerException != null)
        {
            errorDetails.AppendLine($"Inner Exception: {exception.InnerException.GetType().FullName}");
            errorDetails.AppendLine($"Inner Message: {exception.InnerException.Message}");
            errorDetails.AppendLine($"Inner Stack Trace: {exception.InnerException.StackTrace}");
        }

        await LogAsync(category, errorDetails.ToString(), LogLevel.Error);
    }

    public async Task LogImportStartAsync(string provider, string fileName, long fileSize)
    {
        await LogAsync("Import", $"Starting import - Provider: {provider}, File: {fileName}, Size: {fileSize:N0} bytes");
    }

    public async Task LogImportProgressAsync(string provider, int itemsProcessed, int totalItems)
    {
        await LogAsync("Import", $"Progress - Provider: {provider}, Processed: {itemsProcessed}/{totalItems}");
    }

    public async Task LogImportCompleteAsync(string provider, int successful, int failed, int total, TimeSpan duration)
    {
        await LogAsync("Import", 
            $"Completed - Provider: {provider}, Success: {successful}, Failed: {failed}, Total: {total}, Duration: {duration.TotalSeconds:F2}s",
            failed > 0 ? LogLevel.Warning : LogLevel.Info);
    }

    public async Task LogProviderLoadAsync(string providerName, string version, bool success)
    {
        var status = success ? "Loaded successfully" : "Failed to load";
        await LogAsync("ProviderLoad", $"{providerName} v{version} - {status}", success ? LogLevel.Info : LogLevel.Error);
    }

    public void Dispose()
    {
        _disposed = true;
    }
}

public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error,
    Critical
}
