# Security Model

VaultGuard is **zero-knowledge**: your master password never leaves the device, is never written to
disk, and is never sent to the server or API. It exists only long enough, in memory, to derive your keys.
A stolen database, backup file or sync payload is useless without it. Every front-end (WPF, Blazor, MAUI,
API, browser extension) shares the same `VaultGuard.Crypto` core, so these guarantees hold identically
wherever you sign in - including the standalone WPF desktop app's local SQLite vault.

## Key derivation

**PBKDF2-HMAC-SHA256, 600,000 iterations** (the OWASP 2024 recommendation) derives a 256-bit master key
from your master password and a per-user, cryptographically-random 32-byte salt. The iteration count is
deliberately expensive to brute-force.

**Argon2id** is also supported as a memory-hard alternative via a self-describing hash format
(`$argon2id$...`) - authentication hashes can upgrade to Argon2id while existing PBKDF2 vaults keep
verifying unchanged, since the format is auto-detected in the shared core.

**HKDF-SHA256** then derives independent, purpose-specific sub-keys (encryption, authentication, backup,
...) from the master key, so a leak of one sub-key never exposes the others.

## Encryption

Every secret - passwords, notes, card numbers, TOTP seeds, custom fields - is sealed with
**AES-256-GCM authenticated encryption**: a fresh 96-bit random nonce per encryption, a 128-bit
authentication tag, and rejection on decrypt if anything was tampered with. Nothing is ever stored with a
static IV or an unauthenticated cipher mode.

## Authentication is separate from encryption

The value stored for login is an **authentication hash** derived from the master key - not the master
password, and not the encryption key itself (Bitwarden-style separation). Because it's a one-way
derivative, the database can verify you without ever holding anything capable of decrypting your vault.

## Operational hardening

- **In-memory hygiene** - the derived master key is zeroed with `Array.Clear` as soon as it's no longer
  needed; intermediate key-material buffers are wiped the same way.
- **Constant-time comparisons** - all security-sensitive verification (auth hashes, passcode hashes, 2FA/
  TOTP codes, hashed recovery codes) uses `CryptographicOperations.FixedTimeEquals`, so response timing
  can't leak bytes.
- **Two-factor & recovery codes** - TOTP support; recovery codes are hashed and single-use, never stored
  in plaintext.
- **Passkeys** - WebAuthn/FIDO2 sign-in, including a software authenticator for the browser extension.
- **"Remember this device"** - the cached credential lives in the platform's secure store, never as
  plaintext: Windows DPAPI (WPF), Keychain/Keystore (MAUI), `ProtectedLocalStorage` (Blazor web).
- **Encrypted backups** - local, NAS/SMB, FTP/FTPS, OneDrive and Google Drive backups are encrypted on
  the device before upload; those targets only ever see ciphertext.
- **Brute-force protection** - the API rate-limits requests (a global per-IP limit plus a stricter limit
  on authentication endpoints) and enforces account lockout after repeated failed sign-ins.
- **Transport** - HTTPS with an explicit CORS allow-list; the API additionally gates every request with
  bearer-token auth and an API-key middleware.

## What's stored vs. never stored

| Stored (safe) | Never stored |
| --- | --- |
| Per-user random salt (32 bytes) | ❌ Master password (plaintext or reversible) |
| Authentication hash (one-way, from the master key) | ❌ The AES encryption key / master key at rest |
| AES-256-GCM ciphertext + nonce + auth tag | ❌ Any secret in plaintext |
| Hashed, single-use recovery codes | ❌ Recovery codes in plaintext |
| Salted, hashed device passcode | ❌ Passcodes in plaintext |

## Reporting a vulnerability

Please report suspected security issues privately via a GitHub security advisory rather than a public
issue.
