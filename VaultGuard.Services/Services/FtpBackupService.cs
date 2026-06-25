using System.Net;
using Microsoft.Extensions.Logging;
using VaultGuard.Models.DTOs;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.Services.Services;

/// <summary>
/// FTP/FTPS backup service — uploads backups to any NAS or remote FTP site the user points it at.
/// Combined with the existing Hourly/Daily/Weekly/Monthly backup schedule, this is the "save it off
/// to my NAS on a schedule" option that doesn't require a cloud vendor account.
///
/// Uses <see cref="FtpWebRequest"/> (obsolete but still fully functional for basic STOR/RETR/LIST/
/// DELE/MKD — there is no first-class FTP client in modern .NET, and pulling in a third-party
/// package for this would be overkill).
/// </summary>
#pragma warning disable CS0618 // FtpWebRequest is obsolete; see class remarks above.
public class FtpBackupService : IFtpBackupService
{
    private readonly ILogger<FtpBackupService> _logger;
    private FtpConnectionSettings _settings = new();

    public string ServiceName => "FTP";
    public long MaxBackupSizeBytes => 2L * 1024 * 1024 * 1024; // 2 GB — generous, most NAS/FTP sites can take far more

    public FtpBackupService(ILogger<FtpBackupService> logger)
    {
        _logger = logger;
    }

    public void SetConnectionSettings(FtpConnectionSettings settings) => _settings = settings ?? new();

    public FtpConnectionSettings GetConnectionSettings() => _settings;

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            await ListBackupsAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FTP connection test failed");
            return false;
        }
    }

    public async Task<bool> AuthenticateAsync() => await TestConnectionAsync();

    public async Task<bool> IsAuthenticatedAsync() =>
        !string.IsNullOrWhiteSpace(_settings.Host) && await TestConnectionAsync();

    public Task SignOutAsync()
    {
        _settings = new();
        return Task.CompletedTask;
    }

    public async Task<CloudBackupResult> UploadBackupAsync(byte[] backupData, string fileName, string? description = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_settings.Host))
                return Fail("FTP host is not configured.");
            if (backupData.Length > MaxBackupSizeBytes)
                return Fail($"Backup exceeds the {MaxBackupSizeBytes / (1024 * 1024)} MB limit.");

            await EnsureRemoteDirectoryAsync();

            var request = CreateRequest(fileName, WebRequestMethods.Ftp.UploadFile);
            using (var stream = await request.GetRequestStreamAsync())
                await stream.WriteAsync(backupData, 0, backupData.Length);

            using var response = (FtpWebResponse)await request.GetResponseAsync();

            return new CloudBackupResult
            {
                Success = true,
                BackupInfo = new CloudBackupInfo
                {
                    Id = fileName,
                    FileName = fileName,
                    Description = description,
                    CreatedAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow,
                    SizeInBytes = backupData.Length,
                    CloudPath = BuildRemotePath(fileName),
                    ServiceName = ServiceName,
                    Provider = CloudBackupProvider.Ftp,
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FTP upload failed for {FileName}", fileName);
            return Fail($"Upload failed: {ex.Message}");
        }
    }

    public async Task<CloudBackupResult> DownloadBackupAsync(string backupId)
    {
        try
        {
            var request = CreateRequest(backupId, WebRequestMethods.Ftp.DownloadFile);
            using var response = (FtpWebResponse)await request.GetResponseAsync();
            using var responseStream = response.GetResponseStream();
            using var memoryStream = new MemoryStream();
            await responseStream.CopyToAsync(memoryStream);

            return new CloudBackupResult { Success = true, BackupData = memoryStream.ToArray() };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FTP download failed for {BackupId}", backupId);
            return Fail($"Download failed: {ex.Message}");
        }
    }

    public async Task<List<CloudBackupInfo>> ListBackupsAsync()
    {
        try
        {
            var request = CreateRequest(string.Empty, WebRequestMethods.Ftp.ListDirectoryDetails);
            using var response = (FtpWebResponse)await request.GetResponseAsync();
            using var stream = response.GetResponseStream();
            using var reader = new StreamReader(stream);
            var listing = await reader.ReadToEndAsync();

            return ParseDirectoryListing(listing)
                .Where(f => f.Name.EndsWith(".pwmbackup", StringComparison.OrdinalIgnoreCase))
                .Select(f => new CloudBackupInfo
                {
                    Id = f.Name,
                    FileName = f.Name,
                    CreatedAt = f.Modified,
                    ModifiedAt = f.Modified,
                    SizeInBytes = f.Size,
                    CloudPath = BuildRemotePath(f.Name),
                    ServiceName = ServiceName,
                    Provider = CloudBackupProvider.Ftp,
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FTP list failed");
            return [];
        }
    }

    public async Task<bool> DeleteBackupAsync(string backupId)
    {
        try
        {
            var request = CreateRequest(backupId, WebRequestMethods.Ftp.DeleteFile);
            using var response = (FtpWebResponse)await request.GetResponseAsync();
            return response.StatusCode == FtpStatusCode.FileActionOK;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FTP delete failed for {BackupId}", backupId);
            return false;
        }
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private async Task EnsureRemoteDirectoryAsync()
    {
        try
        {
            var request = CreateRequest(string.Empty, WebRequestMethods.Ftp.MakeDirectory, rootOnly: true);
            using var response = (FtpWebResponse)await request.GetResponseAsync();
        }
        catch
        {
            // Directory already exists (most common case) or couldn't be created — either way,
            // the upload itself will surface a clear error if the path is genuinely unusable.
        }
    }

    private FtpWebRequest CreateRequest(string fileName, string method, bool rootOnly = false)
    {
        var path = rootOnly ? _settings.RemoteDirectory.Trim('/') : BuildRemotePath(fileName).TrimStart('/');
        var uri = new Uri($"ftp://{_settings.Host}:{_settings.Port}/{path}");

        var request = (FtpWebRequest)WebRequest.Create(uri);
        request.Method = method;
        request.Credentials = new NetworkCredential(_settings.Username, _settings.Password);
        request.EnableSsl = _settings.UseFtps;
        request.UsePassive = true;
        request.UseBinary = true;
        request.KeepAlive = false;
        return request;
    }

    private string BuildRemotePath(string fileName)
    {
        var dir = string.IsNullOrWhiteSpace(_settings.RemoteDirectory) ? "/" : _settings.RemoteDirectory.Trim();
        if (!dir.StartsWith('/')) dir = "/" + dir;
        if (!dir.EndsWith('/')) dir += "/";
        return string.IsNullOrEmpty(fileName) ? dir : dir + fileName;
    }

    private static CloudBackupResult Fail(string msg) => new() { Success = false, ErrorMessage = msg };

    private static List<(string Name, long Size, DateTime Modified)> ParseDirectoryListing(string listing)
    {
        // Classic Unix-style LIST output: "drwxr-xr-x 2 user group 4096 Jan 01 12:00 filename"
        var results = new List<(string, long, DateTime)>();
        foreach (var line in listing.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 9) continue;
            if (parts[0].StartsWith('d')) continue; // skip directories

            var name = string.Join(' ', parts.Skip(8));
            var size = long.TryParse(parts[4], out var sz) ? sz : 0;
            results.Add((name, size, DateTime.UtcNow));
        }
        return results;
    }
}
#pragma warning restore CS0618
