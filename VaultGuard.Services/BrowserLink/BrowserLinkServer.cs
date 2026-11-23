using VaultGuard.Models.DTOs.Auth;
using System.IO.Pipes;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VaultGuard.DAL;
using VaultGuard.Models;
using VaultGuard.Services.Interfaces;
using VaultGuard.Services.Security;
using VaultGuard.Services.Utilities;

namespace VaultGuard.Services.BrowserLink;

/// <summary>
/// Runs inside the desktop app (WPF, MAUI on Windows/macOS) and answers the browser extension — via its
/// native-messaging host — from the app's unlocked vault, like 1Password's "integrate with the app":
/// unlock the app and the extension works; lock it and the extension is locked too.
///
/// Every data request must come from a browser the user approved (pairing secret) while the vault is
/// unlocked. Credential lookups always need a page URL and only return items for that site.
/// </summary>
public sealed class BrowserLinkServer : IAsyncDisposable
{
    private readonly IServiceProvider _services;
    private readonly IBrowserLinkApprover? _approver;
    private readonly BrowserLinkPairings _pairings;
    private readonly string _appName;
    private readonly string _appVersion;
    private readonly string _pipeName;
    private readonly CancellationTokenSource _cts = new();
    private readonly SemaphoreSlim _approvalGate = new(1, 1);
    private Task? _loop;

    public BrowserLinkServer(IServiceProvider services, IBrowserLinkApprover? approver, string appName, string appVersion,
        BrowserLinkPairings? pairings = null, string? pipeName = null)
    {
        _services = services;
        _approver = approver;
        _appName = appName;
        _appVersion = appVersion;
        _pairings = pairings ?? new BrowserLinkPairings();
        _pipeName = pipeName ?? BrowserLinkProtocol.PipeName;
    }

    public BrowserLinkPairings Pairings => _pairings;

    public void Start() => _loop ??= Task.Run(() => AcceptLoopAsync(_cts.Token));

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            NamedPipeServerStream? pipe = null;
            try
            {
                // CurrentUserOnly: other OS users (and, on Windows, other sessions' users) can't connect.
                pipe = new NamedPipeServerStream(_pipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await pipe.WaitForConnectionAsync(ct);
                var connection = pipe;
                pipe = null;
                _ = Task.Run(() => ServeAsync(connection, ct), ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                VaultGuard.Services.Logging.AppLogger.Warning("Browser link: accept failed", ex);
                await Task.Delay(500, ct).ContinueWith(_ => { });
            }
            finally { pipe?.Dispose(); }
        }
    }

    private async Task ServeAsync(NamedPipeServerStream pipe, CancellationToken ct)
    {
        await using (pipe)
        {
            try
            {
                using var readTimeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
                readTimeout.CancelAfter(TimeSpan.FromSeconds(10));
                using var request = await BrowserLinkProtocol.ReadAsync(pipe, readTimeout.Token);
                if (request == null) return;
                var response = await HandleAsync(request.RootElement, ct);
                await BrowserLinkProtocol.WriteAsync(pipe, response, ct);
            }
            catch (Exception ex)
            {
                VaultGuard.Services.Logging.AppLogger.Warning("Browser link: request failed", ex);
                try { await BrowserLinkProtocol.WriteAsync(pipe, new { success = false, error = "Vault Guard couldn't handle that request." }, ct); }
                catch { /* client gone */ }
            }
        }
    }

