using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using VaultGuard.Services.Sharing;

namespace VaultGuard.Sharing;

/// <summary>
/// Secure-share endpoints, used by the API and the Web app (this file is linked into both):
///  - POST /api/shares        create (signed-in users only; the API's key middleware / the Web's login)
///  - GET  /api/shares/{id}   take one view of the encrypted envelope (anonymous; the key is in the link's #fragment)
///  - GET  /s/{id}            the viewer page, which decrypts in the recipient's browser
/// The server only ever stores and returns ciphertext. See VaultGuard.Services.Sharing.SecureShareCrypto.
/// </summary>
public static class SecureShareEndpoints
{
    public static void MapSecureShares(this IEndpointRouteBuilder app, FileShareStore store, bool allowCreate)
    {
        if (allowCreate)
        {
            app.MapPost("/api/shares", (HttpContext ctx, ShareCreateRequest req) =>
            {
                var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
                var (id, expires, error) = store.Create(req.Envelope, TimeSpan.FromMinutes(Math.Max(1, req.ExpiresInMinutes)), req.MaxViews, userId);
                return id == null ? Results.BadRequest(new { error }) : Results.Ok(new ShareCreateResponse { Id = id, ExpiresUtc = expires });
            });

            app.MapDelete("/api/shares/{id}", (HttpContext ctx, string id) =>
            {
                var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
                return store.Delete(id, userId) ? Results.NoContent() : Results.NotFound();
            });
        }

        app.MapGet("/api/shares/{id}", (HttpContext ctx, string id) =>
        {
            NoStore(ctx);
            var rec = store.TakeView(id);
            return rec == null
                ? Results.NotFound(new { error = "This link has expired or has already been used." })
                : Results.Ok(new { envelope = rec.Envelope, viewsLeft = rec.ViewsLeft, expiresUtc = rec.ExpiresUtc });
        });

        app.MapGet("/s/{id}", (HttpContext ctx, string id) =>
        {
            NoStore(ctx);
            ctx.Response.Headers["Content-Security-Policy"] =
                "default-src 'none'; script-src 'unsafe-inline'; style-src 'unsafe-inline'; connect-src 'self'; img-src data:; base-uri 'none'; form-action 'none'; frame-ancestors 'none'";
            ctx.Response.Headers["Referrer-Policy"] = "no-referrer";
            ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
            return Results.Content(ViewerHtml, "text/html; charset=utf-8");
        });
    }

    private static void NoStore(HttpContext ctx)
    {
        ctx.Response.Headers["Cache-Control"] = "no-store";
        ctx.Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
    }

