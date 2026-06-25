using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ModernWpf.Controls;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.WPF.Helpers;

public static class TwoFactorDialogHelper
{
    public static async Task<bool> OpenManageDialogAsync(IServiceProvider serviceProvider, string? userId, string? userEmail)
    {
        var twoFactorService = serviceProvider.GetService<ITwoFactorService>();
        var qrCodeService = serviceProvider.GetService<IQrCodeService>();

        if (twoFactorService == null || qrCodeService == null || string.IsNullOrEmpty(userId))
        {
            var dialog = new ContentDialog
            {
                Title = "Two-Factor Authentication",
                Content = "Not signed in. Please sign in first.",
                CloseButtonText = "OK"
            };
            await dialog.ShowAsync();
            return false;
        }

        var twoFactorDialog = new VaultGuard.WPF.Dialogs.TwoFactorSetupDialog(twoFactorService, qrCodeService, userId, userEmail ?? "");
        await twoFactorDialog.ShowAsync();
        return true;
    }

    public static async Task<bool> GetTwoFactorEnabledAsync(IServiceProvider serviceProvider, string? userId)
    {
        var twoFactorService = serviceProvider.GetService<ITwoFactorService>();
        if (twoFactorService == null || string.IsNullOrEmpty(userId)) return false;
        var status = await twoFactorService.GetTwoFactorStatusAsync(userId);
        return status.IsEnabled;
    }
}
