# Testing the Vault Guard extension locally

You can run the extension on your own machine without publishing it to a store and without
any certificate. Load it "unpacked" from this folder. Chrome, Edge, Brave and Firefox all
support this for development.

The steps take about five minutes the first time.

---

## What you need

| | |
|---|---|
| **OS** | Windows 10 or 11. The browser link runs in the Windows builds of the WPF and MAUI apps. |
| **.NET SDK** | .NET 10 (`dotnet --version` shows 10.x). |
| **Browser** | Chrome, Edge, Brave, Chromium or Vivaldi (any recent version), or Firefox 115+. |
| **Repository** | A clone of this repo. The extension folder is `VaultGuard.BrowserExtension\`. |

There are no certificates to install and no admin rights or registry edits to make by hand.
The app does the registration for you, for your Windows user only.

---

## How the pieces fit

```
 web page ──► extension (autofill.js / popup)
                 │  chrome.runtime.sendNativeMessage
                 ▼
          VaultGuard.BrowserExtension.NativeHost.exe   (the "browser helper", started by the browser)
                 │  named pipe, current Windows user only
                 ▼
          Vault Guard app (WPF or MAUI) — answers from the unlocked vault
```

- When it builds, the app copies the helper into its own `BrowserSupport\` folder.
- Each time the app starts, it registers that helper with every browser installed for your
  user.
- The browser starts the helper when the extension asks for it.
- The helper passes each request on to the running app, which checks the browser is paired
  and the vault is unlocked before it answers.

---

## Step 1: Build and run the desktop app once

Use either app. You only need one.

**WPF**

```bash
dotnet build VaultGuard.WPF\VaultGuard.WPF.csproj
```

```bash
dotnet run --project VaultGuard.WPF\VaultGuard.WPF.csproj
```

**MAUI (Windows)**

```bash
dotnet build VaultGuard.App\VaultGuard.App.csproj -f net10.0-windows10.0.19041.0
```

```bash
dotnet run --project VaultGuard.App\VaultGuard.App.csproj -f net10.0-windows10.0.19041.0
```

The build also produces the helper. Check that it's there:

```
VaultGuard.WPF\bin\Debug\net10.0-windows10.0.19041.0\BrowserSupport\VaultGuard.BrowserExtension.NativeHost.exe
```

Sign in and **unlock** your vault. Keep the app running while you test.

> Running from Visual Studio works too. Set `VaultGuard.WPF` (or `VaultGuard.App` with the
> Windows target) as the startup project and press F5.

Check the registration after the first start, if you want to:

- File: `%LOCALAPPDATA%\VaultGuard\BrowserLink\com.passwordmanager.native_host.json`
  (its `"path"` points at the helper above).
- Registry: `HKCU\Software\Google\Chrome\NativeMessagingHosts\com.passwordmanager.native_host`.
  Edge, Brave, Chromium, Vivaldi and Firefox have matching keys.

---

## Step 2: Load the extension

### Chrome, Edge, Brave, Vivaldi

1. Open the extensions page:
   - Chrome: `chrome://extensions`
   - Edge: `edge://extensions`
   - Brave: `brave://extensions`
2. Turn on **Developer mode**. In Chrome it's the switch at the top right; in Edge it's in the
   left sidebar.
3. Click **Load unpacked** and choose the `VaultGuard.BrowserExtension` folder, the one that
   contains `manifest.json`.
4. Check that the card shows the ID **`niapolneagndnohemlljncmndahkoncp`**.

   The ID is fixed by the `key` in `manifest.json`, so it's the same on every machine. The app
   only allows this ID to talk to the helper. If you see a different ID, `manifest.json` has
   lost its `"key"` line.
5. Pin the extension: click the puzzle-piece icon in the toolbar, then the pin next to
   **Vault Guard**.

### Firefox

1. Open `about:debugging#/runtime/this-firefox`.
2. Click **Load Temporary Add-on…** and choose
   `VaultGuard.BrowserExtension\manifest.json`.
3. Firefox uses the add-on ID `vaultguard@dotnetappdevni.com`, which the app registers
   automatically.

Firefox removes temporary add-ons when it closes, so repeat this after each restart.

---

## Step 3: Connect the browser to the app

1. Click the **Vault Guard** toolbar button.
2. The popup shows the app link status. Click **Connect to Vault Guard app**.
3. The app asks **"Allow this browser to use Vault Guard?"**. Click **Allow**.
4. The popup now shows that it's connected.

The browser keeps a random 256-bit pairing secret on this device only. The app stores only a
hash of it. If the app is locked, the popup offers **"I've unlocked it — check again"**.

You only connect once per browser profile. To disconnect, use the popup.

---

## Step 4: Try autofill

You need at least one **Login** item saved in the app with a website URL.

