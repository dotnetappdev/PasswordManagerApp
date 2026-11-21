# Browser link (extension ↔ Vault Guard app)

The browser extension uses the Vault Guard desktop app (WPF, or the MAUI app on Windows), the same way 1Password's extension uses the 1Password app:

- **The app holds the keys.** Unlock Vault Guard and the extension can fill. Lock it and the extension is locked too. The extension never asks for, sends or stores the master password.
- **Approve each browser once.** Click **Connect to Vault Guard app** in the extension popup, then choose **Allow** in the app. The browser gets a random 256-bit secret. The app stores only its SHA-256.
- **Updates keep it working.** On every start, the app registers its browser helper with every installed browser, for the current user, with no admin rights needed. After an update, a move or a reinstall, the browsers follow the new location automatically.

## How it fits together

```
extension (background.js) ──native messaging──▶ BrowserSupport\VaultGuard.BrowserExtension.NativeHost.exe
                                                     │  named pipe (current user only)
                                                     ▼
                                     Vault Guard app: BrowserLinkServer (VaultGuard.Services/BrowserLink)
```

- **Helper:** `build/BrowserSupport.targets` ships the native host next to each app, in `BrowserSupport\`. Publish builds make it self-contained.
- **Registration:** `BrowserLinkRegistrar` writes the host manifest and the per-user registration for:
  - Chrome, Edge, Brave, Chromium and Vivaldi: HKCU `…\NativeMessagingHosts\com.passwordmanager.native_host`.
  - Firefox: HKCU `Software\Mozilla\NativeMessagingHosts`.
  - On macOS and Linux it writes the per-user `NativeMessagingHosts` folders instead.
- **Extension IDs:**
  - Chromium browsers: `niapolneagndnohemlljncmndahkoncp`. The `key` in `VaultGuard.BrowserExtension/manifest.json` fixes this ID for unpacked and side-loaded copies.
  - Firefox: `vaultguard@dotnetappdevni.com`.
  - Store builds get their own IDs. Add them, comma-separated, to the `BrowserLink:ExtensionIds` setting.
- **Requests answered by the app:**
  - `hello`: app name, version, protocol version, locked/unlocked, paired.
  - `pair`: triggers the approval prompt.
  - `getCredentials`: needs a page domain. It returns logins for that site only: an exact host, or a subdomain of the saved host, and never across shared-hosting suffixes such as `github.io`. With no domain, it returns a list for browsing that never contains passwords.
  - `getCreditCards`.
  - `saveTotpSecret`: the secret is stored encrypted.
  - `getTotpCode`: the current one-time code for one login, only if it's saved for the page's site.
  - `checkPasswordReuse`: the phishing guard. Is the password being typed (sent as `loginPassword`) saved for a
    different site? Returns the titles and sites of those logins, never passwords, and nothing when the same
    password is also saved for the current site. Compared in constant time; rate-limited by the extension.
  - `saveLogin`: "Save this login?" from the extension. It creates a login for the site, or (with `itemId`)
    updates the password of an existing login saved for the same site, keeping the old one in password history.
    The site comes from the browser tab, not the page, and the password travels as `loginPassword` (the helper
    strips any field named `password`, so a master password can never cross the link). It's encrypted before
    it's stored.
- **When the app isn't running:** `hello` reports it, and the helper falls back to its older local-database mode.

## Security

- **Pipe:** only the current OS user can open it. There's one request per connection, and messages are capped at 1 MB.
- **Every data request needs** a paired browser and an unlocked vault.
- **Approval prompt:** it names the browser and the extension origin. Prompts are shown one at a time, and a prompt nobody answers within two minutes counts as declined.
- **Forgetting browsers:** `WpfBrowserLink.DisconnectAllBrowsers()` removes all approved browsers.

## Troubleshooting

- **"Open the Vault Guard app…":** the app isn't running, or this browser's helper registration is missing. Starting the app re-registers it.
- **"Unlock Vault Guard…":** the app is locked.
- **"…too old for this extension":** update the app.
- **`install-windows.bat`:** this is the old admin-only installer and is no longer needed.
