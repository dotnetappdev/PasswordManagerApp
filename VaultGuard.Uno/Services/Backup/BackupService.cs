namespace VaultGuard.Uno.Services.Backup;

/// <summary>
/// Platform-agnostic backup service
/// </summary>
public class BackupService : IBackupService
{
    private readonly ILogger<BackupService> _logger;
    private readonly string _databasePath;

#if __IOS__
    private readonly iCloudBackupService _platformService;
#elif __ANDROID__
    private readonly GoogleDriveBackupService _platformService;
#else
    private readonly object? _platformService = null;
#endif

    public BackupService(ILogger<BackupService> logger, string databasePath)
    {
        _logger = logger;
        _databasePath = databasePath;

#if __IOS__
        _platformService = new iCloudBackupService(logger);
#elif __ANDROID__
        _platformService = new GoogleDriveBackupService(logger);
#endif
    }

    public async Task<bool> IsBackupAvailableAsync()
    {
#if __IOS__ || __ANDROID__
        return await _platformService!.IsBackupAvailableAsync();
#else
        return await Task.FromResult(false);
#endif
    }

    public async Task<bool> IsSignedInAsync()
    {
#if __IOS__ || __ANDROID__
        return await _platformService!.IsSignedInAsync();
#else
        return await Task.FromResult(false);
#endif
    }

    public async Task<BackupResult> BackupAsync()
    {
#if __IOS__ || __ANDROID__
        try
        {
            return await _platformService!.BackupAsync(_databasePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup failed");
            return new BackupResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
#else
        return await Task.FromResult(new BackupResult
        {
            Success = false,
            ErrorMessage = "Backup not supported on this platform"
        });
#endif
    }

    public async Task<BackupResult> RestoreAsync()
    {
#if __IOS__ || __ANDROID__
        try
        {
            return await _platformService!.RestoreAsync(_databasePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Restore failed");
            return new BackupResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
#else
        return await Task.FromResult(new BackupResult
        {
            Success = false,
            ErrorMessage = "Restore not supported on this platform"
        });
#endif
    }

    public async Task<DateTime?> GetLastBackupDateAsync()
    {
#if __IOS__ || __ANDROID__
        return await _platformService!.GetLastBackupDateAsync();
#else
        return null;
#endif
    }

    public async Task EnableAutoBackupAsync()
    {
#if __IOS__ || __ANDROID__
        await _platformService!.EnableAutoBackupAsync();
#else
        await Task.CompletedTask;
#endif
    }

    public async Task DisableAutoBackupAsync()
    {
#if __IOS__ || __ANDROID__
        await _platformService!.DisableAutoBackupAsync();
#else
        await Task.CompletedTask;
#endif
    }

    public bool IsAutoBackupEnabled()
    {
#if __IOS__ || __ANDROID__
        return _platformService!.IsAutoBackupEnabled();
#else
        return false;
#endif
    }
}
