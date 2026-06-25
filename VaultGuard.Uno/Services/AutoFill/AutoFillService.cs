namespace PasswordManager.Uno.Services.AutoFill;

/// <summary>
/// Platform-agnostic AutoFill service
/// </summary>
public class AutoFillService : IAutoFillService
{
    private readonly ILogger<AutoFillService> _logger;

#if __IOS__
    private readonly iOSAutoFillService _platformService;
#elif __ANDROID__
    private readonly AndroidAutoFillService _platformService;
#else
    private readonly object? _platformService = null;
#endif

    public AutoFillService(ILogger<AutoFillService> logger)
    {
        _logger = logger;

#if __IOS__
        _platformService = new iOSAutoFillService(logger);
#elif __ANDROID__
        _platformService = new AndroidAutoFillService(logger);
#endif
    }

    public async Task<bool> IsAutoFillAvailableAsync()
    {
#if __IOS__ || __ANDROID__
        return await _platformService!.IsAutoFillAvailableAsync();
#else
        return await Task.FromResult(false);
#endif
    }

    public async Task<bool> IsAutoFillEnabledAsync()
    {
#if __IOS__ || __ANDROID__
        return await _platformService!.IsAutoFillEnabledAsync();
#else
        return await Task.FromResult(false);
#endif
    }

    public async Task<bool> RequestEnableAutoFillAsync()
    {
#if __IOS__ || __ANDROID__
        return await _platformService!.RequestEnableAutoFillAsync();
#else
        return await Task.FromResult(false);
#endif
    }

    public async Task<List<AutoFillCredential>> GetCredentialsAsync()
    {
#if __IOS__ || __ANDROID__
        return await _platformService!.GetCredentialsAsync();
#else
        return new List<AutoFillCredential>();
#endif
    }

    public async Task SaveCredentialAsync(AutoFillCredential credential)
    {
#if __IOS__ || __ANDROID__
        await _platformService!.SaveCredentialAsync(credential);
#else
        await Task.CompletedTask;
#endif
    }

    public async Task DeleteCredentialAsync(string identifier)
    {
#if __IOS__ || __ANDROID__
        await _platformService!.DeleteCredentialAsync(identifier);
#else
        await Task.CompletedTask;
#endif
    }
}
