using VaultGuard.Services.Interfaces;
using VaultGuard.Services.Ssh;

namespace VaultGuard.Components.Shared.Services;

/// <summary>
/// Runs Vault Guard's SSH agent (VaultGuard.Services/Ssh/SshAgentServer) inside a Blazor host, as the WPF app does.
/// Registered only where it makes sense (MAUI on Windows, where ssh and git look for the "openssh-ssh-agent" pipe).
/// Each signing request is approved in the app: SshApprovalPrompt (in MainLayout) answers <see cref="ApprovalRequested"/>
/// with a dialog. With no prompt on screen to answer, the request is denied.
/// </summary>
public sealed class SshAgentHost : ISshApprover, IAsyncDisposable
{
    public const string EnabledKey = "SshAgent:Enabled";

    public sealed class Approval
    {
        public required string KeyTitle { get; init; }
        public TaskCompletionSource<bool> Answer { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private readonly IServiceProvider _services;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private SshAgentServer? _server;

    public SshAgentHost(IServiceProvider services) => _services = services;

    /// <summary>Raised on a background thread; the handler must complete <see cref="Approval.Answer"/>.</summary>
    public event Action<Approval>? ApprovalRequested;

    public bool IsRunning => _server?.PipeName != null;
    public string? SocketPath => _server?.SocketPath;

    public bool IsEnabled => (_services.GetService(typeof(IAppSettingsService)) as IAppSettingsService)?.GetBool(EnabledKey) == true;

    /// <summary>Saves the setting and starts or stops the agent to match.</summary>
    public async Task SetEnabledAsync(bool on)
    {
        if (_services.GetService(typeof(IAppSettingsService)) is IAppSettingsService st)
        {
            st.Set(EnabledKey, on);
            st.Save();
        }
        await ApplyAsync();
    }

    /// <summary>Starts or stops the agent to match the saved setting (call once at start-up).</summary>
    public async Task ApplyAsync()
    {
        await _gate.WaitAsync();
        try
        {
            if (IsEnabled && _server == null)
            {
                try
                {
                    _server = new SshAgentServer(_services, this);
                    _server.Start();
                    if (_server.PipeName == null) { await _server.DisposeAsync(); _server = null; }
                }
                catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error("SSH agent failed to start", ex); _server = null; }
            }
            else if (!IsEnabled && _server != null)
            {
                await _server.DisposeAsync();
                _server = null;
            }
        }
        finally { _gate.Release(); }
    }

    public async Task<bool> ApproveAsync(string keyTitle, CancellationToken ct)
    {
        var handler = ApprovalRequested;
        if (handler == null) return false;
        var approval = new Approval { KeyTitle = keyTitle };
        using var reg = ct.Register(() => approval.Answer.TrySetResult(false));
        try { handler(approval); }
        catch (Exception ex) { VaultGuard.Services.Logging.AppLogger.Error("SSH approval prompt failed", ex); return false; }
        return await approval.Answer.Task;
    }

    public async ValueTask DisposeAsync()
    {
        if (_server != null) await _server.DisposeAsync();
        _server = null;
    }
}
