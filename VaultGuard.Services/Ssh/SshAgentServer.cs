using System.Buffers.Binary;
using System.IO.Pipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VaultGuard.DAL;
using VaultGuard.Models;
using VaultGuard.Services.Interfaces;
using VaultGuard.Services.Security;

namespace VaultGuard.Services.Ssh;

/// <summary>Asks the user whether an app may use an SSH key (WPF dialog).</summary>
public interface ISshApprover
{
    Task<bool> ApproveAsync(string keyTitle, CancellationToken ct);
}

/// <summary>
/// An SSH agent that serves the SSH keys in the unlocked vault, so `ssh`, `git` and other SSH tools can use them
/// without the private key ever being written to disk (like 1Password's SSH agent).
///
/// - Windows: the named pipe OpenSSH looks for, \\.\pipe\openssh-ssh-agent. If Windows' own "OpenSSH Authentication
///   Agent" service already holds it, \\.\pipe\vaultguard-ssh-agent instead (point SSH_AUTH_SOCK at it, or stop
///   that service). macOS/Linux: a socket; point SSH_AUTH_SOCK at <see cref="SocketPath"/>.
/// - Keys: items of type SSH key whose "Private Key" field holds an unencrypted OpenSSH or PEM key (Ed25519, RSA,
///   ECDSA). Listed only while the vault is unlocked (and travel mode allows their vault).
/// - Every signature needs the user's approval; an approval covers that key for one minute (a git push signs more
///   than once). Pipe/socket open to the current OS user only.
/// Protocol: draft-miller-ssh-agent (REQUEST_IDENTITIES 11 → 12, SIGN_REQUEST 13 → 14, FAILURE 5).
/// </summary>
public sealed class SshAgentServer : IAsyncDisposable
{
    public const string WindowsPipe = "openssh-ssh-agent";
    public const string FallbackPipe = "vaultguard-ssh-agent";
    private const byte Failure = 5, RequestIdentities = 11, IdentitiesAnswer = 12, SignRequest = 13, SignResponse = 14;
    private static readonly TimeSpan ApprovalLifetime = TimeSpan.FromMinutes(1);

    private readonly IServiceProvider _services;
    private readonly ISshApprover _approver;
    private readonly CancellationTokenSource _cts = new();
    private readonly SemaphoreSlim _approvalGate = new(1, 1);
    private readonly Dictionary<string, DateTime> _approvedUntil = new();
    private Task? _loop;

    public SshAgentServer(IServiceProvider services, ISshApprover approver)
    {
        _services = services;
        _approver = approver;
    }

    /// <summary>The pipe name in use once started.</summary>
    public string? PipeName { get; private set; }

    /// <summary>What SSH_AUTH_SOCK should be for this agent.</summary>
    public string? SocketPath => PipeName == null ? null
        : OperatingSystem.IsWindows() ? $@"\\.\pipe\{PipeName}" : Path.Combine(Path.GetTempPath(), "CoreFxPipe_" + PipeName);

    /// <summary>Starts listening. <paramref name="pipeName"/> overrides the default (tests).</summary>
    public void Start(string? pipeName = null)
    {
        var candidates = pipeName != null ? new[] { pipeName } : OperatingSystem.IsWindows() ? new[] { WindowsPipe, FallbackPipe } : new[] { FallbackPipe };
        foreach (var name in candidates)
        {
            try
            {
                var first = Create(name);
                PipeName = name;
                _loop = Task.Run(() => AcceptLoop(first, name));
                return;
            }
            catch (IOException) { /* in use (e.g. the Windows OpenSSH agent service): try the next name */ }
            catch (UnauthorizedAccessException) { }
        }
        VaultGuard.Services.Logging.AppLogger.Warning("SSH agent: no pipe name was free");
    }

