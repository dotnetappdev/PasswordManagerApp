namespace VaultGuard.Models.DTOs;

/// <summary>
/// Connection details for a NAS / network-drive backup target.
///
/// This is the dedicated "back up to my NAS or a mapped drive" option, kept separate from the
/// FTP/FTPS provider. The target can be a UNC path (\\server\share\folder) or a mapped drive
/// (Z:\backups). When <see cref="RequireAuthentication"/> is set, the supplied credentials are
/// used to connect to the share (via WNetAddConnection2 on Windows) before reading/writing.
/// </summary>
public class NetworkConnectionSettings
{
    /// <summary>UNC path (\\server\share) or mapped drive root (Z:\) to store backups under.</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>When true, connect to the share with the username/password below before access.</summary>
    public bool RequireAuthentication { get; set; }

    /// <summary>Domain-qualified or share-local username, e.g. "DOMAIN\\user" or "nasuser".</summary>
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}

/// <summary>A network drive currently mapped on this machine, for the backup-target picker.</summary>
public record MappedDriveInfo
{
    /// <summary>Drive letter with separator, e.g. "Z:\".</summary>
    public string Root { get; init; } = string.Empty;

    /// <summary>The UNC path the drive is mapped to, e.g. "\\nas\backups". Empty if unknown.</summary>
    public string UncPath { get; init; } = string.Empty;

    /// <summary>Friendly text for a dropdown, e.g. "Z:\  →  \\nas\backups".</summary>
    public string DisplayName => string.IsNullOrEmpty(UncPath) ? Root : $"{Root}  →  {UncPath}";
}
