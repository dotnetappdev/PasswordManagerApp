using Microsoft.JSInterop;
using MudBlazor;
using VaultGuard.Components.Shared.Components;
using VaultGuard.Services.Interfaces;

namespace VaultGuard.Components.Shared.Services;

/// <summary>
/// Blazor counterpart of the WPF SecurityGateHelper. Gates sensitive actions behind a step-up
/// authenticator/recovery-code prompt when the matching setting is on and the current user has
/// 2FA enabled. Toggle preferences are persisted in browser localStorage.
/// </summary>
public static class TwoFactorGate
{
    public enum GateAction
    {
        VaultDelete,
        ItemDelete
    }

    public const string RequireCodeOnVaultDeleteKey = "vg.requireCodeOnVaultDelete";
    public const string RequireCodeOnItemDeleteKey = "vg.requireCodeOnItemDelete";

    public static string KeyFor(GateAction action) => action switch
    {
        GateAction.VaultDelete => RequireCodeOnVaultDeleteKey,
        GateAction.ItemDelete => RequireCodeOnItemDeleteKey,
        _ => string.Empty
    };

    public static async Task<bool> GetToggleAsync(IJSRuntime js, GateAction action)
    {
        try
        {
            var raw = await js.InvokeAsync<string?>("localStorage.getItem", KeyFor(action));
            return raw == "true";
        }
        catch { return false; }
    }

    public static async Task SetToggleAsync(IJSRuntime js, GateAction action, bool value)
    {
        try { await js.InvokeVoidAsync("localStorage.setItem", KeyFor(action), value ? "true" : "false"); }
        catch { }
    }

    /// <summary>
    /// Returns true if the action may proceed. Only prompts when the toggle is on AND the current
    /// user has 2FA enabled; otherwise returns true without interrupting the user.
    /// </summary>
    public static async Task<bool> RequireCodeForActionAsync(
        IDialogService dialogService,
        ITwoFactorService twoFactorService,
        IAuthService authService,
        IJSRuntime js,
        GateAction action)
    {
        if (!await GetToggleAsync(js, action))
            return true;

        var userId = await authService.GetCurrentUserIdAsync();
        if (string.IsNullOrEmpty(userId))
            return true;

        var status = await twoFactorService.GetTwoFactorStatusAsync(userId);
        if (!status.IsEnabled)
            return true;

        var message = action == GateAction.VaultDelete
            ? "Enter the 6-digit code from your authenticator app to delete this vault."
            : "Enter the 6-digit code from your authenticator app to delete this item.";

        return await PromptAndVerifyAsync(dialogService, userId, "Confirm with authenticator", message);
    }

    /// <summary>Shows the code dialog and returns true only if the entered code verifies.</summary>
    public static async Task<bool> PromptAndVerifyAsync(IDialogService dialogService, string userId, string title, string message)
    {
        var parameters = new DialogParameters
        {
            ["UserId"] = userId,
            ["Message"] = message
        };
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.ExtraSmall, FullWidth = true };

        var dialog = await dialogService.ShowAsync<TwoFactorCodeDialog>(title, parameters, options);
        var result = await dialog.Result;
        return result is not null && !result.Canceled && result.Data is true;
    }
}
