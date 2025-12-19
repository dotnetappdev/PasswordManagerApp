namespace PasswordManager.Uno.Services.QRCode;

/// <summary>
/// Interface for QR code generation and scanning
/// </summary>
public interface IQRCodeService
{
    /// <summary>
    /// Generate QR code image from text
    /// </summary>
    Task<byte[]> GenerateQRCodeAsync(string text, int width = 300, int height = 300);

    /// <summary>
    /// Scan QR code from camera
    /// </summary>
    Task<string?> ScanQRCodeAsync();

    /// <summary>
    /// Check if camera permission is granted
    /// </summary>
    Task<bool> HasCameraPermissionAsync();

    /// <summary>
    /// Request camera permission
    /// </summary>
    Task<bool> RequestCameraPermissionAsync();
}
