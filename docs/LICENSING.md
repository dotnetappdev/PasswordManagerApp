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
| `LicenseKey` | Server-side row: the CD key string, customer, plan, feature flags, max activations, expiry, revocation state. Issued by a SuperAdmin via VaultGuard.Admin → `POST /api/license`. |
| `LicenseActivation` | One row per device that has activated a given key. Enforces the `MaxActivations` seat limit. |
| `LicensePayload` | The plaintext claims (customer, plan, features, device id, expiry) that get sealed into a certificate at activation time. |
| `SignedLicenseCertificate` | What the client actually caches: `CiphertextBase64` / `NonceBase64` / `TagBase64` (AES-256-GCM) + `SignatureBase64` (ECDSA). |
| `LicenseFeature` | `[Flags]` enum — individual Pro features, so a license can grant any combination. |
| `LicensePlan` | Free / Pro / Business / Enterprise — each with a default `LicenseFeature` set (`LicensePlans.DefaultFeatures`). |

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

`GET /api/license/signing-config` (SuperAdmin only) hands back the current public key + AES key so an
operator can paste them into a client build's `appsettings.json` "Licensing" section. This endpoint never
returns the private key.

## Generating the signing keys (one-time, per deployment)

The private key must be generated once and stored **only** in VaultGuard.API's configuration (and, if
you run VaultGuard.Admin separately from the API, nowhere else — VaultGuard.Admin never issues a
certificate itself, it just calls the API). A quick way to generate the pair:

```csharp
var crypto = new VaultGuard.Crypto.Services.LicenseCryptoService();
var (publicKeyPem, privateKeyPem) = crypto.GenerateSigningKeyPair();
```

(run this once from a scratch console app, or a temporary throwaway top-level statement — there is no
CLI wired up for it in this pass). Then:

- `VaultGuard.API/appsettings.json` → `Licensing:SigningPrivateKeyPem` and `Licensing:SigningPublicKeyPem`
  (or better, set `SigningPrivateKeyPem` via `dotnet user-secrets` / an environment variable /
  Secret Manager in production — never commit a real private key).
- The AES key: `openssl rand -base64 32` → `Licensing:AesKeyBase64` (API config).
- Every **client** (`VaultGuard.WPF/appsettings.json`, `VaultGuard.Web/appsettings.json`) gets only
  `Licensing:SigningPublicKeyPem` and `Licensing:AesKeyBase64` — never the private key.

## Known limitations / follow-ups

- No payment provider is wired up. `Subscription.ExternalProviderRef` is a free-text slot for a future
  Stripe/Paddle subscription id; issuing a license today is a manual SuperAdmin action in VaultGuard.Admin.
- The AES key and public key are static per deployment (no key rotation flow yet). Rotating the signing
  key invalidates every cached certificate until clients re-validate online.
- `GET /api/license/signing-config` is a convenience for distributing client config; it isn't a substitute
  for hardcoding the public key at build time for a hostile-client threat model (a MITM'd or compromised
  server could otherwise hand out a different "public key" — pin it at build time if that risk matters to you).
