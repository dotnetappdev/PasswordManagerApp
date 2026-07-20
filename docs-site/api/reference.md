# API Reference: Overview & Auth

`VaultGuard.API` is an ASP.NET Core Web API. It's the same surface the mobile app and browser extension
use, and it runs the exact `VaultGuard.Services`/`VaultGuard.Crypto` core the in-process WPF and Blazor
clients use - so behaviour is identical everywhere.

## Base URL

Whatever host you run the API on, e.g. `https://localhost:7001` in development.

## Accessing Scalar

`VaultGuard.API` serves a full interactive reference UI ([Scalar](https://scalar.com/)) of its own,
live, for free - no separate tool or account needed:

```
{API_BASE_URL}/scalar
```

e.g. `https://localhost:7001/scalar` in development, or `https://api.yourdomain.com/scalar` once
deployed. Every endpoint is browsable there, and each one has a **Test Request** button that lets you
fill in headers/body and call your running API directly from the browser - the same client Stripe/Twilio-style
docs use, just self-hosted with your instance.

Prefer staying on this docs site? **[Try It Live](/api/try-it)** embeds the same client here - point it
at your API's URL and it works identically.

> **CORS note:** the Test Request panel calls your API straight from your browser, so it's a real
> cross-origin request unless you're browsing from the exact same origin as the API. `Cors:AllowedOrigins`
> (see `appsettings.json`) defaults to `localhost` variants only - add whatever origin you're browsing
> the docs from (e.g. `https://dotnetappdev.github.io` if using the hosted docs site against a public
> instance) if requests get blocked.

Below: the standalone `/scalar` page after clicking **Test Request → Send** on `POST /api/ApiKeys/issue` -
a real request went out and a real response (`401 Unauthorized`, since no `X-API-Key` was set) came back
in 14ms, shown in the response panel on the right.

<div class="screenshot-grid">
  <figure><img src="/screenshots/api/scalar.png" alt="Scalar endpoint reference" /><figcaption>Endpoint reference (sidebar + docs)</figcaption></figure>
  <figure><img src="/screenshots/api/scalar-standalone-response.png" alt="Scalar live response" /><figcaption>Test Request → Send, with a real response</figcaption></figure>
</div>

<style>
.screenshot-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
  gap: 16px;
  margin: 16px 0;
}
.screenshot-grid figure { margin: 0; }
.screenshot-grid img { width: 100%; border-radius: 8px; border: 1px solid var(--vp-c-divider); }
.screenshot-grid figcaption { text-align: center; font-size: 0.85em; color: var(--vp-c-text-2); margin-top: 6px; }
</style>

## Authentication

Every endpoint except `/health`, `/scalar`, `/openapi` and `/api/authentication/*` requires the
`X-API-Key` header.

```
X-API-Key: <your-key>
```

### Getting a key

**From the web app** (recommended): sign in, go to **Settings → API Keys** (`/api-keys`), create a
named key, copy it immediately - it's shown once and stored hash-only afterwards.

**Directly from the API**, proving identity with email + master password - useful for scripting or
first-time device setup:

```
POST /api/authentication/generate-api-key
Content-Type: application/json

{
  "name": "My Pixel 8",
  "email": "you@example.com",
  "masterPassword": "your-master-password"
}
```

```json
{
  "apiKey": "414C1BUrIaVHwulgSPZAW1-ELcL--MhSS9Cmx8Z8GZc",
  "userId": "0d6fe338-9517-42cf-aeb7-16410b285da7",
  "keyName": "My Pixel 8",
  "instructions": "Store this API key securely..."
}
```

The equivalent under `/api/apikeys/issue` (`ApiKeysController.IssueApiKey`) does the same thing and is
what the desktop/web/mobile "Generate key from server" / "Test connection" flows call.

### Session tokens (for decrypt/reveal endpoints)

Signing in (`POST /api/auth/login/enhanced` or `/api/auth/login/masterkey`) with the `X-API-Key` header
returns a session **token**. Endpoints that decrypt or reveal secrets (`/api/passworditems/{id}/decrypt`,
`/api/passworditems/{id}/reveal`, `/api/auth/qr/generate`, OTP setup) additionally require that token as:

```
Authorization: Bearer <token>
```

```
POST /api/auth/login/enhanced
X-API-Key: 414C1BUrIaVHwulgSPZAW1-ELcL--MhSS9Cmx8Z8GZc
Content-Type: application/json

{ "email": "you@example.com", "password": "your-master-password" }
```

> The seeded demo account `admin@passwordmanager.local` uses master key `7hm3Z!Csu:Y64nm` in a
> development environment seeded with demo data, handy for trying the flow above immediately.

## Authorization

Beyond authentication, most write/read actions on password items go through a permission check
(`Permissions.Passwords.View/Create/Edit/Delete/ViewSensitive`) scoped to the resource owner - a user can
only act on their own items unless a parent/child relationship grants otherwise. A missing item and an
item you don't have access to both return `404`, never `403`, so an ID can't be used to enumerate other
users' data.

## Conventions

- All request/response bodies are JSON.
- Collection endpoints return arrays directly (not wrapped in an envelope).
- Not-found returns `404` with a short string message, e.g. `"Password item with ID 42 not found"`.
- Validation failures return `400` with `ModelState` errors.
- Unhandled server errors return `500` with a generic message (details are logged server-side, not
  exposed to the client).
- Auth endpoints are rate-limited per-IP more strictly than the rest of the API.

## Where to go next

- [Password Items](/api/password-items) - the core CRUD surface, plus reveal/decrypt.
- [Collections, Categories & Tags](/api/organization) - the three organisational axes.
- [Users & Settings](/api/users) - profile, admin user management, API key lifecycle.
