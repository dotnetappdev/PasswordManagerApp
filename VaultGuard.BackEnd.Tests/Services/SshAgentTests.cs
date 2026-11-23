using System.Diagnostics;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using VaultGuard.Crypto.Extensions;
using VaultGuard.Crypto.Interfaces;
using VaultGuard.DAL;
using VaultGuard.Models;
using VaultGuard.Services.Interfaces;
using VaultGuard.Services.Security;
using VaultGuard.Services.Services;
using VaultGuard.Services.Ssh;

namespace VaultGuard.BackEnd.Tests.Services;

/// <summary>
/// The SSH agent against the real OpenSSH tools: ssh-add lists the vault's keys, and ssh-keygen signs through the
/// agent and verifies the signatures, for Ed25519, RSA and ECDSA. Skipped where OpenSSH isn't installed.
/// </summary>
[TestFixture, NonParallelizable]
public class SshAgentTests
{
    private string _dir = null!;
    private ServiceProvider _sp = null!;
    private SshAgentServer _agent = null!;
    private readonly List<string> _prompts = new();
    private bool _allow = true;

    private sealed class Approver : ISshApprover
    {
        private readonly SshAgentTests _t;
        public Approver(SshAgentTests t) => _t = t;
        public Task<bool> ApproveAsync(string keyTitle, CancellationToken ct) { lock (_t._prompts) _t._prompts.Add(keyTitle); return Task.FromResult(_t._allow); }
    }

    private static (int Code, string Out) Run(string file, string args, Dictionary<string, string>? env = null, string? input = null)
    {
        var psi = new ProcessStartInfo(file, args) { RedirectStandardOutput = true, RedirectStandardError = true, RedirectStandardInput = input != null, UseShellExecute = false };
        foreach (var (k, v) in env ?? new()) psi.Environment[k] = v;
        using var p = Process.Start(psi)!;
        if (input != null) { p.StandardInput.Write(input); p.StandardInput.Close(); }
        var output = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
        p.WaitForExit(30000);
        return (p.ExitCode, output);
    }

    [SetUp]
    public void SetUp()
    {
        if (!File.Exists("/usr/bin/ssh-keygen") || !File.Exists("/usr/bin/ssh-add")) Assert.Ignore("OpenSSH client isn't installed.");
        _allow = true;
        _prompts.Clear();
        _dir = Path.Combine(Path.GetTempPath(), $"ssh-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_dir);
        var s = new ServiceCollection();
        s.AddLogging(); s.AddCryptographyServices(); s.AddSingleton<IVaultSessionService, VaultSessionService>();
        s.AddDbContext<VaultGuardDbContext>(o => o.UseSqlite($"Data Source={Path.Combine(_dir, "v.db")};Pooling=False"));
        _sp = s.BuildServiceProvider();
        var crypto = _sp.GetRequiredService<IPasswordCryptoService>();
        var sessions = _sp.GetRequiredService<IVaultSessionService>();
        var sid = sessions.InitializeSession("me", crypto.DeriveMasterKey("pw", crypto.GenerateUserSalt()));
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VaultGuardDbContext>();
        db.Database.EnsureCreated();
        db.Users.Add(new ApplicationUser { Id = "me", UserName = "me", Email = "me@x" });
        foreach (var (type, extra) in new[] { ("ed25519", ""), ("rsa", "-b 2048"), ("ecdsa", "-b 256") })
        {
            var path = Path.Combine(_dir, "id_" + type);
            Assert.That(Run("ssh-keygen", $"-q -t {type} {extra} -N \"\" -C test -f {path}").Code, Is.EqualTo(0));
            var item = new PasswordItem { Title = $"My {type} key", Type = ItemType.SshKey, UserId = "me",
                CustomFields = new List<CustomField> { new() { Name = "Private Key", IsProtected = true, Value = File.ReadAllText(path) } } };
            VaultCipher.ForSession(sessions, sid)!.Seal(item);
            db.PasswordItems.Add(item);
            File.Delete(path); // the private key now lives only in the vault
        }
        db.SaveChanges();
        var stored = string.Concat(db.CustomFields.Select(c => c.StoredValue));
        Assert.That(stored, Does.Not.Contain("PRIVATE KEY"), "keys are stored encrypted");
        _agent = new SshAgentServer(_sp, new Approver(this));
        _agent.Start($"vgtest-{Guid.NewGuid():N}"[..20]);
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_agent != null) await _agent.DisposeAsync();
        if (_sp != null) await _sp.DisposeAsync();
        SqliteConnection.ClearAllPools();
        try { Directory.Delete(_dir, true); } catch { }
    }

    private Dictionary<string, string> Env => new() { ["SSH_AUTH_SOCK"] = _agent.SocketPath! };

    [Test]
    public void SshAdd_ListsTheVaultsKeys()
    {
        var (code, output) = Run("ssh-add", "-l", Env);
        Assert.That(code, Is.EqualTo(0), output);
        Assert.That(output, Does.Contain("My ed25519 key").And.Contain("My rsa key").And.Contain("My ecdsa key"));
        Assert.That(output, Does.Contain("ED25519").And.Contain("RSA").And.Contain("ECDSA"));
    }

    [TestCase("ed25519")]
    [TestCase("rsa")]
    [TestCase("ecdsa")]
    public void SshKeygen_SignsThroughTheAgent_AndTheSignatureVerifies(string type)
    {
        // The public key of this vault key, as the agent lists it.
        var pub = Run("ssh-add", "-L", Env).Out.Split('\n').First(l => l.Contains($"My {type} key"));
        var pubFile = Path.Combine(_dir, $"{type}.pub");
        File.WriteAllText(pubFile, pub + "\n");
        var msg = Path.Combine(_dir, $"msg-{type}.txt");
        File.WriteAllText(msg, "hello from the vault");

        var sign = Run("ssh-keygen", $"-Y sign -f {pubFile} -n file {msg}", Env);
        Assert.That(sign.Code, Is.EqualTo(0), sign.Out);
        Assert.That(_prompts, Does.Contain($"My {type} key"), "the user was asked");

        var allowed = Path.Combine(_dir, "allowed");
        File.WriteAllText(allowed, "me@x " + pub + "\n");
        var verify = Run("ssh-keygen", $"-Y verify -f {allowed} -I me@x -n file -s {msg}.sig", input: File.ReadAllText(msg));
        Assert.That(verify.Code, Is.EqualTo(0), verify.Out);
        Assert.That(verify.Out, Does.Contain("Good"));
    }

    [Test]
    public void Declined_OrLocked_SignsNothing()
    {
        _allow = false;
        var pub = Run("ssh-add", "-L", Env).Out.Split('\n').First(l => l.Contains("My ed25519 key"));
        var pubFile = Path.Combine(_dir, "d.pub"); File.WriteAllText(pubFile, pub + "\n");
        var msg = Path.Combine(_dir, "d.txt"); File.WriteAllText(msg, "x");
        Assert.That(Run("ssh-keygen", $"-Y sign -f {pubFile} -n file {msg}", Env).Code, Is.Not.EqualTo(0));

        var sessions = _sp.GetRequiredService<IVaultSessionService>();
        sessions.LockVault(sessions.GetActiveSessionId()!);
        var listed = Run("ssh-add", "-l", Env);
        Assert.That(listed.Out, Does.Contain("no identities"), "a locked vault offers no keys");
    }
}
