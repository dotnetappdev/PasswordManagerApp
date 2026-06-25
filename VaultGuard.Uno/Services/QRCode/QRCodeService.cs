using ZXing;
using ZXing.Common;

namespace VaultGuard.Uno.Services.QRCode;

/// <summary>
/// QR code service implementation using ZXing
/// </summary>
public class QRCodeService : IQRCodeService
{
    private readonly ILogger<QRCodeService> _logger;

    public QRCodeService(ILogger<QRCodeService> logger)
    {
        _logger = logger;
    }

    public async Task<byte[]> GenerateQRCodeAsync(string text, int width = 300, int height = 300)
    {
        try
        {
            var writer = new BarcodeWriterPixelData
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new EncodingOptions
                {
                    Width = width,
                    Height = height,
                    Margin = 1
                }
            };

            var pixelData = writer.Write(text);
            
            // Convert pixel data to byte array
            var bytes = new byte[pixelData.Pixels.Length];
            Buffer.BlockCopy(pixelData.Pixels, 0, bytes, 0, pixelData.Pixels.Length);
            
            return bytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating QR code");
            throw;
        }
    }

    public async Task<string?> ScanQRCodeAsync()
    {
        try
        {
#if __IOS__ || __ANDROID__
            // Open camera for scanning
            // This would typically use a camera page/dialog
            // For now, return null as placeholder
            _logger.LogInformation("QR code scanning requested");
            return null;
#else
            return null;
#endif
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning QR code");
            return null;
        }
    }

    public async Task<bool> HasCameraPermissionAsync()
    {
        try
        {
#if __IOS__
            var status = await AVFoundation.AVCaptureDevice.RequestAccessForMediaTypeAsync(AVFoundation.AVMediaTypes.Video);
            return status;
#elif __ANDROID__
            var context = Android.App.Application.Context;
            var permission = Android.Content.PM.Permission.Granted;
            return AndroidX.Core.Content.ContextCompat.CheckSelfPermission(
                context, 
                Android.Manifest.Permission.Camera) == permission;
#else
            return false;
#endif
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking camera permission");
            return false;
        }
    }

    public async Task<bool> RequestCameraPermissionAsync()
    {
        try
        {
#if __IOS__
            return await AVFoundation.AVCaptureDevice.RequestAccessForMediaTypeAsync(AVFoundation.AVMediaTypes.Video);
#elif __ANDROID__
            var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
            if (activity != null)
            {
                AndroidX.Core.App.ActivityCompat.RequestPermissions(
                    activity,
                    new[] { Android.Manifest.Permission.Camera },
                    100);
                return true;
            }
            return false;
#else
            return false;
#endif
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting camera permission");
            return false;
        }
    }
}
