# Installing the Vault Guard browser extension

Vault Guard is **free forever**: every feature is included for everyone, with no subscription, no payments and
no paid tier. The
browser extension works with the **Vault Guard desktop app** on Windows (the WPF app or the Windows MAUI app).
Your passwords stay in the app, on your PC. The extension asks the app for the one login it needs, for the site
you're on.

This guide covers everyday installation. If you're developing the extension, see
[TESTING_LOCALLY.md](TESTING_LOCALLY.md), which also explains reloading after changes.

---

## Before you start

| | |
|---|---|
| **Windows** | Windows 10 or 11 |
| **Vault Guard app** | The Vault Guard desktop app (WPF), or the Vault Guard Windows app (MAUI), installed |
| **Browser** | Google Chrome, Microsoft Edge, Brave, Vivaldi or Chromium (any recent version), or Firefox 115+ |

You don't need administrator rights or any certificate. You don't edit the registry or any configuration file.

---

## Step 1: Install Vault Guard and open it once

1. Install the Vault Guard app from the [latest release](https://github.com/dotnetappdev/PasswordManagerApp/releases/latest).
2. Open it and sign in (or create your vault).

Each time Vault Guard starts, it registers a small helper that lets browsers talk to it, for your Windows user
only. That's why the app needs to have been opened once before the extension can connect.

---

## Step 2: Add the extension to your browser

### Get the extension

Download **VaultGuardBrowserExtension-*version*.zip** from the
[latest release](https://github.com/dotnetappdev/PasswordManagerApp/releases/latest) and unzip it to a folder
you'll keep, for example `Documents\Vault Guard extension`. Don't delete the folder afterwards: the browser
loads the extension from it.

> Store listings (Chrome Web Store, Edge Add-ons, Firefox Add-ons) are planned. When they're available you'll
> be able to click **Add to browser** instead, and updates will be automatic. The steps after this section stay
> the same.

### Chrome, Edge, Brave, Vivaldi or Chromium

1. Open the browser's extensions page:

   | Browser | Address |
   |---|---|
   | Chrome | `chrome://extensions` |
   | Edge | `edge://extensions` |
   | Brave | `brave://extensions` |
   | Vivaldi | `vivaldi://extensions` |

2. Turn on **Developer mode**. In Chrome it's the switch at the top right; in Edge it's in the left sidebar.
3. Click **Load unpacked** and choose the folder you unzipped: the one that contains `manifest.json`.
4. Vault Guard appears in the list. Its ID should be `niapolneagndnohemlljncmndahkoncp`.
5. Click the puzzle-piece icon in the toolbar and pin **Vault Guard**, so its button is always visible.

Some browsers show a "developer mode extensions" notice at start-up while the extension isn't installed from
a store. That's expected; click **Keep** or close the notice.

### Firefox

1. Open `about:debugging#/runtime/this-firefox`.
2. Click **Load Temporary Add-on…** and choose `manifest.json` in the folder you unzipped.

Firefox removes add-ons loaded this way when it closes, so you'll need to repeat this after restarting Firefox
until the Firefox Add-ons listing is available.

---

## Step 3: Connect the extension to Vault Guard

1. Make sure the Vault Guard app is open and unlocked.
2. Click the **Vault Guard** button in the browser toolbar.
3. Click **Connect to Vault Guard app**.
4. The app asks **"Allow this browser to use Vault Guard?"**. Click **Allow**.

That's it. You only connect once per browser. To disconnect later: extension **Settings → Vault Guard app →
Disconnect this browser**.

---

## Using it

| What you do | What happens |
|---|---|
| Click into a sign-in form | A small blue padlock appears in the fields; the Vault Guard menu lists your logins for that site. Pick one (or use ↑ ↓ and Enter) to fill it. |
| **Ctrl + \\** in a field | Opens the menu. |
| Sign up for a new account | The first suggestion is **Use a strong password**, which fills both password boxes. |
| Sign in or sign up with a login Vault Guard doesn't have | **"Save login to Vault Guard?"**: click **Save**. If you changed a password, it offers **Update**. |
| Fill a login that has an authenticator | Its one-time code is copied, ready to paste on the 2-step page. |
| Type a saved password on a site it doesn't belong to | **"Is this the right site?"** warns you before you sign in: a common sign of a fake site. |
| Checkout forms | Your saved cards are offered. |

Nothing is filled until you choose it, and hidden fields are never filled.

### Settings

Click the toolbar button, then **Settings**:

- **Filling & saving**:
  - Warn me if I type a saved password on the wrong site.
  - Copy one-time code after filling.
  - Offer to save logins. This is also where you can reset the sites you chose "Never" for.
- **Passkeys**: whether Vault Guard saves website passkeys in your vault.

---

## Updating the extension

When a new Vault Guard release comes out:

1. Download the new **VaultGuardBrowserExtension** zip.
2. Unzip it over the same folder, replacing the files.
3. Open the browser's extensions page and click **↻ Reload** on Vault Guard (in Firefox, load it again from
   `about:debugging`).

Your connection to the app and your settings are kept.

## Removing the extension

To stop a browser using Vault Guard without removing the extension, open the extension's **Settings → Vault
Guard app → Disconnect this browser**. To remove it completely, click **Remove** on Vault Guard on the browser's
extensions page. Neither touches your vault.

---

## Troubleshooting

| Problem | What to do |
|---|---|
| The popup says **"Browser link helper not installed"** | Open the Vault Guard app once (it registers the helper when it starts), then close **every** window of the browser and open it again. |
| The popup says **"Vault Guard isn't running"** | Start the Vault Guard app. Run it as yourself, not with "Run as administrator". |
| **Connect** doesn't finish | Look for the **Allow** dialog in the Vault Guard app (it may be behind other windows). It waits for two minutes. |
| **"Vault Guard is locked"** | Unlock the app, then click **I've unlocked it**. |
| No padlock in the fields | Refresh the page. Some sites put their sign-in form inside a frame from another site; use the toolbar button there instead. |
| **"No saved logins for this site"** | The login's website in Vault Guard must match the site (subdomains count: `example.com` also covers `login.example.com`). Edit the login's website in the app. |
| The extension's ID isn't `niapolneagndnohemlljncmndahkoncp` | You loaded a different folder, or `manifest.json` was edited. Load the folder from the release zip. |
| The extension vanished from Firefox | Firefox removes temporary add-ons on restart; load it again (see above). |

Still stuck? The app's log is in `%LOCALAPPDATA%\VaultGuard\logs`. Please include it when reporting a problem.

---

## Privacy and security

- **The extension holds no vault.** It asks the app for the login it needs, for the site you're on. Passwords are
  only decrypted for a site that matches the saved website.
- **The link is local and private.** It works only on your PC, for your Windows user, and only after you
  approve the browser in the app. The browser keeps a random pairing secret; the app stores only a hash of it.
- **When Vault Guard locks, the browser can't fill until you unlock.** The app locks after the idle time you set,
  and when Windows locks or sleeps.
- **What the page can and can't see.** The in-page menu and prompts are isolated from the website, which can't
  read or change them. A login you're asked to save is held in memory for at most two minutes and never written
  to disk.

More detail: [docs/BROWSER_LINK.md](../docs/BROWSER_LINK.md).
