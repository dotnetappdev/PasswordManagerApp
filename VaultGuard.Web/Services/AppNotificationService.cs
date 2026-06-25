using System;
using System.Collections.Generic;
using MudBlazor;

namespace PasswordManager.Web.Services;

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

    public AppNotificationService(ISnackbar snackbar) => _snackbar = snackbar;

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
}
