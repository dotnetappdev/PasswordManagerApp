# Vault Guard Browser Extension

Fills your Vault Guard logins, one-time codes and cards into web pages, and suggests strong
passwords on sign-up forms. It talks to the **Vault Guard desktop app** (WPF or MAUI on Windows)
over a local, paired link. Your vault stays in the app and is decrypted there, only for the site
you're on.

> **Installing it?** Follow **[INSTALLATION.md](INSTALLATION.md)**. Vault Guard is free forever: every feature
> is included, with no subscription, payments or paid tier.
>
> **Want to try it right now?** Follow **[TESTING_LOCALLY.md](TESTING_LOCALLY.md)**. You load the
> extension unpacked: no store, no certificate, about five minutes.

## Documentation

- **[INSTALLATION.md](INSTALLATION.md)**: installing it for everyday use (release zip, each browser,
  connecting, updating, troubleshooting).
- **[TESTING_LOCALLY.md](TESTING_LOCALLY.md)**: run and test the extension on your own machine,
  step by step, with troubleshooting.
- **[../docs/BROWSER_LINK.md](../docs/BROWSER_LINK.md)**: how the app ↔ browser link works
  (pairing, the helper, registration, security).
- **[PACKAGING.md](PACKAGING.md)**: packaging for the Chrome Web Store, Edge Add-ons and
  Firefox AMO.
- **[HELP.md](HELP.md)**,
  **[CONTEXT_DETECTION.md](CONTEXT_DETECTION.md)**, **[PASSKEYS_SETUP.md](PASSKEYS_SETUP.md)**:
  older, more detailed notes.

## Features

- **Padlock icon in form fields.** Appears on sign-in, sign-up, one-time-code and card fields.
  Click it, focus the field, or press **Ctrl + \\** (**⌘ + \\** on Mac) to open the Vault Guard
  menu.
- **Suggestions menu.** Lists your logins for the current site with coloured initials. Use
  ↑ / ↓ and Enter, or click. Esc closes it. Light and dark mode follow your system.
- **"Save this login?"** After you sign in or sign up, a small card in the corner offers to save the login
  to Vault Guard (with a name you can edit), to **update** the saved password if it changed, or to never ask
  on that site again. It also works on single-page apps that don't navigate after signing in. Turn it off,
  or reset the "never" list, in the popup's Settings.
- **Phishing guard.** When you pause typing in a sign-in password field, the extension asks the app whether that
  password is saved for a *different* site. If so (your bank password on a look-alike site) it warns you before you
  sign in, with **Leave this site**, **Clear password** or **I trust this site**. The app answers with login titles
  and sites only, never passwords; values Vault Guard filled itself and sign-up fields aren't checked. Turn it off
  in the popup's Settings → Filling & saving.
- **One-time code copied after filling.** Filling a login that has an authenticator (from the in-page menu or
  the popup) copies its current 6-digit code, ready to paste on the 2-step page. Turn it off in the popup's
  Settings → Filling & saving.
- **Strong password suggestion** on sign-up and change-password forms. It fills both the
  "new" and "confirm" boxes.
- **One-time codes** from logins that have an authenticator set up.
- **Cards** at checkout: number, name, expiry and CVC.
- **Authenticator QR detection.** When a site shows a 2FA setup QR code, the extension offers to
  save it to the matching login.
- **Passkeys in your vault.** Sites that support passkeys can save them in Vault Guard, and you sign in with them from any connected browser. Vault Guard asks first, in its own card, and only signs for the site the passkey belongs to (see PASSKEYS_SETUP.md).
- **Sign in after filling.** After you pick a login, the extension presses the site's sign-in button for you,
  including username-then-password sign-ins. It holds back when the form has a captcha or another required field.
  Turn it off in Settings → **Sign in after filling**.
- **Open & fill.** In the popup, a login for another site opens that site and signs you in.
- **Email aliases.** On sign-up forms, "Use a new email alias" creates a unique address for the site with your
  SimpleLogin or addy.io account and fills it in. Connect the account in the Vault Guard app: Settings → Email
  aliases. The app talks to the alias service; the extension only gets the new address.
