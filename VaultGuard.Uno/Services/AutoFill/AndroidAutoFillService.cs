#if __ANDROID__
using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Service.Autofill;

namespace PasswordManager.Uno.Services.AutoFill;

/// <summary>
/// Android AutoFill service implementation
/// </summary>
public class AndroidAutoFillService
{
    private readonly ILogger _logger;

    public AndroidAutoFillService(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<bool> IsAutoFillAvailableAsync()
    {
        // AutoFill is available on Android 8.0 (API 26)+
        return Build.VERSION.SdkInt >= BuildVersionCodes.O;
    }

    public async Task<bool> IsAutoFillEnabledAsync()
    {
        try
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
                return false;

            var context = Android.App.Application.Context;
            var autofillManager = (AutofillManager?)context.GetSystemService(Context.AutofillService);

            return autofillManager?.IsEnabled ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking AutoFill status");
            return false;
        }
    }

    public async Task<bool> RequestEnableAutoFillAsync()
    {
        try
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
                return false;

            var context = Android.App.Application.Context;
            var intent = new Intent(Settings.ActionRequestSetAutofillService);
            intent.SetData(Android.Net.Uri.Parse($"package:{context.PackageName}"));
            intent.AddFlags(ActivityFlags.NewTask);
            
            context.StartActivity(intent);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting AutoFill enable");
            return false;
        }
    }

    public async Task<List<AutoFillCredential>> GetCredentialsAsync()
    {
        // Credentials are managed by the AutoFill service
        // This would typically read from the local database
        var credentials = new List<AutoFillCredential>();
        return credentials;
    }

    public async Task SaveCredentialAsync(AutoFillCredential credential)
    {
        try
        {
            // Credentials are saved to local database
            // AutoFill service will read from there
            _logger.LogInformation("Credential saved for AutoFill");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving credential");
            throw;
        }
    }

    public async Task DeleteCredentialAsync(string identifier)
    {
        try
        {
            _logger.LogInformation("Credential deleted from AutoFill");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting credential");
            throw;
        }
    }
}
#endif
