# Licensing (CD Keys / Pro Feature Unlock)

VaultGuard ships a CD-key licensing system so a super admin can sell/issue "Pro" licenses that unlock
paid features (API access, cloud backup, unlimited devices, advanced sharing, priority support, SSO,
multi-tenancy) in the WPF and Blazor (Web/MAUI) clients, without those clients ever talking directly to
the database.

## Design goals

- **CD keys are opaque, one-time activation credentials**, not the thing that's actually trusted at
  runtime. This mirrors how most real commercial license systems work (FileBot, JetBrains, etc.): a short
  human-typeable key activates a device, and what the app actually checks afterward is a richer, signed
  artifact — not the key itself.
- **The artifact the client trusts is *encrypted, then signed*.** Both layers exist for different
  reasons, and it matters which one is doing the actual security work:
  - **AES-256-GCM encryption** of the license claims (customer, plan, features, expiry) keeps the cached
    certificate from being casually opened and edited in a text editor. This is genuinely "encrypted," as
    requested — but the AES key necessarily ships inside every client binary (WPF, Blazor, mobile), so a
    sufficiently determined attacker who decompiles a client **can** recover it. Encryption alone would not
    stop someone from then handcrafting a fake "Enterprise" certificate.
  - **ECDSA P-256 signing** over the encrypted blob (ciphertext ‖ nonce ‖ tag) is what actually prevents
    that. The private signing key is generated once and lives **only** on the license-issuing side
    (VaultGuard.API's configuration / VaultGuard.Admin) — it is never embedded in a client. Every client
    only carries the *public* key, which can verify a signature but cannot produce one. Even a fully
    reverse-engineered client (AES key extracted, source recovered) still cannot mint a certificate that
    passes signature verification, because doing so requires the private key.

  In short: **encryption protects the cached file from casual tampering; the signature is the actual
  anti-forgery/anti-piracy guarantee.** Ship both, but design the threat model around the signature.

- **CD key ≠ the trusted artifact.** The key just gets you a certificate. If a key needs to be revoked
  (chargeback, abuse, license transfer), the server does it centrally — the client will find out the next
  time it's online (`POST /api/license/validate`) and fall back to Free.

## Data model (`VaultGuard.Models.Licensing`)

| Type | Purpose |
|---|---|
| `LicenseKey` | Server-side row: the CD key string, customer, plan, feature flags, max activations, expiry, revocation state, and an optional `UserId` (see "Assigning a license to a user" below). Full CRUD via VaultGuard.Admin → `LicenseController`. |
| `LicenseActivation` | One row per device that has activated a given key. Enforces the `MaxActivations` seat limit. |
| `LicensePayload` | The plaintext claims (customer, plan, features, device id, expiry) that get sealed into a certificate at activation time. |
| `SignedLicenseCertificate` | What the client actually caches: `CiphertextBase64` / `NonceBase64` / `TagBase64` (AES-256-GCM) + `SignatureBase64` (ECDSA). |
| `LicenseFeature` | `[Flags]` enum — individual Pro features, so a license can grant any combination. |
| `LicensePlan` | Free / Pro / Business / Enterprise — each with a default `LicenseFeature` set (`LicensePlans.DefaultFeatures`). |
| `LicensingSettings` | Single-row, database-persisted signing keys + issuance defaults — see "Settings" below. |
| `Subscription` | Billing/plan state; auto-created and linked (`LicenseKeyId`) when a license is assigned directly to a user. |

## License CRUD (VaultGuard.Admin → Licenses page)

`LicenseController` exposes full CRUD, all `[RequireSuperAdmin]`:

| Endpoint | Purpose |
|---|---|
| `POST /api/license` | Issue a new key (by email, or assigned to a user — see below). |
| `GET /api/license` | List, filterable by `?customerEmail=`, `?tenantId=`, or `?userId=`. |
| `GET /api/license/{id}` | Get one. |
| `PUT /api/license/{id}` | Edit customer info, assignment, plan, features, max activations, expiry, notes. |
| `DELETE /api/license/{id}` | Hard delete (cascades to its `LicenseActivation` rows). |
| `POST /api/license/{id}/revoke` | Soft-disable without deleting (keeps history/audit trail — prefer this over delete for a license that was actually used). |

The Admin UI's Licenses page has Edit and Delete actions per row, plus an "Assigned User" column.

## Assigning a license directly to a user

Besides issuing a freeform CD key by email for someone to self-activate, a SuperAdmin can assign a
license straight to an existing VaultGuard account — from the Licenses page ("Issue License Key" →
toggle "Assign directly to an existing VaultGuard account" → pick the user from the autocomplete), or
from the Users page (Manage → "Assign New License", using the configured defaults).

`POST /api/license` with `UserId` set:
1. Pulls `CustomerEmail`/`CustomerName` from that account if not explicitly overridden.
2. Sets `LicenseKey.UserId`, so `GET /api/license?userId=…` and the Users page's "Manage" dialog show it.
3. Immediately creates a linked, **Active** `Subscription` row (`LicenseKeyId` set) — the assignment shows
   up right away, without waiting for the user to type the CD key into a device.

The CD key is still generated and still needs to be activated on a device (`POST /api/license/activate`)
for that specific install to actually unlock Pro features locally — assignment establishes who the
license belongs to and reflects it in Subscriptions/Users immediately; it doesn't remotely unlock an
already-running client. Give the admin-visible key to the user (or, since they're a known account, you
could extend this with an authenticated "my license" endpoint the client calls with the signed-in user's
own credentials instead of a typed key — not built in this pass, see Known limitations).

## CD key format

`VG-7K9QP-3M2XR-8HN4T-QW6ZD` — a `VG-` prefix, then 20 Crockford Base32 characters (excludes `I`, `L`,
`O`, `U` so it can't be confused with `1`/`1`/`0`) split into 5-character groups: 16 characters of random
entropy (10 bytes / 80 bits) plus a 4-character CRC-16 checksum. The checksum lets the client catch a
typo'd key **before** ever calling the API (`ILicenseCryptoService.TryNormalizeCdKey`) — it is not a
security control, the ECDSA signature on the resulting certificate is.

Implementation: `VaultGuard.Crypto/Services/Crockford32.cs` + `LicenseCryptoService.cs`.

## Activation flow

```
Customer types CD key into WPF/Blazor Settings → License
        │
        ▼
POST /api/license/activate  { keyCode, deviceId, deviceName, appVersion, platform }
        │  (VaultGuard.API/Controllers/LicenseController.cs)
        │  - normalizes + checksum-validates the key
        │  - looks up LicenseKey; rejects if revoked/expired
        │  - creates/reuses a LicenseActivation row (enforces MaxActivations)
        │  - builds a LicensePayload, AES-256-GCM encrypts it, ECDSA-signs the result
        ▼
SignedLicenseCertificate  →  cached by the client (ILicenseClientService, via the same
                              shared settings.json WPF/Blazor already use for ApiBaseUrl/ApiKey)
        │
        ▼
Client verifies signature (public key) + decrypts (AES key) → LicensePayload
        │
        ▼
IsProUnlocked / HasFeature(LicenseFeature.ApiAccess) etc. gate UI (e.g. Blazor Web's
"API Key Management" page checks HasFeature(ApiAccess) before showing anything)
```

`POST /api/license/validate` repeats the lookup + re-seals a fresh certificate, for periodic online
re-checks — a revoked key returns 403 and the client (`LicenseClientService.TryRefreshAsync`) clears its
cache immediately. Between checks, the client keeps trusting its last-known-good cached certificate
(**offline use works** — it just doesn't see a revocation until the next successful online check).

## Settings (VaultGuard.Admin → Settings page)

Signing keys can be generated and viewed straight from the Admin UI — no manual `openssl`/console-app
step required for a fresh deployment:

| Endpoint | Purpose |
|---|---|
| `GET /api/license/settings` | Current status (`Configured`, `Source`: `Database`/`AppSettings`/`None`), public key, AES key, and issuance defaults. Never returns the private key. |
| `POST /api/license/settings/generate-keys` | Generates a brand-new ECDSA key pair + AES key and **persists them to the database** (`LicensingSettings`, a single-row table) — from then on this takes priority over the `Licensing` appsettings.json section. |
| `PUT /api/license/settings` | Updates issuance defaults only (`DefaultPlan`, `DefaultMaxActivations`, pre-filled in "Issue License Key") — doesn't touch the keys. |

**Database-backed settings take priority over appsettings.json** (`LicenseController.GetEffectiveConfigAsync`)
so existing deployments that already set `Licensing:*` in `appsettings.json` keep working unchanged;
clicking "Generate Signing Keys" in the Admin UI simply gives that deployment a DB-persisted override.

Either way, every **client** (`VaultGuard.WPF/appsettings.json`, `VaultGuard.Web/appsettings.json`) still
needs the public key + AES key copied into its own `Licensing` section manually — the Settings page shows
both in copyable text fields for exactly that purpose. Never the private key, which this page doesn't
even display.

If you'd rather configure it via appsettings.json/Secret Manager instead of the UI (e.g. for a scripted
deployment), you can still do that directly:

```csharp
var crypto = new VaultGuard.Crypto.Services.LicenseCryptoService();
var (publicKeyPem, privateKeyPem) = crypto.GenerateSigningKeyPair();
```

then `Licensing:SigningPrivateKeyPem` / `SigningPublicKeyPem` (API config only — user-secrets/env vars/Secret
Manager in production, never committed) and `Licensing:AesKeyBase64` (`openssl rand -base64 32`).

## Known limitations / follow-ups

- No payment provider is wired up. `Subscription.ExternalProviderRef` is a free-text slot for a future
  Stripe/Paddle subscription id; issuing/assigning a license today is a manual SuperAdmin action.
- **Key rotation is all-or-nothing.** "Regenerate Signing Keys" replaces the single active key pair —
  certificates issued afterward won't verify against a client still configured with the old public key
  until that client's config is updated too. There's no grace-period/multi-key overlap support.
- Assigning a license to a user (above) makes the assignment visible immediately (Subscriptions, Users),
  but a device still has to activate the CD key (`POST /api/license/activate`) to actually unlock Pro
  features locally — there's no "push unlock to an already-running signed-in client" endpoint yet.
- `GET /api/license/settings` is a convenience for distributing client config; it isn't a substitute for
  hardcoding the public key at build time for a hostile-client threat model (a MITM'd or compromised
  server could otherwise hand out a different "public key" — pin it at build time if that risk matters to you).
