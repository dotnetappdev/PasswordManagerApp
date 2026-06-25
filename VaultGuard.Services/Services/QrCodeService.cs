using VaultGuard.Services.Interfaces;
using QRCoder;

namespace VaultGuard.Services.Services;

/// <summary>
/// QR code rendering backed by QRCoder's <see cref="PngByteQRCode"/>, which emits PNG bytes without
/// any System.Drawing dependency, so it runs on every platform/host.
/// </summary>
public sealed class QrCodeService : IQrCodeService
{
    public byte[] GeneratePng(string content, int pixelsPerModule = 8)
    {
        if (string.IsNullOrEmpty(content))
            return Array.Empty<byte>();

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data);
        return png.GetGraphic(Math.Clamp(pixelsPerModule, 1, 40));
    }

    public string GeneratePngDataUrl(string content, int pixelsPerModule = 8)
    {
        var bytes = GeneratePng(content, pixelsPerModule);
        return bytes.Length == 0
            ? string.Empty
            : "data:image/png;base64," + Convert.ToBase64String(bytes);
    }
}