    /// <summary>
    /// Handles one request (public so it can be tested without a pipe). Apart from "pair" (a key exchange that
    /// carries only public keys) and "hello" (status only), every request must be encrypted with the browser's
    /// link key and every answer is encrypted with it (BrowserLinkCrypto), so nothing readable crosses the link.
    /// </summary>
    public async Task<object> HandleAsync(JsonElement req, CancellationToken ct = default)
    {
        var action = req.Utf8("action");
        if (action == "pair") return await PairAsync(req, ct);

        var keyId = req.Utf8("keyId");
        var iv = req.Utf8("iv");
        var key = _pairings.KeyFor(keyId);
        using var inner = key != null && iv.Length > 0 ? BrowserLinkCrypto.OpenRequest(key, action, keyId, iv, req.Utf8("ct")) : null;
        var paired = inner != null;

        var sessions = _services.GetService<IVaultSessionService>();
        var sessionId = sessions?.GetActiveSessionId();
        var unlocked = sessionId != null;

        if (action == "hello")
            return new
            {
                success = true, linked = true, app = _appName, appVersion = _appVersion,
                protocol = BrowserLinkProtocol.Version, unlocked, paired
            };

        if (!paired)
            return new { success = false, code = "NOT_PAIRED", error = "This browser isn't connected to Vault Guard yet. Click Connect in the extension and approve it in the app." };
        if (!unlocked || sessions == null)
            return new { success = false, code = "LOCKED", error = "Unlock Vault Guard to use it in the browser." };

        var userId = sessions.GetSessionUserId(sessionId!);
        var cipher = VaultCipher.ForSession(sessions, sessionId);
        if (userId == null || cipher == null)
            return new { success = false, code = "LOCKED", error = "Unlock Vault Guard to use it in the browser." };

        var fields = inner!.RootElement;
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VaultGuardDbContext>();
        object result = action switch
        {
            "getCredentials" => await GetCredentialsAsync(db, userId, cipher, fields.Utf8("domain"), ct),
            "getCreditCards" => await GetCreditCardsAsync(db, userId, cipher, ct),
            "saveTotpSecret" => await SaveTotpAsync(db, userId, cipher, fields, ct),
            "getTotpCode" => await GetTotpCodeAsync(db, userId, cipher, fields, ct),
            "saveLogin" => await SaveLoginAsync(db, userId, cipher, fields, ct),
            "checkPasswordReuse" => await CheckPasswordReuseAsync(db, userId, cipher, fields, ct),
            "passkeyCreate" => await PasskeyAsync(scope.ServiceProvider, sessions, sessionId!, userId, fields, create: true),
            "passkeyGet" => await PasskeyAsync(scope.ServiceProvider, sessions, sessionId!, userId, fields, create: false),
            _ => new { success = false, code = "UNSUPPORTED", error = $"Unknown action '{action}'." }
        };
        return BrowserLinkCrypto.SealResponse(key!, action, keyId, iv, result);
    }

