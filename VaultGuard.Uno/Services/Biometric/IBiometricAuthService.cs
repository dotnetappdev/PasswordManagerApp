namespace VaultGuard.Uno.Services.Biometric;

/// <summary>
/// Interface for biometric authentication services
/// </summary>
public interface IBiometricAuthService
{
    /// <summary>
    /// Check if biometric authentication is available on this device
    /// </summary>
    Task<bool> IsBiometricAvailableAsync();

    /// <summary>
    /// Get the type of biometric authentication available (Face ID, Touch ID, Fingerprint, etc.)
    /// </summary>
    Task<string> GetBiometricTypeAsync();

    /// <summary>
    /// Authenticate using biometrics
    /// </summary>
    /// <param name="reason">Reason for authentication prompt</param>
    /// <returns>True if authentication succeeded</returns>
    Task<bool> AuthenticateAsync(string reason);

    /// <summary>
    /// Check if user has enabled biometric login for this app
    /// </summary>
    bool IsBiometricLoginEnabled();

    /// <summary>
    /// Enable biometric login and store credentials securely
    /// </summary>
    Task EnableBiometricLoginAsync(string email, string encryptedCredentials);

    /// <summary>
    /// Disable biometric login and remove stored credentials
    /// </summary>
    Task DisableBiometricLoginAsync();

    /// <summary>
    /// Get stored credentials after successful biometric authentication
    /// </summary>
    Task<(string email, string encryptedCredentials)?> GetStoredCredentialsAsync();
}
