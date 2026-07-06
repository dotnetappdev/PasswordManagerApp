using System;
using System.Collections.Generic;
using MudBlazor;

namespace VaultGuard.Web.Services;

public enum AppNotificationType { Success, Error, Warning, Info }

public sealed record AppNotification(
    string Message,
    string? Title = null,
    AppNotificationType Type = AppNotificationType.Info,
    int DurationMs = 4000);

/// <summary>
/// Thin wrapper around MudBlazor ISnackbar that enforces consistent styling
/// and allows any component to show toasts without injecting ISnackbar directly.
/// </summary>
public sealed class AppNotificationService
{
    private readonly ISnackbar _snackbar;
    private readonly IDialogService _dialogService;

    public AppNotificationService(ISnackbar snackbar, IDialogService dialogService)
    {
        _snackbar = snackbar;
        _dialogService = dialogService;
    }

    public void Success(string message, string? title = null) => Show(message, title, AppNotificationType.Success);
    public void Error  (string message, string? title = null) => Show(message, title, AppNotificationType.Error);
    public void Warning(string message, string? title = null) => Show(message, title, AppNotificationType.Warning);
    public void Info   (string message, string? title = null) => Show(message, title, AppNotificationType.Info);

    public void Show(string message, string? title, AppNotificationType type, int durationMs = 4000)
    {
        var severity = type switch
        {
            AppNotificationType.Success => Severity.Success,
            AppNotificationType.Error   => Severity.Error,
            AppNotificationType.Warning => Severity.Warning,
            _                           => Severity.Info
        };

        var display = string.IsNullOrWhiteSpace(title) ? message : $"**{title}** — {message}";

        _snackbar.Add(display, severity, config =>
        {
            config.VisibleStateDuration = durationMs;
            config.HideTransitionDuration = 400;
            config.ShowTransitionDuration = 300;
            config.ShowCloseIcon = true;
            config.SnackbarVariant = Variant.Filled;
        });
    }

    // ── Modal message / exception dialogs (WPF ExceptionDialog parity) ────────────
    /// <summary>Shows a modal error dialog with an optional expandable exception detail + copy button.</summary>
    public Task ShowErrorAsync(string message, Exception? exception = null, string? title = null)
        => ShowDialogAsync(message, exception?.ToString(), AppNotificationType.Error, title ?? "Something went wrong");

    public Task ShowWarningAsync(string message, string? details = null, string? title = null)
        => ShowDialogAsync(message, details, AppNotificationType.Warning, title ?? "Warning");

    public Task ShowInfoAsync(string message, string? details = null, string? title = null)
        => ShowDialogAsync(message, details, AppNotificationType.Info, title ?? "Information");

    public Task ShowSuccessAsync(string message, string? details = null, string? title = null)
        => ShowDialogAsync(message, details, AppNotificationType.Success, title ?? "Success");

    private async Task ShowDialogAsync(string message, string? details, AppNotificationType type, string title)
    {
        var parameters = new DialogParameters
        {
            { "Message", message },
            { "Details", details },
            { "Type", type },
        };
        var options = new DialogOptions
        {
            CloseButton = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
            BackdropClick = false,
        };
        var dialog = await _dialogService.ShowAsync<VaultGuard.Web.Components.Shared.AppMessageDialog>(
            title, parameters, options);
        await dialog.Result;
    }
}