    private static NamedPipeServerStream Create(string name) => new(name, PipeDirection.InOut,
        NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

    private async Task AcceptLoop(NamedPipeServerStream first, string name)
    {
        var pipe = first;
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                await pipe.WaitForConnectionAsync(_cts.Token);
                var connected = pipe;
                _ = Task.Run(() => ServeAsync(connected));
                pipe = Create(name);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                VaultGuard.Services.Logging.AppLogger.Warning("SSH agent: accept failed", ex);
                try { await Task.Delay(500, _cts.Token); pipe = Create(name); } catch { break; }
            }
        }
        await pipe.DisposeAsync();
    }

    /// <summary>One client: requests until it disconnects.</summary>
    private async Task ServeAsync(NamedPipeServerStream pipe)
    {
        await using (pipe)
        {
            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    var request = await ReadMessageAsync(pipe, _cts.Token);
                    if (request == null) return;
                    var reply = await HandleAsync(request, _cts.Token);
                    await WriteMessageAsync(pipe, reply, _cts.Token);
                }
            }
            catch (Exception ex) when (ex is IOException or OperationCanceledException or FormatException) { }
        }
    }

    /// <summary>Handles one agent request (public for tests).</summary>
    public async Task<byte[]> HandleAsync(byte[] request, CancellationToken ct = default)
    {
        if (request.Length == 0) return new[] { Failure };
        try
        {
            switch (request[0])
            {
                case RequestIdentities:
                {
                    var keys = await LoadKeysAsync(ct);
                    using var ms = new MemoryStream();
                    ms.WriteByte(IdentitiesAnswer);
                    SshKey.WriteUInt32(ms, (uint)keys.Count);
                    foreach (var k in keys)
                    {
                        SshKey.WriteString(ms, k.PublicBlob);
                        SshKey.WriteString(ms, System.Text.Encoding.UTF8.GetBytes(k.Comment));
                    }
                    return ms.ToArray();
                }
                case SignRequest:
                {
                    var blob = SshKey.ReadBytes(request, 1, out var next);
                    var data = SshKey.ReadBytes(request, next, out next);
                    var flags = next + 4 <= request.Length ? BinaryPrimitives.ReadUInt32BigEndian(request.AsSpan(next, 4)) : 0u;
                    var key = (await LoadKeysAsync(ct)).FirstOrDefault(k => k.PublicBlob.AsSpan().SequenceEqual(blob));
                    if (key == null || !await ApprovedAsync(key, ct)) return new[] { Failure };
                    using var ms = new MemoryStream();
                    ms.WriteByte(SignResponse);
                    SshKey.WriteString(ms, key.Sign(data, flags));
                    return ms.ToArray();
                }
                default:
                    return new[] { Failure }; // adding/removing keys is done in the vault, not through the agent
            }
        }
        catch (Exception ex) when (ex is FormatException or InvalidOperationException)
        {
            return new[] { Failure };
        }
    }

    private async Task<bool> ApprovedAsync(SshKey key, CancellationToken ct)
    {
        var id = Convert.ToBase64String(key.PublicBlob);
        lock (_approvedUntil)
            if (_approvedUntil.TryGetValue(id, out var until) && until > DateTime.UtcNow) return true;
        await _approvalGate.WaitAsync(ct); // one prompt at a time
        try
        {
            lock (_approvedUntil)
                if (_approvedUntil.TryGetValue(id, out var until) && until > DateTime.UtcNow) return true;
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromMinutes(2));
            bool ok;
            try { ok = await _approver.ApproveAsync(key.Comment, timeout.Token); }
            catch (OperationCanceledException) { ok = false; }
            if (ok) lock (_approvedUntil) _approvedUntil[id] = DateTime.UtcNow + ApprovalLifetime;
            return ok;
        }
        finally { _approvalGate.Release(); }
    }

    /// <summary>The SSH keys in the unlocked vault (none while it's locked).</summary>
    private async Task<List<SshKey>> LoadKeysAsync(CancellationToken ct)
    {
        var sessions = _services.GetService<IVaultSessionService>();
        var sessionId = sessions?.GetActiveSessionId();
        var userId = sessionId == null ? null : sessions!.GetSessionUserId(sessionId);
        using var cipher = sessionId == null ? null : VaultCipher.ForSession(sessions!, sessionId);
        if (userId == null || cipher == null) return new();

        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VaultGuardDbContext>();
        var items = await TravelMode.Apply(db.PasswordItems.AsNoTracking(), db)
            .Where(p => p.UserId == userId && !p.IsDeleted && p.Type == ItemType.SshKey)
            .Include(p => p.CustomFields)
            .OrderBy(p => p.Title).ToListAsync(ct);
        var keys = new List<SshKey>();
        foreach (var item in items)
        {
            cipher.Open(item);
            var pem = item.CustomFields?.FirstOrDefault(f => string.Equals(f.Name, "Private Key", StringComparison.OrdinalIgnoreCase))?.Value;
            VaultItemSecrets.Forget(item);
            if (string.IsNullOrWhiteSpace(pem)) continue;
            var key = SshKey.Parse(pem, item.Title ?? "SSH key", out var error);
            if (key != null) keys.Add(key);
            else VaultGuard.Services.Logging.AppLogger.Warning($"SSH agent: skipped \"{item.Title}\": {error}");
        }
        return keys;
    }

    // ---- framing: uint32 length, then the message ----
    private static async Task<byte[]?> ReadMessageAsync(Stream s, CancellationToken ct)
    {
        var header = new byte[4];
        if (!await ReadExactlyAsync(s, header, ct)) return null;
        var len = BinaryPrimitives.ReadUInt32BigEndian(header);
        if (len == 0 || len > 256 * 1024) throw new FormatException("Bad message length.");
        var body = new byte[len];
        return await ReadExactlyAsync(s, body, ct) ? body : null;
    }

    private static async Task WriteMessageAsync(Stream s, byte[] body, CancellationToken ct)
    {
        var header = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(header, (uint)body.Length);
        await s.WriteAsync(header, ct);
        await s.WriteAsync(body, ct);
        await s.FlushAsync(ct);
    }

    private static async Task<bool> ReadExactlyAsync(Stream s, byte[] buf, CancellationToken ct)
    {
        var read = 0;
        while (read < buf.Length)
        {
            var n = await s.ReadAsync(buf.AsMemory(read), ct);
            if (n == 0) return false;
            read += n;
        }
        return true;
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        if (_loop != null) { try { await _loop; } catch { } }
        _cts.Dispose();
    }
}
