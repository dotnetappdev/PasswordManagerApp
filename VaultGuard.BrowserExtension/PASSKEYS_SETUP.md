# Setting up Passkeys (WebAuthn) with the Password Manager

This guide explains how to turn the Password Manager into a **virtual passkey
authenticator** for third‑party websites — the same model 1Password uses.

When set up, registering for a passkey‑enabled site (e.g. github.com, google.com,
webauthn.io) saves the passkey's **private key, encrypted, in your vault**, and
logging back in signs the site's challenge using that key. The browser never sees
the private key, and it syncs anywhere your vault syncs.

---

## How it works (overview)

```
 Website  ──navigator.credentials.create/get──►  inpage.js  (page world)
                                                     │  postMessage
                                                     ▼
                                                 content.js  (extension)
                                                     │  chrome.runtime
                                                     ▼
                                              background.js  (service worker)
                                                     │  native messaging (stdio)
                                                     ▼
                                   Native Host (.exe)  ──reads/writes──►  SQLite vault
                                   • generates P‑256 key pair
                                   • encrypts private key with your master key (AES‑256‑GCM)
                                   • signs challenges with ES256
```

- **Creation** – the native host generates a P‑256 key pair, encrypts the private
  key under your master key, and stores it in the `UserPasskeys` table.
- **Zero‑knowledge** – the private key is AES‑256‑GCM encrypted *before* it touches
  the database. The vault must be unlocked (you must be signed in) to use it.
- **Authentication** – the host decrypts the key in memory and signs the site's
  challenge; the signature counter is incremented each time.

---

## Choosing your backend (Local SQLite vs API)

Like other password managers, the extension can read your vault from different places.
Pick one in the popup → **⚙️ Settings → Data Source**:

| Mode | What it uses | When to use |
|---|---|---|
| **Desktop app (local SQLite)** | The native host reads your local `passwordmanager.db` | Most private; everything stays on your machine. **Required for passkeys today.** |
| **API server (self-hosted)** | Your own server's REST API over HTTP(S) | You run the API yourself and want browser access from anywhere. |
| **API server (live / cloud)** | The hosted service URL | You use the cloud sync service. |
| **Auto** | Tries the local app first, then the API | Convenient default. |
| **Offline** | Cached credentials in the browser | Read‑only fallback. |

- For an **API** backend, set the **API server URL** (presets are provided for
  self‑hosted `localhost` and cloud) and sign in with your email + master password.
- **Passkeys** currently work in **Desktop app / Local SQLite** mode. To use passkeys
  through a self‑hosted or live API, the server must expose the vault‑passkey endpoints
  `POST /api/passkey/vault/create` and `POST /api/passkey/vault/assert` (same key‑gen,
  zero‑knowledge encryption and ES256 signing the native host performs). Until those
  exist, passkey requests in API mode fall back to the platform authenticator.

The rest of this guide covers the **Local SQLite** setup, which is what passkeys need.

---

## Prerequisites

- Windows 10/11 (these steps use Windows; macOS/Linux scripts exist in
  `PasswordManager.BrowserExtension.NativeHost/`).
