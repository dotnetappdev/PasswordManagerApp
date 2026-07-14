# Passkeys

VaultGuard supports passkeys in two distinct ways. They use different machinery, so it helps to keep them
separate.

| | Website passkeys | Device passkeys |
| --- | --- | --- |
| What | Passkeys VaultGuard **stores and uses for other sites** (github.com, etc.), each tied to its site | A passkey that **signs you into VaultGuard itself** on a device |
| Vault item | `ItemType.Passkey` (has a `Website`) | Not a vault item - an `AspNet` account credential (`UserPasskey`) |
| How it is created | Only via the **browser extension** acting as a virtual authenticator | Registered from the Passkeys page (web/WPF/Android) via WebAuthn / platform authenticator |
| Where the private key lives | Software P-256 key, AES-256-GCM encrypted **inside your vault** (zero-knowledge) | In the platform authenticator (Windows Hello / Touch ID / Android keystore) - VaultGuard only stores the public key |

---

## Website passkeys (be the authenticator)

A web page cannot store a passkey into another app - only something that overrides `navigator.credentials`
can. That is the **browser extension** (`VaultGuard.BrowserExtension`):

- `inpage.js` overrides `navigator.credentials.create/get`. When you create a passkey on a site, VaultGuard
  generates the key and stores the **private key encrypted under your master key** instead of letting the
  platform hold it, so it syncs with the rest of your vault.
- Controlled by **Settings -> Passkeys -> "Intercept website passkeys"** (default on). Off = the browser/OS
  handles passkeys normally.
- Backend is selectable in the popup: **native host** (local SQLite, `VaultGuard.BrowserExtension.NativeHost`)
  or **API server** (`POST /api/passkey/vault/create` + `/vault/assert`, real ES256 software authenticator,
  private key encrypted under the master key). The extension falls back to the platform authenticator if the
  vault is locked or has no matching key, so existing passkeys never break.
- Saved website passkeys appear in every app under **Passkeys -> "Passkeys saved for your websites"**.

Install and setup: [`VaultGuard.BrowserExtension/INSTALLATION.md`](../VaultGuard.BrowserExtension/INSTALLATION.md)
and [`VaultGuard.BrowserExtension/PASSKEYS_SETUP.md`](../VaultGuard.BrowserExtension/PASSKEYS_SETUP.md).

**Status:** the create/assert path is implemented end to end (extension -> native host or API -> encrypted
software key). It still needs real in-browser verification (e.g. on webauthn.io) and has known v1
simplifications (ES256 only, `excludeCredentials`/UV policy not fully enforced, no per-signature consent
prompt).

---

## Device passkeys (sign in to VaultGuard)

Registered from the **Passkeys** page and used from the login screen's **"Sign in with Passkey"** button.
The server side is real and verified with Fido2NetLib (`PasskeyService` + `PasskeyController`:
`register/start|complete`, `authenticate/start|complete`).

### One important truth about a zero-knowledge vault

A passkey assertion **proves identity - it cannot decrypt the vault**. The vault key is derived from your
master password, which the server never has. So "sign in with a passkey" is really a **quick unlock**: after
you sign in once with your master key, VaultGuard caches the master key locally, and a passkey (or Windows
Hello / biometric) releases that cached key. This is why you may see *"Passkey verified, but no master key is
cached on this device"* - it means the one-time master-key sign-in has not happened yet on that device. Every
end-to-end-encrypted manager works this way.

On WPF/web the cached key is protected by the OS (DPAPI, current Windows user). Note that this makes the
biometric an application-level gate, not a cryptographic one: another process running as you could read the
cached key without the biometric. Only enable quick unlock on a trusted, disk-encrypted device. See
[`SECURITY.md`](SECURITY.md).

### Relying Party configuration (required for the real domain)

The WebAuthn Relying Party (RP) is the **web app host**, shared by web and native mobile so one passkey
identity covers everything. It is set in the `Fido2` config section of **both** the API and the Web app
(defaults to `vaultguardapp.dotnetappdevni.com`; local dev overrides to `localhost`). If this does not match
the real origin, registration succeeds in the platform prompt but server verification fails.

### Association files (required for native mobile passkeys)

Android Credential Manager and iOS AutoFill only trust a passkey for the RP domain if that domain serves
association files. The Web host now serves them, driven by the `PasskeyAssociations` config section:

- `GET /.well-known/assetlinks.json` - Android Digital Asset Links (package + signing SHA-256 fingerprints).
- `GET /apple-app-site-association` (and under `/.well-known/`) - iOS `webcredentials` app IDs.

**Before mobile passkeys will work you must:**

1. **Android:** add your **release** keystore's SHA-256 fingerprint to
   `PasskeyAssociations:AndroidSha256CertFingerprints` (the shipped value is the shared **debug** keystore
   only). Get it with `keytool -list -v -keystore <release.jks> -alias <alias>`.
2. **iOS:** set `PasskeyAssociations:AppleTeamId` to your Apple Developer Team ID (without it the iOS file
   emits no apps). Bundle IDs default to `com.vaultguard.app` and `com.vaultguard.app.autofill`. Add the
   `webcredentials:vaultguardapp.dotnetappdevni.com` associated domain to the iOS entitlements.
3. Deploy the Web app and confirm both files are reachable over valid HTTPS at the RP domain.

### Per-platform status

| Platform | Register | Sign in (quick unlock) | Notes |
| --- | --- | --- | --- |
| Web (Blazor) | Yes | Yes | Server-verified FIDO2; needs cached master key |
| WPF | Yes | Yes (Windows Hello) | DPAPI-cached master key; see security note above |
| Android | Yes (Credential Manager) | Needs `assetlinks.json` live + release fingerprint | RP now config-driven on the API |
| iOS | Not yet | Not yet | Needs Team ID + associated-domain entitlement; native flow still TODO |

---

## What is genuinely done vs. needs a real device/browser

Building and wiring is complete and compiles for the items marked "Yes" above. The final "does the browser /
Windows Hello / phone actually accept this passkey" step must be exercised on a real browser and device - it
cannot be verified headlessly. Drive those flows and report what breaks, and the fixes are quick.