| Try this | What you should see |
|---|---|
| Open a site's sign-in page for which you have a Login saved | A small blue padlock icon at the right edge of the username and password fields |
| Click into the username field | The Vault Guard menu opens under the field, listing your logins for that site |
| Use ↑ / ↓, then **Enter** (or click a login) | Username and password are filled, and a "Filled …" toast appears at the bottom right |
| Press **Esc** | The menu closes and doesn't reopen on that field until you click the padlock |
| Press **Ctrl + \\** (**⌘ + \\** on Mac) in a field | The menu opens |
| Open a sign-up or change-password form | The first suggestion is **Use a strong password**. Pick it to fill both password boxes; it's also copied to the clipboard |
| Open a 2-step verification page whose login has an authenticator | The one-time code for that login is offered and filled |
| Open a checkout form | Your saved cards are offered, and number, name, expiry and CVC are filled |
| Sign in on a site you haven't saved | **"Save login to Vault Guard?"** in the top-right corner: edit the name and click **Save**. It appears in the app's vault |
| Sign in again with a different password | **"Update password?"**: **Update** changes the saved password (the old one goes into password history) |
| Choose **Never for this site** | That site isn't asked again (reset it in the popup's Settings → Saving logins) |
| Lock the app, then click a field | "Vault Guard is locked…" with **I've unlocked it** |
| Quit the app, then click a field | "Open Vault Guard on this computer…" |

Things to know:

- **Nothing fills by itself.** Autofill happens only when you pick something.
- **Hidden fields are never filled.** This protects you from pages that hide extra inputs to
  harvest passwords.
- **The menu is isolated from the page.** It's drawn in an isolated layer that the page's own
  scripts and styles can't read or restyle, and it doesn't change the page's HTML.
- **Passwords go only to the matching site.** The app checks the site against the login's
  saved URL before it decrypts anything.

The repo also has two local test pages you can open straight from disk (`file://`):
`test-page.html` and `test-context-detection.html`. For `file://` pages, Chrome needs
**Allow access to file URLs** turned on in the extension's **Details**.

---

## Step 5: Reload after you change something

| You changed… | Do this |
|---|---|
| `autofill.js`, `content.js`, CSS | Click **↻ reload** on the extension's card, then refresh the web page |
| `background.js`, `manifest.json` | Click **↻ reload** on the card. If the change doesn't show up, remove the extension and load it again (Chrome sometimes keeps the old background worker) |
| `popup.*` | Close and reopen the popup |
| The helper or app (C#) | Quit the app, rebuild, and start it again. The registration updates itself |

To see logs:

- **Page script:** open DevTools on the page; messages start with `Vault Guard:`.
- **Background:** click **service worker** on the extension card.
- **Popup:** right-click the popup and choose **Inspect**.
- **App and helper:** `%LOCALAPPDATA%\VaultGuard\logs\<year>\<month>\<day>.txt`.

---

## Troubleshooting

**The popup says "Browser link helper not installed"**

- Start the app once. It registers the helper on every start.
- Check that `BrowserSupport\VaultGuard.BrowserExtension.NativeHost.exe` exists next to the
  app's exe. If not, rebuild; the helper is built by `build\BrowserSupport.targets`.
- Check the extension ID is `niapolneagndnohemlljncmndahkoncp` (see Step 2).
- Close every window of the browser and open it again. Browsers read the registration when
  they start.

**The popup says "Vault Guard isn't running"**

The helper is fine, but the app isn't running, or it's running as a different Windows user.
Start the app as yourself, not with "Run as administrator".

**Connect does nothing, or says the connection wasn't approved**

Look for the **Allow** dialog in the app; it may be behind other windows. It times out after
two minutes.

**No padlock appears in a field**

- Reload the extension and refresh the page.
- The icon appears only on visible username, email, password, one-time-code and card fields,
  and on at most 24 fields per page.
- Some sites draw their sign-in form inside an iframe from another domain. The menu runs on the
  top page only, so use the toolbar popup there.

**The menu says "No saved logins for <site>"**

The website URL on your Login must match the site's domain. Subdomains count, so
`example.com` matches `login.example.com`. Edit the Login in the app and fix its URL.

**It worked, then stopped after rebuilding the app**

Restart the app so it re-registers the new helper path, then reload the extension.

**Firefox: the add-on disappeared**

Temporary add-ons are removed when Firefox restarts. Load it again from `about:debugging`.

---

## Why no certificate or store is needed

- **Unpacked extensions don't need signing or a store.** Browsers trust them because you load
  them yourself, with Developer mode on.
- **Signing is only for distribution.** The Chrome Web Store, Edge Add-ons and Firefox AMO sign
  the extension when it's published. Your users never install a certificate; they just install
  the extension from the store.
- **The desktop app's code-signing certificate is a separate matter.** It only affects
  Windows SmartScreen when someone downloads the installer. It isn't needed for local testing.
