# Single Sign-On (Google)

VaultGuard supports "Continue with Google" on the login screen of both the **Blazor web app** and the
**WPF desktop app**. Read the security model below before configuring it - SSO in VaultGuard is not what
it is in most apps.

## One important truth about a zero-knowledge vault

VaultGuard never sees your master password on a server, and no server ever stores the key that decrypts
your vault - that key is **derived from the master password itself** (`AuthService.LoginAsync` ->
`IVaultSessionService.InitializeSession`). Google (or any identity provider) can prove *who you are*, but
it can never supply that derived key, because it never had it.

So "Sign in with Google" in VaultGuard does exactly one thing: it verifies your Google account's email
address and uses it to **jump straight to your existing profile** on the login screen's profile picker,
skipping the "which account is mine?" step. It does **not** log you in, and the master-password step that
follows is unchanged. If no local profile matches the verified email, you're told so and nothing happens -
SSO never creates an account or bypasses the master password.

This is why the feature needs no new database tables, no token storage tied to a session, and no changes
to `AuthService`/`IVaultSessionService` at all - it's purely a shortcut into the picker that already exists.

## Two separate Google OAuth clients are required

The web app and the desktop app use different OAuth flows, so they need **two different OAuth client
registrations** in the same (or different) Google Cloud project:

