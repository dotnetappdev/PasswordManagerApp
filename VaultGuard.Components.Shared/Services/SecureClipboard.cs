using Microsoft.JSInterop;
using VaultGuard.Services.Interfaces;
using VaultGuard.Services.Security;

namespace VaultGuard.Components.Shared.Services;

/// <summary>
/// Copies vault values to the clipboard and wipes them after the user's "Clear clipboard after" setting
/// (Web and MAUI). Use this instead of calling navigator.clipboard.writeText directly.
/// </summary>
public sealed class SecureClipboard : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly IAppSettingsService? _settings;
    private IJSObjectReference? _module;

    public SecureClipboard(IJSRuntime js, IAppSettingsService? settings = null)
    {
        _js = js;
        _settings = settings;
    }

    public int ClearAfterSeconds => _settings != null
        ? VaultSecuritySettings.ClipboardClearSeconds(_settings)
        : VaultSecuritySettings.DefaultClipboardClearSeconds;

    internal async ValueTask<IJSObjectReference> ModuleAsync() =>
        _module ??= await _js.InvokeAsync<IJSObjectReference>("import", "./_content/VaultGuard.Components.Shared/js/vault-security.js");

    /// <summary>Copies <paramref name="text"/> and schedules the wipe. Throws if the browser refuses the copy.</summary>
    public async Task CopyAsync(string? text)
    {
        var module = await ModuleAsync();
        await module.InvokeAsync<bool>("copySecret", text ?? string.Empty, ClearAfterSeconds);
    }

    /// <summary>Wipes the clipboard now (on lock / sign-out).</summary>
    public async Task ClearNowAsync()
    {
        try { await (await ModuleAsync()).InvokeVoidAsync("clearNow"); }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or InvalidOperationException or TaskCanceledException) { }
    }

    public async ValueTask DisposeAsync()
    {
        if (_module == null) return;
        try { await _module.DisposeAsync(); } catch (JSDisconnectedException) { }
    }
}