- **Toolbar badge.** The Vault Guard button shows how many logins you have for the site you're on.
- **Keyboard shortcuts.** **Ctrl + Shift + L** fills this site's login and signs in (or shows the menu when you
  have several). **Ctrl + Shift + G** fills a strong new password in the selected field. **Ctrl + Shift + Y**
  opens the popup. On a Mac use ⌘ instead of Ctrl. Change them at `chrome://extensions/shortcuts` (Edge:
  `edge://extensions/shortcuts`, Brave: `brave://extensions/shortcuts`), or in Firefox under Add-ons → ⚙ →
  **Manage Extension Shortcuts**.
- **Right-click menu.** On any text field: **Vault Guard → Fill login / Fill a strong new password / Fill card**,
  handy on forms the padlock doesn't recognise.
- **Popup.** Connection status, search across your whole vault, quick copy buttons (username, password, and
  one-time code for the site you're on), generator and settings.

### Safe-by-default filling

- **Saving never exposes the password to the page.** A submitted login is held by the extension's background
  in memory-only session storage for at most two minutes and dropped once you answer; the prompt only shows
  the username.
- **Nothing fills on page load.** Filling happens only when you choose an item.
- **Only visible fields are filled.** Hidden "harvesting" inputs get nothing.
- **Passwords go only to the matching site.** The app checks the site against the login's saved
  website before decrypting, and the popup's vault browser never receives passwords.
- **The page can't see or change the menu.** The icons and menu are drawn in a closed Shadow DOM
  layer. Page scripts can't read it, page CSS can't restyle it, and the page's own HTML is never
  modified.
- **The link is paired and per-user.** It only works for your Windows user, and the browser must
  be approved once in the app.

## How it works

```
 web page ──► autofill.js (field icons + menu)      popup.html/js
                     │ chrome.runtime.sendMessage        │
                     └──────────► background.js ◄────────┘
                                     │ native messaging (stdio)
                                     ▼
                VaultGuard.BrowserExtension.NativeHost.exe   ← shipped in the app's BrowserSupport\ folder
                                     │ named pipe (current user only)
                                     ▼
                    Vault Guard app (WPF / MAUI): pairing check → unlocked vault → answer
```

- The app registers the helper with Chrome, Edge, Brave, Chromium, Vivaldi and Firefox
  **every time it starts**, for your user only, so it keeps working after updates or moves.
- The extension's ID is fixed (`niapolneagndnohemlljncmndahkoncp`) by the `key` in
  `manifest.json`. Firefox uses `vaultguard@dotnetappdevni.com`.

## Files

| File | Purpose |
|---|---|
| `manifest.json` | Manifest V3; fixed `key`; `nativeMessaging` permission |
| `background.js` | Service worker: link status, pairing, routes requests to the helper |
| `autofill.js` | In-page field icons, suggestions menu, filling (Shadow DOM) |
| `content.js` | Popup fill handlers, authenticator-QR detector, passkey bridge |
| `inpage.js` | Passkey (WebAuthn) page hook |
| `jsQR.js` | QR decoding for authenticator setup |
| `popup.html` / `popup.js` / `popup.css` | Toolbar popup |
| `icons/` | `icon.svg` master plus 16/32/48/128 PNGs |
| `test-page.html`, `test-context-detection.html` | Local test forms |

## Browser compatibility

| Browser | Support |
|---|---|
| Chrome, Edge, Brave, Chromium, Vivaldi | Recent versions (Manifest V3) |
| Firefox | 128+ (the extension uses Firefox's background scripts; Chrome and Brave use the service worker from the same manifest) |
| Safari | Not supported (it uses a different extension system) |

## Contributing

Test changes against real sign-in, sign-up, 2-step and checkout pages, in both light and dark
mode, before opening a pull request. TESTING_LOCALLY.md explains how to reload the extension and
where to find the logs.