    /// <summary>The recipient's page. Nothing is fetched until they click "Show"; decryption happens here.</summary>
    public const string ViewerHtml = """
<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<meta name="robots" content="noindex, nofollow"><meta name="color-scheme" content="light dark"><title>Shared with you · Vault Guard</title>
<style>
:root{--bg:#f4f6fb;--card:#fff;--fg:#172033;--muted:#667085;--line:#e4e7ec;--accent:#2252DA;--field:#f8f9fc}
@media (prefers-color-scheme:dark){:root{--bg:#12151c;--card:#1c2130;--fg:#e6e9f0;--muted:#98a2b3;--line:#313849;--accent:#6C8DF3;--field:#161a24}}
*{box-sizing:border-box}body{margin:0;min-height:100vh;display:grid;place-items:center;padding:16px;background:var(--bg);color:var(--fg);font:15px/1.45 -apple-system,"Segoe UI",system-ui,sans-serif}
.card{width:100%;max-width:460px;background:var(--card);border:1px solid var(--line);border-radius:16px;padding:22px;box-shadow:0 18px 50px -20px rgba(16,24,40,.35)}
h1{font-size:19px;margin:0 0 4px}.sub{color:var(--muted);margin:0 0 16px}.brand{display:flex;gap:10px;align-items:center;margin-bottom:14px;font-weight:650}
.logo{width:26px;height:26px;border-radius:7px;background:linear-gradient(#6C8DF3,#2252DA);display:grid;place-items:center;color:#fff;font-size:14px}
button{font:inherit;font-weight:600;border:0;border-radius:10px;padding:10px 16px;cursor:pointer;background:var(--accent);color:#fff}
button.ghost{background:transparent;color:var(--accent);padding:6px 10px}
.field{margin:10px 0}.label{font-size:11px;font-weight:700;letter-spacing:.06em;text-transform:uppercase;color:var(--muted);margin-bottom:4px}
.row{display:flex;gap:8px;align-items:center;background:var(--field);border:1px solid var(--line);border-radius:10px;padding:8px 10px}
.val{flex:1;min-width:0;word-break:break-all}.notes{white-space:pre-wrap}
input{font:inherit;width:100%;padding:10px;border-radius:10px;border:1px solid var(--line);background:var(--field);color:var(--fg)}
.msg{color:var(--muted);font-size:13px;margin-top:14px}.err{color:#DC2626}
</style></head><body><main class="card" id="app">
<div class="brand"><span class="logo">🔒</span>Vault Guard</div>
<h1>Someone shared an item with you</h1>
<p class="sub">It's encrypted. Only this link can open it, and it may only open a limited number of times.</p>
<button id="show" type="button">Show it</button>
<p class="msg" id="msg"></p>
</main>
<script>
(() => {
  const $ = (id) => document.getElementById(id);
  const msg = (t, bad) => { $('msg').textContent = t; $('msg').className = 'msg' + (bad ? ' err' : ''); };
  const id = location.pathname.split('/').pop();
  const linkKey = location.hash.slice(1);
  if (!linkKey) { $('show').remove(); msg('This link is incomplete: the part after "#" is missing. Ask for the full link.', true); return; }
  const unb64 = (s) => Uint8Array.from(atob(s.replace(/-/g, '+').replace(/_/g, '/').padEnd(Math.ceil(s.length / 4) * 4, '=')), c => c.charCodeAt(0));
  let envelope = null, viewsLeft = null;

  async function deriveKey(passphrase) {
    let ikm = unb64(linkKey);
    if (envelope.salt) {
      const base = await crypto.subtle.importKey('raw', new TextEncoder().encode(passphrase), 'PBKDF2', false, ['deriveBits']);
      const pk = new Uint8Array(await crypto.subtle.deriveBits({ name: 'PBKDF2', hash: 'SHA-256', salt: unb64(envelope.salt), iterations: envelope.iter }, base, 256));
      const both = new Uint8Array(ikm.length + pk.length); both.set(ikm); both.set(pk, ikm.length); ikm = both;
    }
    const hk = await crypto.subtle.importKey('raw', ikm, 'HKDF', false, ['deriveKey']);
    return crypto.subtle.deriveKey({ name: 'HKDF', hash: 'SHA-256', salt: new Uint8Array(0), info: new TextEncoder().encode('VaultGuard share v1') },
      hk, { name: 'AES-GCM', length: 256 }, false, ['decrypt']);
  }

  async function open(passphrase) {
    const key = await deriveKey(passphrase);
    const plain = await crypto.subtle.decrypt({ name: 'AES-GCM', iv: unb64(envelope.iv) }, key, unb64(envelope.ct));
    render(JSON.parse(new TextDecoder().decode(plain)));
  }

  function field(label, value, secret) {
    if (!value) return '';
    const safe = (s) => String(s).replace(/[&<>"']/g, ch => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[ch]));
    return `<div class="field"><div class="label">${label}</div><div class="row"><span class="val ${label === 'Notes' ? 'notes' : ''}" data-v="${safe(value)}">${secret ? '••••••••' : safe(value)}</span>` +
      (secret ? `<button class="ghost" data-act="show" type="button">Show</button>` : '') + `<button class="ghost" data-act="copy" type="button">Copy</button></div></div>`;
  }

  function render(c) {
    const left = viewsLeft === null ? 'It can be opened again until it expires.' : viewsLeft > 0 ? `It can be opened ${viewsLeft} more time${viewsLeft === 1 ? '' : 's'}.` : 'This link has now been used up: save what you need.';
    $('app').innerHTML = `<div class="brand"><span class="logo">🔒</span>Vault Guard</div><h1></h1><p class="sub">${left}</p>` +
      field('Username', c.username) + field('Password', c.password, true) + field('Website', c.url) + field('Notes', c.notes) +
      `<p class="msg">Decrypted in your browser. Vault Guard's server never saw it.</p>`;
    $('app').querySelector('h1').textContent = c.title || 'Shared item';
    $('app').addEventListener('click', async (e) => {
      const b = e.target.closest('button'); if (!b) return;
      const v = b.parentElement.querySelector('.val');
      if (b.dataset.act === 'show') { const hidden = v.textContent.startsWith('•'); v.textContent = hidden ? v.dataset.v : '••••••••'; b.textContent = hidden ? 'Hide' : 'Show'; }
      if (b.dataset.act === 'copy') { try { await navigator.clipboard.writeText(v.dataset.v); b.textContent = 'Copied'; setTimeout(() => b.textContent = 'Copy', 1500); } catch {} }
    });
  }

  function askPassphrase() {
    $('app').insertAdjacentHTML('beforeend', `<div class="field" id="pp"><div class="label">Passphrase</div><input id="pass" type="password" autocomplete="off" placeholder="The passphrase you were given"></div><button id="unlock" type="button">Open</button>`);
    const go = async () => { try { await open($('pass').value); } catch { msg("That passphrase doesn't open it. Try again.", true); } };
    $('unlock').addEventListener('click', go);
    $('pass').addEventListener('keydown', (e) => { if (e.key === 'Enter') go(); });
    $('pass').focus();
  }

  $('show').addEventListener('click', async () => {
    $('show').disabled = true; msg('Opening…');
    try {
      const r = await fetch('/api/shares/' + encodeURIComponent(id), { cache: 'no-store', credentials: 'omit' });
      if (!r.ok) { $('show').remove(); msg('This link has expired or has already been used.', true); return; }
      const data = await r.json(); envelope = data.envelope; viewsLeft = data.viewsLeft ?? null;
      $('show').remove(); msg('');
      if (envelope.salt) askPassphrase(); else await open();
    } catch { msg("This link couldn't be opened. It may be incomplete.", true); $('show').disabled = false; }
  });
})();
</script></body></html>
""";
}
