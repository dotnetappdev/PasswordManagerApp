# Vault Guard browser helper (native messaging host)

The small program the browser starts when the Vault Guard extension needs your vault. It doesn't read the
database itself: it passes each request on to the **running Vault Guard app** (WPF, or MAUI on Windows), which
answers from your unlocked vault. You never install or register it by hand.

```
extension ──stdio (native messaging)──► VaultGuard.BrowserExtension.NativeHost.exe
                                                │ named pipe, current Windows user only
                                                ▼
                                  Vault Guard app (WPF / MAUI) — pairing check, unlocked vault
```

- **Shipped with the app.** `build/BrowserSupport.targets` builds the helper into the app's `BrowserSupport\`
  folder on every build, and as a self-contained copy on publish.
- **Registered by the app on every start.** `BrowserLinkRegistrar` writes the native-messaging manifests and
  `HKCU` registry keys for Chrome, Edge, Brave, Chromium, Vivaldi and Firefox, for the current user only (no
  admin rights). After an update or a move the registration follows the app automatically.
- **Only our extension may start it.** The manifest allows only `chrome-extension://niapolneagndnohemlljncmndahkoncp/`
  (fixed by the `key` in the extension's `manifest.json`) and, for Firefox, `vaultguard@dotnetappdevni.com`.

## What it forwards

`hello`, `pair`, `getCredentials`, `getCreditCards`, `getTotpCode`, `saveTotpSecret`, `saveLogin` and `checkPasswordReuse` go to the app.

| Request | What the app does |
|---|---|
| `hello` | Reports whether the app is running, the browser is paired and the vault is unlocked |
| `pair` | Shows **"Allow this browser to use Vault Guard?"**; on **Allow** returns a random 256-bit secret (the app stores only its hash) |
| `getCredentials` | Logins whose saved website matches the page's site. Passwords are decrypted only for a matching site; with no site, titles and usernames only |
| `getCreditCards` | Saved cards for checkout forms |
| `getTotpCode` | The current one-time code for one login, after checking it matches the site |
| `saveTotpSecret` | Saves an authenticator QR code found on a site to the matching login (encrypted) |
| `checkPasswordReuse` | Phishing guard: is the typed password (`loginPassword`) saved for a different site? Returns login titles and sites only, and nothing if it's also saved for this site |
| `saveLogin` | "Save this login?": creates a login for the site, or updates a changed password (old one kept in history). The site password is sent as `loginPassword`; any `password` field is stripped before forwarding |

Every request carries the pairing secret and the extension's origin. The app refuses anything from an
unpaired browser, or while the vault is locked. Protocol: 4-byte little-endian length plus UTF-8 JSON, in both
directions.

When the app isn't running, `hello` says so and the extension shows "Open Vault Guard". The older
stand-alone handlers in `Program.cs` (direct SQLite access with the master password) are kept only as a
fallback for setups without the app.

## Build

You don't normally build it yourself: building `VaultGuard.WPF` or `VaultGuard.App` (Windows) builds it into
`BrowserSupport\`. To build it on its own:

```bash
dotnet build VaultGuard.BrowserExtension.NativeHost -r win-x64
```

Trimming is off on purpose (`PublishTrimmed=false`): the JSON protocol uses reflection-based serialization,
and a trimmed build failed on its first message.

## Testing

See [`../VaultGuard.BrowserExtension/TESTING_LOCALLY.md`](../VaultGuard.BrowserExtension/TESTING_LOCALLY.md)
for loading the extension unpacked and connecting it to the app, and [`../docs/BROWSER_LINK.md`](../docs/BROWSER_LINK.md)
for the design and security model.

## Troubleshooting

| Symptom | Fix |
|---|---|
| "Browser link helper not installed" | Start the app once (it registers the helper), check `BrowserSupport\VaultGuard.BrowserExtension.NativeHost.exe` exists next to the app, then restart the browser |
| "Vault Guard isn't running" | Start the app as yourself (not "Run as administrator"); the pipe only accepts the same Windows user |
| Connect doesn't finish | Look for the **Allow** dialog in the app; it times out after two minutes |
| Check the registration | `%LOCALAPPDATA%\VaultGuard\BrowserLink\com.passwordmanager.native_host.json` and `HKCU\Software\Google\Chrome\NativeMessagingHosts\com.passwordmanager.native_host` |
| Logs | `%LOCALAPPDATA%\VaultGuard\logs\<year>\<month>\<day>.txt` (app and helper) |
