# Single Sign-On (any OIDC identity provider)

VaultGuard supports "Continue with &lt;provider&gt;" buttons on the login screen of both the **Blazor web
app** and the **WPF desktop app**. This is a **pluggable, config-driven system, never a hardcoded
vendor** - you can wire up Google, Microsoft Entra ID (Azure AD), a modern on-prem ADFS instance, Okta,
Auth0, Keycloak, or any other OIDC-compliant identity provider by editing configuration only. Adding or
removing a provider never requires a code change or a new NuGet dependency.

Read the security model below before configuring it - SSO in VaultGuard is not what it is in most apps.

## One important truth about a zero-knowledge vault

VaultGuard never sees your master password on a server, and no server ever stores the key that decrypts
your vault - that key is **derived from the master password itself** (`AuthService.LoginAsync` ->
`IVaultSessionService.InitializeSession`). An identity provider can prove *who you are*, but it can never
supply that derived key, because it never had it.

So "Sign in with &lt;provider&gt;" in VaultGuard does exactly one thing: it verifies your identity at that
provider and uses it to **jump straight to your existing profile** on the login screen's profile picker,
skipping the "which account is mine?" step. It does **not** log you in, and the master-password step that
follows is unchanged. If no local profile matches, you're told so and nothing happens - SSO never creates
an account or bypasses the master password.

This is why the feature needs no changes to `AuthService`/`IVaultSessionService` at all - it's purely a
shortcut into the picker that already exists, and it's why the provider list can grow or shrink freely
without touching that core logic.

### How your local profile gets linked to the external identity