    /// <summary>
    /// Passkeys for websites, kept in the vault (a software authenticator, like 1Password's). "site" is the tab's
    /// real site, set by the extension's background script (never by the page); the passkey's rpId must be that
    /// site or a parent domain of it, and for sign-in the page's signed client data must name the same origin.
    /// "check" only reports whether a passkey exists, so the extension can ask the user before signing.
    /// </summary>
    private static async Task<object> PasskeyAsync(IServiceProvider sp, IVaultSessionService sessions, string sessionId, string userId,
        JsonElement fields, bool create)
    {
        var passkeys = sp.GetService<IPasskeyService>();
        if (passkeys == null) return new { success = false, error = "Passkeys aren't available in this app.", fallback = true };
        var site = SiteMatcher.Host(fields.Utf8("site"));
        var rpId = (fields.Utf8("rpId") ?? "").Trim().ToLowerInvariant();
        if (site == null || rpId.Length == 0 || !SiteMatcher.Matches(rpId, site))
            return new { success = false, error = "This passkey doesn't belong to this site.", fallback = true };

        // GetMasterKey returns the session's own key: work on a copy, and wipe only the copy.
        var key = sessions.GetMasterKey(sessionId)?.Clone() as byte[];
        if (key == null) return new { success = false, code = "LOCKED", error = "Unlock Vault Guard to use passkeys.", fallback = true };
        try
        {
            if (create)
            {
                var r = await passkeys.CreateVaultPasskeyWithKeyAsync(userId, new VaultPasskeyCreateRequestDto
                {
                    RpId = rpId, RpName = fields.Utf8("rpName"), UserHandle = fields.Utf8("userHandle"),
                    UserName = fields.Utf8("userName"), UserDisplayName = fields.Utf8("userDisplayName")
                }, key);
                return new { success = r.Success, error = r.Error, credentialId = r.CredentialId, attestationObject = r.AttestationObject, publicKeyCose = r.PublicKeyCose, fallback = !r.Success };
            }

            var check = fields.TryGetProperty("check", out var c) && c.ValueKind == JsonValueKind.True;
            var clientData = fields.Utf8("clientDataJSON");
            if (!check && !ClientDataMatches(clientData, site))
                return new { success = false, error = "The sign-in request doesn't match this site.", fallback = true };
            var allow = fields.TryGetProperty("allowCredentialIds", out var a) && a.ValueKind == JsonValueKind.Array
                ? a.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()!).ToList() : new List<string>();
            var g = await passkeys.AssertVaultPasskeyWithKeyAsync(userId, new VaultPasskeyAssertRequestDto
            {
                RpId = rpId, ClientDataJSON = clientData, AllowCredentialIds = allow
            }, key, checkOnly: check);
            return check
                ? new { success = g.Success, available = g.Success, userName = g.UserName, fallback = !g.Success }
                : new { success = g.Success, error = g.Error, credentialId = g.CredentialId, authenticatorData = g.AuthenticatorData, signature = g.Signature, userHandle = g.UserHandle, fallback = !g.Success };
        }
        finally { System.Security.Cryptography.CryptographicOperations.ZeroMemory(key); }
    }

    /// <summary>The page's WebAuthn client data must be a "webauthn.get" for this site's own origin.</summary>
    internal static bool ClientDataMatches(string clientDataB64Url, string site)
    {
        try
        {
            var json = BrowserLinkCrypto.FromB64(clientDataB64Url);
            using var doc = JsonDocument.Parse(json);
            var origin = doc.RootElement.Utf8("origin");
            return doc.RootElement.Utf8("type") == "webauthn.get" && SiteMatcher.Host(origin) == site &&
                   (origin.StartsWith("https://", StringComparison.Ordinal) || SiteMatcher.Host(origin) == "localhost");
        }
        catch { return false; }
    }

    private async Task<object> PairAsync(JsonElement req, CancellationToken ct)
    {
        if (_approver == null) return new { success = false, error = "This app can't approve browsers." };
        var clientPublicKey = req.Utf8("clientPublicKey");
        if (clientPublicKey.Length == 0)
            return new { success = false, code = "UPDATE_EXTENSION", error = "Update the Vault Guard extension to connect it (this version of the app encrypts the browser link)." };
        BrowserLinkCrypto.Exchange exchange;
        try { exchange = BrowserLinkCrypto.Agree(clientPublicKey); }
        catch (Exception ex) when (ex is System.Security.Cryptography.CryptographicException or FormatException)
        { return new { success = false, error = "The browser sent an invalid key." }; }

        var browser = req.Utf8("browser");
        var origin = req.Utf8("origin");
        var label = string.IsNullOrWhiteSpace(browser) ? "A browser" : browser;
        if (!string.IsNullOrWhiteSpace(origin)) label += $" ({origin.TrimEnd('/')})";

        // One approval prompt at a time; give the user up to two minutes to answer.
        if (!await _approvalGate.WaitAsync(TimeSpan.FromSeconds(5), ct))
            return new { success = false, error = "Another browser is waiting for approval in Vault Guard." };
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromMinutes(2));
            bool approved;
            try { approved = await _approver.ApproveAsync(label, timeout.Token); }
            catch (OperationCanceledException) { approved = false; }
            if (!approved) return new { success = false, code = "DECLINED", error = "The connection wasn't approved in Vault Guard." };
            // Only public keys cross the link; each side derives the same key on its own.
            var keyId = _pairings.Create(label, exchange.Key);
            return new { success = true, paired = true, keyId, appPublicKey = exchange.AppPublicKey };
        }
        finally
        {
            System.Security.Cryptography.CryptographicOperations.ZeroMemory(exchange.Key);
            _approvalGate.Release();
        }
    }

    private static async Task<object> GetCredentialsAsync(VaultGuardDbContext db, string userId, VaultCipher cipher, string domain, CancellationToken ct)
    {
        var items = await db.PasswordItems.AsNoTracking()
            .Where(p => p.UserId == userId && !p.IsDeleted && p.LoginItem != null)
            .Include(p => p.LoginItem).Include(p => p.CustomFields)
            .ToListAsync(ct);

        // No page (e.g. the popup on a new tab): list the vault for browsing — titles, usernames and sites only.
        // Passwords are only ever decrypted for a page whose site matches, so the vault can't be dumped.
        if (string.IsNullOrWhiteSpace(domain))
        {
            return new
            {
                success = true,
                credentials = items.Select(i => new
                {
                    id = i.Id, title = i.Title, username = i.LoginItem!.Username ?? "", password = "",
                    websiteUrl = i.LoginItem.WebsiteUrl ?? i.LoginItem.Website ?? i.Website ?? "", passwordWithheld = true
                }).ToList()
            };
        }

        var credentials = new List<object>();
        foreach (var item in items)
        {
            var url = item.LoginItem!.WebsiteUrl ?? item.LoginItem.Website ?? item.Website ?? "";
            if (!SiteMatcher.Matches(url, domain)) continue;
            cipher.Open(item);
            credentials.Add(new
            {
                id = item.Id, title = item.Title, username = item.LoginItem.Username ?? "",
                password = item.LoginItem.Password ?? "", websiteUrl = url,
                hasTotp = !string.IsNullOrEmpty(TotpHelper.GetSecret(item))
            });
            VaultItemSecrets.Forget(item);
        }
        return new { success = true, credentials };
    }

    private static async Task<object> GetCreditCardsAsync(VaultGuardDbContext db, string userId, VaultCipher cipher, CancellationToken ct)
    {
        var items = await db.PasswordItems.AsNoTracking()
            .Where(p => p.UserId == userId && !p.IsDeleted && p.CreditCardItem != null)
            .Include(p => p.CreditCardItem)
            .ToListAsync(ct);
        var creditCards = items.Select(item =>
        {
            cipher.Open(item);
            var c = item.CreditCardItem!;
            var result = new
            {
                id = item.Id, title = item.Title, cardholderName = c.CardholderName ?? "", cardNumber = c.CardNumber ?? "",
                expiryDate = c.ExpiryDate ?? "", cvv = c.CVV ?? "", cardType = c.CardType.ToString(),
                billingAddressLine1 = c.BillingAddressLine1 ?? "", billingAddressLine2 = c.BillingAddressLine2 ?? "",
                billingCity = c.BillingCity ?? "", billingState = c.BillingState ?? "", billingZipCode = c.BillingZipCode ?? "",
                billingCountry = c.BillingCountry ?? ""
            };
            VaultItemSecrets.Forget(item);
            return result;
        }).ToList();
        return new { success = true, creditCards };
    }

    private static async Task<object> SaveTotpAsync(VaultGuardDbContext db, string userId, VaultCipher cipher, JsonElement req, CancellationToken ct)
    {
        var uri = req.Utf8("otpauthUri");
        var host = req.Utf8("hostname");
        if (string.IsNullOrWhiteSpace(uri)) return new { success = false, error = "otpauthUri is required" };

        var candidates = await db.PasswordItems
            .Where(p => p.UserId == userId && !p.IsDeleted && p.LoginItem != null)
            .Include(p => p.LoginItem).Include(p => p.CustomFields)
            .ToListAsync(ct);
        var item = string.IsNullOrEmpty(host) ? null
            : candidates.FirstOrDefault(p => SiteMatcher.Matches(p.LoginItem!.WebsiteUrl ?? p.LoginItem.Website ?? "", host));
        var created = item == null;
        if (item == null)
        {
            var title = FirstNonEmpty(req.Utf8("issuer"), req.Utf8("account"), host, "Authenticator");
            item = new PasswordItem
            {
                Title = title, Type = ItemType.Login, UserId = userId, CreatedAt = DateTime.UtcNow, LastModified = DateTime.UtcNow,
                LoginItem = new LoginItem { Username = req.Utf8("account"), WebsiteUrl = string.IsNullOrEmpty(host) ? null : "https://" + host, UserId = userId }
            };
            db.PasswordItems.Add(item);
        }
        TotpHelper.SetSecret(item, uri);
        cipher.Seal(item); // the authenticator secret is stored encrypted, like every other secret
        item.LastModified = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return new { success = true, created, updated = !created, itemId = item.Id, title = item.Title };
    }

    /// <summary>
    /// Phishing guard: is the password being typed on this page the saved password of a login for a *different*
    /// site? Returns only the titles and sites of those logins — never a password. If the same password is also
    /// saved for this site, it's the user's own reuse (Security Center flags that), not a warning here.
    /// </summary>
    private static async Task<object> CheckPasswordReuseAsync(VaultGuardDbContext db, string userId, VaultCipher cipher, JsonElement req, CancellationToken ct)
    {
        var domain = req.Utf8("domain");
        var typed = req.Utf8("loginPassword"); // not "password": the helper strips that name
        if (string.IsNullOrWhiteSpace(domain) || typed.Length < 6 || typed.Length > 4096)
            return new { success = true, matches = Array.Empty<object>() };

        var logins = await db.PasswordItems.AsNoTracking()
            .Where(p => p.UserId == userId && !p.IsDeleted && p.LoginItem != null && p.LoginItem.EncryptedPassword != null)
            .Include(p => p.LoginItem)
            .ToListAsync(ct);

        var typedBytes = System.Text.Encoding.UTF8.GetBytes(typed);
        var elsewhere = new List<(string Title, string Site)>();
        var savedHere = false;
        foreach (var item in logins)
        {
            var url = item.LoginItem!.WebsiteUrl ?? item.LoginItem.Website ?? item.Website ?? "";
            cipher.Open(item);
            var pw = item.LoginItem.Password ?? "";
            var same = pw.Length == typed.Length &&
                       System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(System.Text.Encoding.UTF8.GetBytes(pw), typedBytes);
            VaultItemSecrets.Forget(item);
            if (!same) continue;
            if (SiteMatcher.Matches(url, domain)) { savedHere = true; break; }
            elsewhere.Add((item.Title ?? "a login", SiteMatcher.Host(url) ?? url));
        }
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(typedBytes);
        if (savedHere || elsewhere.Count == 0) return new { success = true, matches = Array.Empty<object>() };
        return new { success = true, matches = elsewhere.Take(3).Select(m => new { title = m.Title, site = m.Site }).ToArray() };
    }

    /// <summary>
    /// "Save this login?" from the extension: creates a login for the site, or (with itemId) updates the saved
    /// password of an existing login for the same site, keeping the old one in password history. The site comes
    /// from the browser tab the form was submitted in; an existing item is only touched if it belongs to the
    /// signed-in user and is saved for that same site.
    /// </summary>
    private static async Task<object> SaveLoginAsync(VaultGuardDbContext db, string userId, VaultCipher cipher, JsonElement req, CancellationToken ct)
    {
        var url = req.Utf8("url");
        var domain = req.Utf8("domain");
        var username = req.Utf8("username").Trim();
        // "loginPassword", not "password": the helper strips any "password" field so a master password can never
        // travel over the link.
        var password = req.Utf8("loginPassword"); // exactly as typed
        if (string.IsNullOrWhiteSpace(domain) || string.IsNullOrEmpty(password))
            return new { success = false, error = "domain and password are required" };
        if (password.Length > 4096 || username.Length > 512)
            return new { success = false, error = "That login is too long to save." };
        // The URL must be on the site the request is for; only its scheme + host are stored.
        var site = Uri.TryCreate(url, UriKind.Absolute, out var u) && u.Scheme is "https" or "http" && SiteMatcher.Matches(u.Host, domain)
            ? $"{u.Scheme}://{u.Host}" : "https://" + domain.Trim().TrimEnd('.');

        if (req.TryGetProperty("itemId", out var idEl) && idEl.ValueKind == JsonValueKind.Number && idEl.TryGetInt32(out var itemId))
        {
            var item = await db.PasswordItems
                .Include(p => p.LoginItem).Include(p => p.CustomFields)
                .FirstOrDefaultAsync(p => p.Id == itemId && p.UserId == userId && !p.IsDeleted, ct);
            if (item?.LoginItem == null || !SiteMatcher.Matches(item.LoginItem.WebsiteUrl ?? item.LoginItem.Website ?? item.Website ?? "", domain))
                return new { success = false, error = "No matching login for this site." };

            var previous = (item.LoginItem.EncryptedPassword, item.LoginItem.PasswordNonce, item.LoginItem.PasswordAuthTag);
            cipher.Open(item);
            if (item.LoginItem.Password == password)
            {
                VaultItemSecrets.Forget(item);
                return new { success = true, created = false, updated = false, itemId = item.Id, title = item.Title };
            }
            item.LoginItem.Password = password;
            if (!string.IsNullOrEmpty(username)) item.LoginItem.Username = username;
            cipher.Seal(item);
            if (!string.IsNullOrEmpty(previous.EncryptedPassword))
                PasswordHistoryHelper.Append(item, previous.EncryptedPassword, previous.PasswordNonce, previous.PasswordAuthTag);
            item.LastModified = DateTime.UtcNow;
            VaultItemSecrets.Forget(item);
            await db.SaveChangesAsync(ct);
            return new { success = true, created = false, updated = true, itemId = item.Id, title = item.Title };
        }

        var title = req.Utf8("title").Trim();
        if (title.Length == 0) title = SiteMatcher.Host(domain) ?? domain;
        if (title.Length > 200) title = title[..200];
        var created = new PasswordItem
        {
            Title = title, Type = ItemType.Login, UserId = userId, CreatedAt = DateTime.UtcNow, LastModified = DateTime.UtcNow,
            LoginItem = new LoginItem
            {
                Username = username, Password = password, WebsiteUrl = site, Website = site, UserId = userId,
                Email = username.Contains('@') ? username : null
            }
        };
        cipher.Seal(created); // never stored in plaintext
        VaultItemSecrets.Forget(created);
        db.PasswordItems.Add(created);
        await db.SaveChangesAsync(ct);
        return new { success = true, created = true, updated = false, itemId = created.Id, title = created.Title };
    }

    /// <summary>
    /// The current one-time code for a login, for filling a 2FA field. Only for an item saved for the page's site,
    /// and only the code — the authenticator secret itself never leaves the app.
    /// </summary>
    private static async Task<object> GetTotpCodeAsync(VaultGuardDbContext db, string userId, VaultCipher cipher, JsonElement req, CancellationToken ct)
    {
        var domain = req.Utf8("domain");
        if (!req.TryGetProperty("itemId", out var idEl) || !idEl.TryGetInt32(out var itemId) || string.IsNullOrWhiteSpace(domain))
            return new { success = false, error = "itemId and domain are required" };
        var item = await db.PasswordItems.AsNoTracking()
            .Include(p => p.LoginItem).Include(p => p.CustomFields)
            .FirstOrDefaultAsync(p => p.Id == itemId && p.UserId == userId && !p.IsDeleted, ct);
        if (item?.LoginItem == null || !SiteMatcher.Matches(item.LoginItem.WebsiteUrl ?? item.LoginItem.Website ?? item.Website ?? "", domain))
            return new { success = false, error = "No matching login for this site." };
        cipher.Open(item);
        var secret = TotpHelper.GetSecret(item) ?? item.LoginItem.TotpSecret;
        var totp = new VaultGuard.Services.Services.TotpService();
        var code = totp.TryParse(secret, out _, out _, out var period) ? totp.GenerateCode(secret) : null;
        VaultItemSecrets.Forget(item);
        return code == null
            ? new { success = false, error = "This login has no authenticator set up." }
            : new { success = true, code, secondsRemaining = totp.GetRemainingSeconds(period) };
    }

    private static string FirstNonEmpty(params string[] values) => values.First(v => !string.IsNullOrWhiteSpace(v));

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        if (_loop != null) { try { await _loop; } catch { /* stopping */ } }
        _cts.Dispose();
    }
}