| | Blazor Web | WPF Desktop |
| --- | --- | --- |
| Google Cloud client type | **Web application** | **Desktop app** |
| Flow | Server-side authorization code (ASP.NET Core's built-in Google handler) | Loopback redirect + PKCE (RFC 8252), hand-rolled with `HttpListener` - no third-party OIDC library |
| Needs a registered redirect URI? | Yes - exact HTTPS URL | No - Google accepts any `http://127.0.0.1:<port>` for this client type |
| Config file | `VaultGuard.Web/appsettings.json` -> `Sso` section | `VaultGuard.WPF/appsettings.json` -> `Sso` section |
| Config keys | `GoogleClientId`, `GoogleClientSecret` | `GoogleDesktopClientId`, `GoogleDesktopClientSecret` |

Both are optional and independent - you can enable one, both, or neither. The "Continue with Google"
button is hidden on a given platform until that platform's pair of values is filled in.

## What you (the deploying organization) need to do

### 1. Create a Google Cloud project (or reuse one)

Go to [Google Cloud Console](https://console.cloud.google.com/apis/credentials) and select or create a
project. You'll configure the OAuth consent screen once - it's shared by both client registrations below.

### 2. Set up the OAuth consent screen

- User type: **External** (unless every user is inside your Google Workspace organization, in which case
  **Internal** is simpler and skips Google's verification review).
- Scopes: the app only ever requests `openid email profile` - no sensitive or restricted scopes, so it
  qualifies for Google's lightweight verification tier.
- Add your support email and, if using **External**, submit for verification once you're ready for
  production (unverified apps work fine for internal testing with added test users).

### 3. Create the Web application client (for the Blazor app)

1. **Credentials -> Create Credentials -> OAuth client ID -> Web application.**
2. **Authorized redirect URI:** `https://<your-domain>/signin-google` - this is ASP.NET Core's default
   Google callback path (`GoogleOptions.CallbackPath`), not overridden in `Program.cs`. It must be the
   exact HTTPS origin the app is deployed at; `http://localhost:5000/signin-google` for local dev if
   you're testing without HTTPS.
3. Copy the **Client ID** and **Client secret** into `VaultGuard.Web/appsettings.json`:

   ```json
   "Sso": {
     "GoogleClientId": "xxxxxxxxxx.apps.googleusercontent.com",
     "GoogleClientSecret": "GOCSPX-xxxxxxxxxxxxxxxxxxxxxxxx"
   }
   ```

   (Or via environment variables / secrets manager - see `CONFIGURATION_GUIDE.md`'s "Secrets management"
   section - as `Sso__GoogleClientId` / `Sso__GoogleClientSecret`.)

### 4. Create the Desktop app client (for the WPF app)

1. **Credentials -> Create Credentials -> OAuth client ID -> Desktop app.**
2. No redirect URI to register - Google's loopback exemption for this client type accepts any
   `http://127.0.0.1:<port>` the app happens to bind at sign-in time.
3. Copy the **Client ID** and **Client secret** into `VaultGuard.WPF/appsettings.json`:

   ```json
   "Sso": {
     "GoogleDesktopClientId": "xxxxxxxxxx.apps.googleusercontent.com",
     "GoogleDesktopClientSecret": "GOCSPX-xxxxxxxxxxxxxxxxxxxxxxxx"
   }
   ```

   Yes, Google issues a client secret for "Desktop app" clients too, even though it's a public client by
   RFC 8252's definition - Google's token endpoint still expects it in the exchange, so it must ship with
   the app rather than being treated as truly confidential. Don't reuse the Web application client's
   secret here; it will not work (redirect URI validation differs by client type).

### 5. Restart the app(s)

Both apps read the `Sso` section at startup. Once both `GoogleClientId`/`GoogleClientSecret` (Web) or
`GoogleDesktopClientId`/`GoogleDesktopClientSecret` (WPF) are non-empty, the "Continue with Google" button
appears automatically on that platform's login screen - no other flag to flip.

## How it behaves for end users

1. User clicks **Continue with Google** on the profile picker.
2. Their default browser opens Google's sign-in/consent page (system browser on WPF; same-tab redirect
   on web).
3. On success, VaultGuard reads the verified email from Google's response.
4. If a local profile's email matches, the picker jumps straight to that profile's master-password entry
   (exactly as if the user had clicked that profile's tile) - `ShowLockMessage`/2FA-quick-unlock/Windows
   Hello evaluation all run unchanged from that point.
5. If no profile matches, the user sees an error naming the Google email and is returned to the picker.
   No account is created.
6. The user still enters their master password (or completes 2FA/quick-unlock) exactly as before - Google
   involvement ends at step 4.

## Implementation notes

- **Blazor**: uses ASP.NET Core's built-in `Microsoft.AspNetCore.Authentication.Google` handler, signed in
  under `IdentityConstants.ExternalScheme` (isolated from the app's real authentication cookie) so the
  challenge/callback can never accidentally establish a real session. `GET /login/google` starts the
  challenge; `GET /login/google-callback` reads the email claim, signs the external principal back out
  immediately, and redirects to `/login?ssoEmail=...` (or `?ssoError=1`) for `Login.razor` to consume.
- **WPF**: `GoogleSsoService` (`VaultGuard.Services/Services/GoogleSsoService.cs`) hand-rolls the OAuth2 +
  PKCE loopback flow using only BCL APIs (`System.Net.HttpListener`, `System.Diagnostics.Process`,
  `System.Security.Cryptography.SHA256`) - the same pattern `OneDriveBackupService` already uses for
  OneDrive sign-in - so no third-party OIDC package is required. It reads the `email`/`email_verified`
  claims out of the returned `id_token`'s JWT payload without verifying the signature, since the result is
  only ever used to pick a profile, never to authenticate.
- Neither platform stores a Google refresh token or keeps the user signed into Google within VaultGuard;
  every click re-runs the browser flow.

## Troubleshooting

| Symptom | Likely cause |
| --- | --- |
| "Continue with Google" button doesn't appear | `Sso` section for that platform still has empty values - check `appsettings.json` (or the env var override) and restart the app |
| Google shows `redirect_uri_mismatch` (Web) | The registered redirect URI doesn't exactly match `https://<your-domain>/signin-google`, including scheme and trailing slash |
| WPF: "Could not start the local sign-in listener" | Another process is holding the chosen loopback port, or the OS is blocking `HttpListener` from binding - rare, since a fresh port is picked per attempt |
| "No local profile matches the Google account ..." | Expected when the Google email doesn't match any existing VaultGuard profile's email - SSO does not create accounts; add/edit the matching profile first |
| Sign-in "times out waiting for the browser to redirect back" | User closed the browser tab or didn't complete Google's consent screen within 5 minutes |

## Related documentation

- [`AUTHENTICATION.md`](AUTHENTICATION.md) - master-key login, 2FA, passkeys, and the rest of the account
  security surface SSO plugs into.
- [`PASSKEYS.md`](PASSKEYS.md) - a similar "identity proof, not a decryption key" pattern already used for
  device passkeys/Windows Hello quick unlock; useful background for the same zero-knowledge reasoning.
- [`CONFIGURATION_GUIDE.md`](CONFIGURATION_GUIDE.md) - secrets management (Google Secret Manager, env vars,
  user-secrets) for keeping `GoogleClientSecret`/`GoogleDesktopClientSecret` out of source control.
