namespace VaultGuard.Services.Interfaces;

/// <summary>
/// FTP/FTPS backup service interface — lets users point backups at a NAS or any remote FTP site.
/// Combined with the existing backup-schedule settings (Hourly/Daily/Weekly/Monthly), this gives
/// scheduled off-site backups without needing a specific cloud vendor account.
/// </summary>
public interface IFtpBackupService : ICloudBackupService
{
    /// <summary>Stores the FTP connection details used by every subsequent call.</summary>
    void SetConnectionSettings(FtpConnectionSettings settings);

    /// <summary>Returns the currently configured connection details (password included).</summary>
    FtpConnectionSettings GetConnectionSettings();

    /// <summary>Attempts to connect and list the remote directory — does not upload anything.</summary>
    Task<bool> TestConnectionAsync();
}

public class FtpConnectionSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 21;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string RemoteDirectory { get; set; } = "/";
    public bool UseFtps { get; set; } = true; // FTP over explicit TLS, on by default
}