/// <summary>
/// Which saved sites a page may fill. Exact host, or a subdomain of the saved host (saved "example.com"
/// fills "login.example.com" but not the reverse). Hosts on shared-hosting suffixes (github.io,
/// azurewebsites.net, …) and bare public suffixes only ever match exactly — otherwise credentials saved for
/// one tenant's site would be offered to every other tenant's site.
/// </summary>
public static class SiteMatcher
{
    private static readonly string[] SharedSuffixes =
    {
        "github.io", "gitlab.io", "herokuapp.com", "azurewebsites.net", "cloudapp.net", "vercel.app", "netlify.app",
        "pages.dev", "workers.dev", "web.app", "firebaseapp.com", "appspot.com", "blogspot.com", "cloudfront.net",
        "amazonaws.com", "onrender.com", "fly.dev", "glitch.me", "repl.co", "ngrok.io", "ngrok-free.app"
    };

    // Registry-controlled second levels: the suffix itself is public, but "bank.co.uk" belongs to one owner.
    private static readonly string[] PublicSecondLevels =
    {
        "co.uk", "org.uk", "ac.uk", "gov.uk", "com.au", "net.au", "org.au", "co.nz", "co.jp", "com.br", "co.in", "co.za"
    };

    public static string? Host(string urlOrHost)
    {
        if (string.IsNullOrWhiteSpace(urlOrHost)) return null;
        var s = urlOrHost.Trim();
        if (!s.Contains("://")) s = "https://" + s;
        if (!Uri.TryCreate(s, UriKind.Absolute, out var uri) || string.IsNullOrEmpty(uri.Host)) return null;
        var host = uri.Host.ToLowerInvariant().TrimEnd('.');
        return host.StartsWith("www.") ? host[4..] : host;
    }

    public static bool Matches(string savedUrl, string pageHostOrUrl)
    {
        var saved = Host(savedUrl);
        var page = Host(pageHostOrUrl);
        if (saved == null || page == null) return false;
        if (saved == page) return true;
        if (!saved.Contains('.') || PublicSecondLevels.Contains(saved)) return false; // a bare suffix: exact only
        if (SharedSuffixes.Any(sfx => saved == sfx || saved.EndsWith("." + sfx))) return false; // per-tenant hosts: exact only
        return page.EndsWith("." + saved);
    }
}
