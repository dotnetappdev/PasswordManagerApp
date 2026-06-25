namespace VaultGuard.Services.Interfaces;

/// <summary>
/// Renders arbitrary text (e.g. an <c>otpauth://</c> URI) to a QR code PNG so it can be scanned
/// by Google Authenticator, Microsoft Authenticator, Authy, etc. Cross-platform (no System.Drawing),
/// shared by WPF and Blazor.
/// </summary>
public interface IQrCodeService
{
    /// <summary>Returns the QR code as PNG bytes.</summary>
    byte[] GeneratePng(string content, int pixelsPerModule = 8);

    /// <summary>Returns the QR code as a <c>data:image/png;base64,...</c> URL for &lt;img&gt; / ImageBrush.</summary>
    string GeneratePngDataUrl(string content, int pixelsPerModule = 8);
}
