# API Reference: Overview & Auth

`VaultGuard.API` is an ASP.NET Core Web API. It's the same surface the mobile app and browser extension
use, and it runs the exact `VaultGuard.Services`/`VaultGuard.Crypto` core the in-process WPF and Blazor
clients use - so behaviour is identical everywhere.

Interactive docs are also served live by the API itself at `/scalar` (Scalar API reference) when it's running.

## Base URL

Whatever host you run the API on, e.g. `https://localhost:7001` in development.

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
