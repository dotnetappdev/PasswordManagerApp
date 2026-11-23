# Passkeys with Vault Guard

Vault Guard can keep website passkeys in your vault, the way 1Password does. When a site offers to create a
passkey, Vault Guard can save it; when you sign in, Vault Guard signs in with it. Because the passkey lives in your
vault, it works in every browser you connect to Vault Guard, not just the one you created it in.

## Using it

1. Open the Vault Guard app and unlock it, and connect the browser (popup → **Connect to Vault Guard app**).
2. On a site that supports passkeys, start "create a passkey". Vault Guard asks:
   **"Save a passkey in Vault Guard?"** → **Save in Vault Guard**, or **Use another device** for the browser's
   own passkey options (Windows Hello, your phone, a security key).
3. Next time you sign in, Vault Guard asks **"Sign in with a passkey?"** and names the site and account.
   Click **Sign in**.

Vault Guard only offers itself when it has a passkey for that site; otherwise the browser's own passkey window
appears as usual. Turn the feature off in the popup's **Settings → Passkeys**.

## How it works

```
website ─ navigator.credentials ─► inpage.js ─► content.js ─► background.js ─(encrypted link)─► Vault Guard app
                                               asks you first    adds the tab's real site       creates / signs
```

- **The app is the authenticator.** It makes a P-256 key pair for the site, stores the private key encrypted with
  your vault key, and signs sign-in challenges with it (ES256). The private key never leaves the app.
- **The site is checked twice.** The background script adds the site of the tab the request came from (the page
  can't choose it). The app only creates or signs for a passkey whose site (rpId) is that site or a parent domain
  of it (never a public suffix such as `co.uk`), and only signs when the page's signed data names that same
  `https://` origin. A look-alike site gets nothing.
- **You approve each use.** The question is shown in Vault Guard's own card, which the website can't see or click.
- **Everything travels over the encrypted browser link** (see [../docs/BROWSER_LINK.md](../docs/BROWSER_LINK.md)).

## API server mode

If the extension is set to use a Vault Guard API server instead of the app, passkeys use the server's
`/api/passkey/vault/create` and `/api/passkey/vault/assert` endpoints.
