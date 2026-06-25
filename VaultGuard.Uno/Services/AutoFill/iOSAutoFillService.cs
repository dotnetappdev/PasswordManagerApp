#if __IOS__
using AuthenticationServices;
using Foundation;
using Security;

namespace VaultGuard.Uno.Services.AutoFill;

/// <summary>
/// iOS AutoFill credential provider service using Keychain
/// </summary>
public class iOSAutoFillService
{
    private readonly ILogger _logger;
    private const string ServiceName = "VaultGuard";

    public iOSAutoFillService(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<bool> IsAutoFillAvailableAsync()
    {
        // AutoFill is available on iOS 12+
        return UIKit.UIDevice.CurrentDevice.CheckSystemVersion(12, 0);
    }

    public async Task<bool> IsAutoFillEnabledAsync()
    {
        try
        {
            // Check if credentials are enabled
            var enabled = await ASCredentialIdentityStore.SharedStore.GetStateAsync();
            return enabled.Enabled;
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
            // Guide user to Settings to enable AutoFill
            var settingsUrl = new NSUrl("App-prefs:PASSWORDS");
            if (UIKit.UIApplication.SharedApplication.CanOpenUrl(settingsUrl))
            {
                await UIKit.UIApplication.SharedApplication.OpenUrlAsync(settingsUrl, new UIKit.UIApplicationOpenUrlOptions());
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting AutoFill enable");
            return false;
        }
    }

    public async Task<List<AutoFillCredential>> GetCredentialsAsync()
    {
        var credentials = new List<AutoFillCredential>();

        try
        {
            var query = new SecRecord(SecKind.InternetPassword)
            {
                Service = ServiceName
            };

            SecStatusCode resultCode;
            var results = SecKeyChain.QueryAsRecord(query, int.MaxValue, out resultCode);

            if (resultCode == SecStatusCode.Success && results != null)
            {
                foreach (var record in results)
                {
                    var credential = new AutoFillCredential
                    {
                        Identifier = record.Account ?? string.Empty,
                        ServiceName = record.Service ?? ServiceName,
                        Username = record.Account ?? string.Empty,
                        Password = record.ValueData != null ? NSString.FromData(record.ValueData, NSStringEncoding.UTF8) : string.Empty,
                        Website = record.Server,
                        CreatedAt = record.CreationDate?.ToDateTime() ?? DateTime.UtcNow,
                        LastModified = record.ModificationDate?.ToDateTime() ?? DateTime.UtcNow
                    };
                    credentials.Add(credential);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting credentials");
        }

        return credentials;
    }

    public async Task SaveCredentialAsync(AutoFillCredential credential)
    {
        try
        {
            var record = new SecRecord(SecKind.InternetPassword)
            {
                Service = ServiceName,
                Account = credential.Username,
                Server = credential.Website,
                Label = credential.ServiceName,
                ValueData = NSData.FromString(credential.Password, NSStringEncoding.UTF8),
                Accessible = SecAccessible.WhenUnlocked
            };

            var statusCode = SecKeyChain.Add(record);

            if (statusCode == SecStatusCode.DuplicateItem)
            {
                // Update existing
                var queryRecord = new SecRecord(SecKind.InternetPassword)
                {
                    Service = ServiceName,
                    Account = credential.Username
                };

                var updateRecord = new SecRecord(SecKind.InternetPassword)
                {
                    ValueData = NSData.FromString(credential.Password, NSStringEncoding.UTF8)
                };

                SecKeyChain.Update(queryRecord, updateRecord);
            }

            _logger.LogInformation("Credential saved successfully");
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
            var record = new SecRecord(SecKind.InternetPassword)
            {
                Service = ServiceName,
                Account = identifier
            };

            var statusCode = SecKeyChain.Remove(record);

            if (statusCode == SecStatusCode.Success)
            {
                _logger.LogInformation("Credential deleted successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting credential");
            throw;
        }
    }
}

// Extension to convert NSDate to DateTime
public static class NSDateExtensions
{
    public static DateTime ToDateTime(this NSDate date)
    {
        return (DateTime)date;
    }
}
#endif
