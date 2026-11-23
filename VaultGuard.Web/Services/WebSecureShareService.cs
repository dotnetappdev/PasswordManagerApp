using Microsoft.AspNetCore.Components;
using VaultGuard.Components.Shared.Services;
using VaultGuard.Services.Interfaces;
using VaultGuard.Services.Sharing;

namespace VaultGuard.Web.Services;

/// <summary>The Web app keeps shares itself and serves the viewer at /s/{id} (SecureShareEndpoints).</summary>
public sealed class WebSecureShareService : ISecureShareService
{
    private readonly FileShareStore _store;
    private readonly NavigationManager _nav;
    private readonly IAuthService _auth;

    public WebSecureShareService(FileShareStore store, NavigationManager nav, IAuthService auth)
    {
        _store = store;
        _nav = nav;
        _auth = auth;
    }

    public Task<SecureShareClient.Result> CreateAsync(ShareContent content, ShareOptions options)
    {
        var userId = _auth.CurrentUser?.Id;
        if (string.IsNullOrEmpty(userId)) return Task.FromResult(new SecureShareClient.Result(false, null, null, "Sign in to share."));
        // Encrypted here, before it's stored: the store only ever holds ciphertext, and the key goes in the link only.
        var (env, linkKey) = SecureShareCrypto.Encrypt(content, options.Passphrase);
        var (id, expires, error) = _store.Create(env, options.ExpiresIn, options.MaxViews, userId);
        return Task.FromResult(id == null
            ? new SecureShareClient.Result(false, null, null, error)
            : new SecureShareClient.Result(true, $"{_nav.BaseUri.TrimEnd('/')}/s/{id}#{linkKey}", expires, null));
    }
}