The first time you use a given provider, VaultGuard matches you to a local profile by comparing the
verified email address - there's nothing else to go on yet. Once you finish that sign-in by entering your
master password (proving you actually own the account, not just that email), VaultGuard persists a link
between your local account and that provider's stable subject ("sub") claim, using ASP.NET Identity's
built-in `AspNetUserLogins` table (`IUserProfileService.LinkExternalLoginAsync`, called from
`Login.razor`/`LoginViewModel` right after a successful master-password check - never before). This table
already exists in the schema (SQLite creates it automatically via `EnsureCreatedAsync` since
`VaultGuardDbContext` derives from `IdentityDbContext`; it's also already in the SQL Server migration) -
**no new migration was needed**, only actually using the table.

Every sign-in after that looks up the link first (`IUserProfileService.FindByExternalLoginAsync`) and only
falls back to matching by email if no link exists yet - so the picker keeps resolving correctly even if
your email later changes at the IdP or locally. The link is only ever created after a real master-password
proof, so SSO identity alone (e.g. an attacker who merely knows your email and controls *their own*
account at some IdP) can never hijack it.

## Why this is provider-agnostic, not "Google support"

Every entry under `Sso:Providers` is described the same way: a display name, an `Authority` (the IdP's
OIDC issuer URL), and a client registration. At startup/sign-in time the app fetches that Authority's
standard discovery document - `{Authority}/.well-known/openid-configuration` - to learn the real
authorization and token endpoints. That single discovery step is the entire integration surface; it's why
one code path (`OidcSsoService` on WPF, ASP.NET Core's built-in `AddOpenIdConnect` on Blazor) covers every
IdP without a per-vendor branch. You can run any number of providers side by side (e.g. "Continue with
Google" **and** "Continue with Company SSO" on the same login screen).

**What this does *not* cover today: SAML / WS-Federation.** A handful of enterprise setups - typically an
older, on-prem-only ADFS deployment that has never had OIDC enabled - only speak SAML. VaultGuard doesn't
implement a SAML service-provider stack yet; see [SAML / classic ADFS](#saml--classic-adfs-not-yet-implemented)
below for what that would take and why it's a separate piece of work. Modern ADFS (2016+) and every
mainstream identity platform (Entra ID, Okta, Auth0, Keycloak, PingFederate, Google) all support OIDC, so
this covers the overwhelming majority of real deployments.

## Two independent provider lists (Web vs. Desktop)

The web app and the desktop app keep **separate** `Sso:Providers` lists, each in that app's own
`appsettings.json`, because most IdPs require a different client registration per app type even for the
"same" IdP:

| | Blazor Web | WPF Desktop |
| --- | --- | --- |
| Client type at the IdP | Confidential **web application** client | Public **native/desktop** client |
| Flow | Server-side authorization code (ASP.NET Core's `AddOpenIdConnect`, one scheme per configured provider) | Loopback redirect + PKCE (RFC 8252), hand-rolled with `HttpListener` in `OidcSsoService` - no third-party OIDC library |
| Needs a registered redirect URI? | Yes - exact HTTPS URL, one per provider: `https://<your-domain>/signin-oidc/<Id>` | Usually no - most IdPs accept any `http://127.0.0.1:<port>` for a native client type |
| Config file | `VaultGuard.Web/appsettings.json` -> `Sso:Providers` | `VaultGuard.WPF/appsettings.json` -> `Sso:Providers` |

Configure zero, one, or many providers on each platform independently - the login screen shows exactly the
buttons for whatever's configured on that platform, nothing hardcoded.

## Provider configuration fields

Each entry in `Sso:Providers` (same shape on both platforms):

| Field | Required | Meaning |
| --- | --- | --- |
| `Id` | Yes | Stable slug (`"google"`, `"azuread"`, `"keycloak"`, `"adfs"`, ...). Used as the ASP.NET Core auth scheme name (web) and the lookup key (desktop), and as the redirect-path segment on web (`/signin-oidc/<Id>`) - **don't change it once real users have signed in with it.** |
| `DisplayName` | Yes | Shown on the button: "Continue with `<DisplayName>`". |
| `Enabled` | No (default `true`) | Set `false` to keep an entry in config but hide its button, without deleting the entry. |
| `Authority` | Yes* | The IdP's OIDC issuer URL, e.g. `https://accounts.google.com`, `https://login.microsoftonline.com/<tenant>/v2.0`, `https://keycloak.example.com/realms/vaultguard`, `https://adfs.example.com/adfs`. |
| `ClientId` | Yes | The OAuth client ID from that IdP's app registration. |
| `ClientSecret` | No | Confidential clients (typical web registrations, and Google's desktop client type despite being technically public) need this. Public native clients that rely on PKCE alone (common for Entra ID/Keycloak desktop registrations) can leave it empty - it's simply omitted from the token exchange when unset. |
| `Scopes` | No (default `"openid email profile"`) | Space-separated OIDC scopes. |
| `AuthorizationEndpointOverride` / `TokenEndpointOverride` | No | Escape hatch for an IdP with a missing/non-standard discovery document. Leave unset for the normal, discovery-based path. |

\* Or set both endpoint overrides instead, for the rare IdP without usable discovery.

An entry is only surfaced (and only wired into the auth pipeline on web) once it has `Id` + `ClientId` +
(`Authority` or both endpoint overrides) - identical to how empty Sentry/Fido2 config sections hide those
features elsewhere in this app.

## Setting up a provider

The steps are the same shape for every IdP - only the specific console/portal differs:

1. **Register an OAuth/OIDC client for the app.**
   - Web: a **confidential web application** client, redirect URI `https://<your-domain>/signin-oidc/<Id>`.
   - Desktop: a **public native/desktop** client. Most IdPs need no redirect URI registered (they accept
     any loopback port); a few (Google) still require the client to have been created with that specific
     client type even though no URI is registered.
2. Note the **Authority** (issuer URL) - check the IdP's docs for its exact discovery document location if
   unsure; it should resolve at `<Authority>/.well-known/openid-configuration`.
3. Add an entry to `Sso:Providers` in the relevant `appsettings.json` with `Id`, `DisplayName`, `Authority`,
   `ClientId`, and `ClientSecret` if the IdP issued one.
4. Restart the app. The button appears automatically - no other flag to flip.

### Provider notes

- **Google**: `Authority` = `https://accounts.google.com`. Requires a **Web application** client (with the
  redirect URI above) for the web app, and a separate **Desktop app** client for WPF - Google is the one
  mainstream IdP that still issues (and requires) a client secret for its "Desktop app" type even though
  it's a public client by RFC 8252's definition.
- **Microsoft Entra ID / Azure AD**: `Authority` = `https://login.microsoftonline.com/<tenant-id-or-domain>/v2.0`.
  Register a **Web** platform app for the web app (client secret required) and a **Mobile and desktop
  applications** platform app for WPF (public client, no secret, PKCE-only - leave `ClientSecret` empty).
- **ADFS (2016+, OIDC mode)**: `Authority` = `https://<adfs-host>/adfs`. Register an application group with
  a web app (confidential) and/or native app (public) client as above. Requires OIDC to be enabled on the
  ADFS farm - see [SAML / classic ADFS](#saml--classic-adfs-not-yet-implemented) if it isn't.
- **Keycloak**: `Authority` = `https://<keycloak-host>/realms/<realm>`. Create a confidential client for
  web and a public client (with "Standard flow" + PKCE) for desktop.
- **Okta / Auth0 / PingFederate / any other OIDC IdP**: same shape - `Authority` from that IdP's
  application settings, `ClientId`/`ClientSecret` from the client registration.

## How it behaves for end users

1. User clicks **Continue with &lt;provider&gt;** on the profile picker.
2. Their browser opens that provider's sign-in/consent page (system browser on WPF; same-tab redirect on
   web).
3. On success, VaultGuard reads the verified email from the provider's response.
4. If a local profile's email matches, the picker jumps straight to that profile's master-password entry
   (exactly as if the user had clicked that profile's tile) - `ShowLockMessage`/2FA-quick-unlock/Windows
   Hello evaluation all run unchanged from that point.
5. If no profile matches, the user sees an error naming the verified email and is returned to the picker.
   No account is created.
6. The user still enters their master password (or completes 2FA/quick-unlock) exactly as before - the
   identity provider's involvement ends at step 4.

## Implementation notes

- **Blazor**: for every configured provider, `Program.cs` registers a separate ASP.NET Core
  `AddOpenIdConnect(provider.Id, provider.DisplayName, ...)` scheme (`UsePkce = true`, `ClientSecret` set
  only when configured), signed in under `IdentityConstants.ExternalScheme` (isolated from the app's real
  authentication cookie) so a challenge/callback can never accidentally establish a real session.
  `GET /login/sso/{id}` starts the challenge for that provider; `GET /login/sso/{id}/callback` reads the
  email and subject ("sub") claims, signs the external principal back out immediately, and redirects to
  `/login?ssoEmail=...&ssoProvider={id}&ssoSub={sub}` (or `?ssoError=1&ssoProvider={id}`) for `Login.razor`
  to consume.
- **WPF**: `OidcSsoService` (`VaultGuard.Services/Services/OidcSsoService.cs`) hand-rolls the OAuth2 + PKCE
  loopback flow using only BCL APIs (`System.Net.HttpListener`, `System.Diagnostics.Process`,
  `System.Security.Cryptography.SHA256`) - the same pattern `OneDriveBackupService` already uses for
  OneDrive sign-in - so no third-party OIDC package is required. It resolves each provider's real
  authorization/token endpoints via OIDC discovery at sign-in time, then reads the `sub`/`email`/
  `email_verified` claims out of the returned `id_token`'s JWT payload without verifying the signature,
  since the result is only ever used to pick a profile, never to authenticate.
- Neither platform stores a refresh token or keeps the user signed into the IdP within VaultGuard between
  attempts; every click re-runs the browser flow from scratch.
- Adding a new provider is a config-only change on every platform - `SsoConfiguration`/`SsoProviderConfig`
  (`VaultGuard.Models/Configuration/SsoConfiguration.cs`) are the only shared types involved, and neither
  references any specific vendor.

## Web API (`VaultGuard.API`)

The API doesn't run an OIDC challenge/callback itself - that needs a system browser, which a client that
already has one (WPF today; a future native mobile app) is better placed to drive itself, the same loopback
flow `OidcSsoService` already runs. What the API exposes (`SsoController`,
`VaultGuard.API/Controllers/SsoController.cs`) is the provider list and the account-linking management
those clients need around that flow:

| Endpoint | Auth | Purpose |
| --- | --- | --- |
| `GET /api/sso/providers` | Anonymous | Lists configured providers (`Id`, `DisplayName` only - never secrets) so any client can render buttons. |
| `GET /api/sso/links` | Bearer token | Lists the calling user's currently linked external identities. |
| `POST /api/sso/link` | Bearer token | Persists `{ loginProvider, providerKey, providerDisplayName }` via `AspNetUserLogins`. Requires an already-authenticated caller - i.e. only after a real master-password login - for the same reason `Login.razor`/`LoginViewModel` only link after a successful password check. |
| `DELETE /api/sso/link/{provider}` | Bearer token | Removes a linked identity. |

`Sso:Providers` in `VaultGuard.API/appsettings.json` is a separate, independent list from Web's and WPF's
(same reasoning as the two-lists table above) - keep provider `Id`s consistent across all three if you want
"google"/"keycloak"/etc. to mean the same IdP everywhere, but each app's own client registration for that
IdP is still its own.

## Testing locally with Keycloak

Keycloak is a free, self-hostable OIDC/SAML IdP and the easiest way to test this whole feature end-to-end
without registering a real client at Google/Microsoft/Okta. `docker/docker-compose.yml` has a `keycloak`
service that auto-imports `docker/keycloak/realm-export.json` - a ready-made `vaultguard` realm with:

- A confidential **`vaultguard-web`** client (secret `vaultguard-dev-secret`) with redirect URIs already
  registered for `VaultGuard.Web`'s default `dotnet run` ports (`https://localhost:7202/signin-oidc/keycloak`,
  `http://localhost:5169/signin-oidc/keycloak`).
- A public **`vaultguard-desktop`** client (no secret, PKCE-only) with redirect URI `http://127.0.0.1:*` -
  Keycloak's trailing-`*` wildcard, needed because (unlike Google/Azure AD, which grant a blanket loopback
  exemption) Keycloak requires a client's redirect URIs to be registered, and `OidcSsoService` binds a
  different random port every sign-in.
- A test user: `testuser@example.com` / `Test123!`.

**These are throwaway, local-only dev credentials** - `start-dev` mode, no TLS, secrets committed to the
repo - never point this realm export at a real deployment.

To try it:

```bash
cd docker
docker compose up keycloak
```

Keycloak comes up on `http://localhost:8081` (admin console: `http://localhost:8081/admin`, `admin`/`admin`
by default - see `.env.example`'s `KEYCLOAK_ADMIN_PASSWORD`). The fastest local loop is running
`VaultGuard.Web`/`VaultGuard.API`/`VaultGuard.WPF` normally via `dotnet run` alongside it, rather than also
using the (currently stale - see the note in `docker/Dockerfile.api`) `api`/`web` Docker services.

Add matching entries to each app's `Sso:Providers` (`appsettings.Development.json` is the natural place for
web/API so these never land in a committed production config):

```json
// VaultGuard.Web/appsettings.Development.json and VaultGuard.API/appsettings.Development.json
{
  "Id": "keycloak",
  "DisplayName": "Keycloak (dev)",
  "Authority": "http://localhost:8081/realms/vaultguard",
  "ClientId": "vaultguard-web",
  "ClientSecret": "vaultguard-dev-secret"
}
```

```json
// VaultGuard.WPF/appsettings.json (or an appsettings.Development.json override, if you add one)
{
  "Id": "keycloak",
  "DisplayName": "Keycloak (dev)",
  "Authority": "http://localhost:8081/realms/vaultguard",
  "ClientId": "vaultguard-desktop"
}
```

Then sign in as `testuser@example.com` - this exercises the full **SP-initiated** flow (VaultGuard is the
Service Provider: it starts the request by redirecting to Keycloak) end to end: challenge, Keycloak login
page, callback, verified `sub`/`email` claims, profile-picker match, master-password step, and (on success)
the `AspNetUserLogins` link getting persisted so the next sign-in resolves via the link instead of email.

## SAML / classic ADFS (not yet implemented)

If your only option is a SAML 2.0 IdP - most commonly an on-prem ADFS farm that has never had its OIDC
endpoint enabled, or another SAML-only enterprise IdP - VaultGuard doesn't have a SAML service-provider
stack yet. This is a genuinely separate protocol (XML assertions + browser POST bindings, not
authorization-code OAuth), so it needs its own library and its own integration rather than fitting into
the OIDC path above. If you need this, the practical options are:

1. **Enable OIDC on your IdP instead**, if possible (ADFS 2016+, most modern IdPs) - then it's just another
   entry in `Sso:Providers`, no new code.
2. **Add SAML support** as a follow-up feature - likely via `Sustainsys.Saml2` (ASP.NET Core) for the web
   app, config-driven the same way (IdP metadata URL, entity ID) as a `Protocol: "Saml"` provider entry.
   Desktop SAML is unusual - most native apps that need to reach a SAML-only IdP go through a system
   browser + an ASP.NET Core-hosted SP anyway, which is a bigger design question than adding a package.

## Troubleshooting

| Symptom | Likely cause |
| --- | --- |
| No SSO buttons appear | `Sso:Providers` is empty, or every entry is missing `Id`/`ClientId`/`Authority` - check `appsettings.json` (or the env var override) and restart the app |
| One provider's button is missing but others show | That entry's `Enabled` is `false`, or it's missing a required field - the others are independently configured and unaffected |
| IdP shows a redirect URI mismatch (Web) | The registered redirect URI doesn't exactly match `https://<your-domain>/signin-oidc/<Id>`, including scheme, path, and trailing slash |
| WPF: "Could not determine `<provider>`'s sign-in endpoints" | The provider's `Authority` is wrong, unreachable, or doesn't serve a standard `/.well-known/openid-configuration` document - set `AuthorizationEndpointOverride`/`TokenEndpointOverride` explicitly if so |
| WPF: "Could not start the local sign-in listener" | Another process is holding the chosen loopback port, or the OS is blocking `HttpListener` from binding - rare, since a fresh port is picked per attempt |
| "No local profile matches the account ..." | Expected when the verified email doesn't match any existing VaultGuard profile's email - SSO does not create accounts; add/edit the matching profile first |
| Sign-in "times out waiting for the browser to redirect back" | User closed the browser tab or didn't complete the IdP's consent screen within 5 minutes |

## Related documentation

- [`AUTHENTICATION.md`](AUTHENTICATION.md) - master-key login, 2FA, passkeys, and the rest of the account
  security surface SSO plugs into.
- [`PASSKEYS.md`](PASSKEYS.md) - a similar "identity proof, not a decryption key" pattern already used for
  device passkeys/Windows Hello quick unlock; useful background for the same zero-knowledge reasoning.
- [`CONFIGURATION_GUIDE.md`](CONFIGURATION_GUIDE.md) - secrets management (Google Secret Manager, env vars,
  user-secrets) for keeping each provider's `ClientSecret` out of source control.
