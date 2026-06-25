using Microsoft.Extensions.Logging;
using PasswordManager.Services.Interfaces;

namespace PasswordManager.Services.Services;

/// <summary>
/// Service to detect platform type from user agent strings
/// </summary>
public class PlatformDetectionService : IPlatformDetectionService
{
    private readonly ILogger<PlatformDetectionService> _logger;

    public PlatformDetectionService(ILogger<PlatformDetectionService> logger)
    {
        _logger = logger;
    }

    public PlatformType DetectPlatform(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
        {
            return PlatformType.Unknown;
        }

        userAgent = userAgent.ToLowerInvariant();

        // Detect mobile platforms first
        if (userAgent.Contains("android"))
        {
            return PlatformType.MobileAndroid;
        }

        if (userAgent.Contains("iphone") || userAgent.Contains("ipad") || userAgent.Contains("ipod"))
        {
            return PlatformType.MobileIOS;
        }

        // Detect desktop platforms
        if (userAgent.Contains("windows"))
        {
            // Check if it's a MAUI app on Windows
            if (userAgent.Contains("passwordmanager") || userAgent.Contains("maui"))
            {
                return PlatformType.DesktopWindows;
            }
            // Otherwise it's web on Windows
            return PlatformType.Web;
        }

        if (userAgent.Contains("macintosh") || userAgent.Contains("mac os"))
        {
            // Check if it's a MAUI app on macOS
            if (userAgent.Contains("passwordmanager") || userAgent.Contains("maui"))
            {
                return PlatformType.DesktopMacOS;
            }
            // Otherwise it's web on macOS
            return PlatformType.Web;
        }

        if (userAgent.Contains("linux"))
        {
            // Check if it's a MAUI app on Linux
            if (userAgent.Contains("passwordmanager") || userAgent.Contains("maui"))
            {
                return PlatformType.DesktopLinux;
            }
            // Otherwise it's web on Linux
            return PlatformType.Web;
        }

        // Check for common web browsers
        if (userAgent.Contains("mozilla") || userAgent.Contains("chrome") || 
            userAgent.Contains("safari") || userAgent.Contains("firefox") || 
            userAgent.Contains("edge") || userAgent.Contains("opera"))
        {
            return PlatformType.Web;
        }

        _logger.LogWarning("Unable to detect platform from user agent: {UserAgent}", userAgent);
        return PlatformType.Unknown;
    }

    public bool IsOtpSupported(string? userAgent)
    {
        var platformType = DetectPlatform(userAgent);
        
        // OTP is supported on web, Android, and iOS platforms only
        // Desktop applications are excluded per the requirements
        return platformType switch
        {
            PlatformType.Web => true,
            PlatformType.MobileAndroid => true,
            PlatformType.MobileIOS => true,
            PlatformType.DesktopWindows => false,
            PlatformType.DesktopMacOS => false,
            PlatformType.DesktopLinux => false,
            PlatformType.Unknown => false,
            _ => false
        };
    }
}