- [.NET SDK 8.0+](https://dotnet.microsoft.com/download) installed (to build the host).
- Google Chrome or Microsoft Edge.
- A Password Manager account already created (you need your **email + master password**).

---

## Step 1 — Build & install the Native Messaging Host

The native host is the bridge that reads your SQLite vault and does the cryptography.

1. Open a terminal **as Administrator**.
2. Run the installer:

   ```bat
   cd PasswordManager.BrowserExtension.NativeHost
   install-windows.bat
   ```

   This builds a self‑contained `PasswordManager.BrowserExtension.NativeHost.exe`
   into `C:\Program Files\PasswordManager\NativeHost\` and writes a native‑host
   manifest (`com.passwordmanager.native_host.json`) next to it.

> The host auto‑locates your vault DB (`passwordmanager.db` in
> `%APPDATA%\PasswordManager\` or `%LOCALAPPDATA%\PasswordManager\`). If yours lives
> elsewhere, point the extension at it (see *Pointing the extension at the same vault*
> below); the path is passed to the host as `databasePath`.

### Pointing the extension at the same SQLite vault

So the extension shares the **same** `passwordmanager.db` as the desktop app:

1. Open the extension popup → **⚙️ Settings** (also linked on the login screen).
2. Set **Connection Method** to *Auto* or *Native Messaging*.
3. Under **SQLite Database Path**, either:
   - Click **Detect from app** — the native host reports the DB it resolved and fills
     the field for you (recommended), **or**
   - Paste the full path to the same `.db` file the desktop app uses (e.g.
     `C:\Users\You\AppData\Roaming\PasswordManager\passwordmanager.db`). Leave it blank
     to let the host auto‑detect.
4. Click **Save Settings**, then **Test Connection** — it shows the **active database
   path** the host is using so you can confirm it matches the desktop app's vault.

---

## Step 2 — Load the browser extension & get its ID

1. Go to `chrome://extensions` (or `edge://extensions`).
2. Turn on **Developer mode** (top‑right).
3. Click **Load unpacked** and select the `PasswordManager.BrowserExtension` folder.
4. Copy the **Extension ID** shown on the card (a long string like
   `abcdefghijklmnopabcdefghijklmnop`).

---

## Step 3 — Register the host for your browser

The host only talks to your specific extension ID, so you must put that ID into the
host manifest and register it.

1. Edit `C:\Program Files\PasswordManager\NativeHost\com.passwordmanager.native_host.json`
   and replace the placeholder with your real ID:

   ```json
   {
     "name": "com.passwordmanager.native_host",
     "description": "Password Manager Native Messaging Host",
     "path": "C:\\Program Files\\PasswordManager\\NativeHost\\PasswordManager.BrowserExtension.NativeHost.exe",
     "type": "stdio",
     "allowed_origins": [
       "chrome-extension://YOUR_EXTENSION_ID_HERE/"
     ]
   }
   ```

2. Register the manifest with your browser (run in a normal terminal):

   **Chrome**
   ```bat
   reg add "HKCU\Software\Google\Chrome\NativeMessagingHosts\com.passwordmanager.native_host" /ve /t REG_SZ /d "C:\Program Files\PasswordManager\NativeHost\com.passwordmanager.native_host.json" /f
   ```

   **Edge**
   ```bat
   reg add "HKCU\Software\Microsoft\Edge\NativeMessagingHosts\com.passwordmanager.native_host" /ve /t REG_SZ /d "C:\Program Files\PasswordManager\NativeHost\com.passwordmanager.native_host.json" /f
   ```

3. **Fully restart** the browser (close all windows).

---

## Step 4 — Unlock the vault (sign in to the extension)

Passkey signing requires an unlocked vault, so sign in once per browser session:

1. Click the Password Manager extension icon.
2. Enter your **email** and **master password** and sign in.
3. Click **Test connection** if available — it should report the database connection
   is successful.

---

## Step 5 — Create and use a passkey

1. Go to a passkey‑enabled site and choose **"Create a passkey"** / **"Set up passkey"**.
2. The request is intercepted; the host generates and stores the key in your vault.
   The site receives the new credential and confirms registration.
3. Log out, then choose **"Sign in with a passkey"** — the host signs the challenge
   and you're logged in.

You can see stored passkeys in the desktop app under **Passkeys**, and each row in
the vault holds the encrypted private key.

---

## Verifying it works

Use **https://webauthn.io**:

1. Make sure the extension is signed in (Step 4).
2. Enter any username → **Register**. It should succeed and show the credential.
3. Click **Authenticate**. It should succeed using the stored key.

Open the page's **DevTools → Console** to watch for `Password Manager` log lines if
something goes wrong.

---

## Troubleshooting

| Symptom | Likely cause / fix |
|---|---|
| "Vault is locked. Sign in to the extension first." | Open the extension popup and sign in (Step 4). |
| Site uses the OS/Windows Hello prompt instead of the vault | The hook **gracefully falls back** when the vault is locked or no matching key exists. Sign in first; for a fresh site, register a new passkey. |
| `Specified native messaging host not found` | The registry key or `allowed_origins` extension ID is wrong. Re‑check Step 3 and restart the browser. |
| Registration fails on a specific site | Some sites require RS256 (`-257`); v1 issues ES256 (`-7`). The site's native authenticator is used as fallback. |
| Host can't find the database | Set the DB path in the extension options, or confirm `passwordmanager.db` exists under `%APPDATA%\PasswordManager`. |
| Nothing happens at all | Reload the extension at `chrome://extensions`, then reload the website tab so `inpage.js` re‑injects. |

---

## Security notes

- Private keys are **AES‑256‑GCM encrypted under your master key** before storage;
  the master key is derived with PBKDF2 (600k iterations) and only held in memory
  while the vault is unlocked.
- The browser, web pages, and the extension's JS never receive the private key —
  only the final signature.
- Because keys live in your vault rows, they sync across devices like any other item.
- The all‑zero AAGUID is used so the authenticator isn't fingerprintable across sites.

---

## Current limitations (v1)

- Assumes **ES256 (`-7`)** key type (the common case).
- Does not yet enforce `excludeCredentials` or user‑verification policy.
- Resident‑credential discovery matches by site (RP ID) only.
- No explicit consent prompt before signing yet (planned).
