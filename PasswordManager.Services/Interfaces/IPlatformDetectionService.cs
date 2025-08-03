namespace PasswordManager.Services.Interfaces;

/// <summary>
/// Interface for platform detection services
/// </summary>
public interface IPlatformDetectionService
{
    /// <summary>
    /// Detect the current platform type
    /// </summary>
    /// <param name="userAgent">User agent string from HTTP request</param>
    /// <returns>Detected platform type</returns>
    PlatformType DetectPlatform(string? userAgent);
    
    /// <summary>
    /// Check if OTP is supported on the current platform
    /// </summary>
    /// <param name="userAgent">User agent string from HTTP request</param>
    /// <returns>True if OTP is supported on this platform</returns>
    bool IsOtpSupported(string? userAgent);
}

/// <summary>
/// Platform types
/// </summary>
public enum PlatformType
{
    Unknown = 0,
    Web = 1,
    MobileAndroid = 2,
    MobileIOS = 3,
    DesktopWindows = 4,
    DesktopMacOS = 5,
    DesktopLinux = 6
}