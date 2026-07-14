# Security Remediation PRD — Vault Guard

> **Update 2026-06-27:** Beyond the original findings, the codebase now has secret-free,
> no-swallowed-exceptions logging across all platforms (`AppLogger`), a server-verified
> passkey/WebAuthn implementation, and a removed passkey-login auth-bypass. See
> [docs/SECURITY.md](docs/SECURITY.md) → "Recent Security Updates" and "Compliance and
> Standards" for the current posture and the standards the app is built to align with.

**Status:** Ready for implementation
**Owner:** Security
**Audience:** An autonomous coding agent (and reviewers) remediating the findings from the
2026 security audit across **all platforms**: API, Web (Blazor Server), WPF, WinUI, and MAUI
(Blazor Hybrid). MAUI hosts the shared `VaultGuard.Components.Shared` UI via `BlazorWebView`.

---

## 1. Goal

Close the authentication, authorization, cryptographic, and storage gaps found in the audit
without changing the product's core UX, and leave the codebase with **one** consistent 2FA /
recovery-code implementation. Every change must build clean and be covered by a test or a
documented manual verification.

## 2. Ground rules for the implementing agent

1. **Don't weaken existing crypto.** AES-256-GCM + PBKDF2 (600k) for the vault stays. The master
   password is the only thing that derives the vault key — never log it, never persist it in
   plaintext, never "encrypt" it with a constant key.
2. **Hash, don't encrypt, verifiers.** Anything you only need to *check* (passwords, recovery
   codes, API keys) is salted-hashed, never reversibly stored.
3. **One implementation per concern.** Where two implementations exist (2FA/backup codes), keep the
   secure one and delete the other — don't leave dead, insecure code paths.
4. **Fail closed.** Authorization checks default to deny; missing/empty identity → `401`/`404`.
5. **Build + verify after every item.** Run the project build for each affected platform and the
   test suite (`dotnet test`). Update or add tests for the behavior you change.
6. **No secrets in source.** Use user-secrets / environment variables; the repo keeps placeholders only.
7. Keep diffs focused; match surrounding code style; add a short comment explaining *why* for each
   security-relevant change.

## 3. Severity legend

🔴 Critical · 🟠 High · 🟡 Medium · 🔵 Low — implement in that order.

---

## 4. Work items

> Each item: **what's wrong → required change → files → acceptance criteria → verify.**

### 🔴 C1 — Remove the zero-key "encryption" of 2FA backup codes
- **Platforms:** API (also fixes Web/MAUI/WPF since they consume the same backend).
- **Problem:** `AuthController` setup-2FA path encrypts backup codes with `new byte[32]` (all zeros)
  and stores them in `ApplicationUser.BackupCodes` — effectively plaintext to anyone with the DB.
- **Files:** `VaultGuard.API/Controllers/AuthController.cs` (~470–511, 540–715),
  `VaultGuard.Services/Services/OtpService.cs`, `ApplicationUser.BackupCodes`.
- **Change:** Delete the `IOtpService`/`user.BackupCodes` backup-code path. Route all 2FA setup,
  verification, disable, and recovery through `TwoFactorService` + the `UserTwoFactorBackupCodes`
  table (PBKDF2-hashed codes). Migrate/retire the `BackupCodes` column.
- **Acceptance:** No code path stores recovery codes that can be decrypted to plaintext. A DB dump
  contains only salted hashes. Enabling 2FA, verifying a TOTP code, and redeeming a recovery code
  all work end-to-end via `TwoFactorService`.
- **Verify:** `dotnet test`; manually enable 2FA on a test user, confirm `UserTwoFactorBackupCodes`
  rows are hashes and `BackupCodes` is unused.

### 🟠 H1 — Fix IDOR on item read endpoints
- **Platforms:** API.
- **Problem:** `GET /api/passworditems/{id}` (and `collection/{id}`, `category/{id}`, `tag/{id}`)
  return items without verifying they belong to the caller — cross-user metadata disclosure.
- **Files:** `VaultGuard.API/Controllers/PasswordItemsController.cs` (`GetById` ~90; collection/
  category/tag GETs ~111–164), and any other controller that loads by id (Vaults, Collections,
  Categories, Tags, CustomFields).
- **Change:** After loading, compare the resource's `UserId` to the authenticated user (mirror the
  `CanAccessResourceAsync` pattern already used by `PUT`/reveal). Return `404` (not `403`) on
  mismatch to avoid existence disclosure. Audit every `GetByIdAsync(id)` call site.
- **Acceptance:** A request for another user's item id returns `404`; the owner still gets `200`.
- **Verify:** Add an integration test: user A creates an item, user B (different API key) gets `404`.

### 🟠 H2 — Remove hardcoded credentials from config
- **Platforms:** API (Web/MAUI configs too).
- **Problem:** `appsettings.json` ships real-looking DB credentials (`sa`/`sa12345`, etc.).
- **Files:** `VaultGuard.API/appsettings*.json`, any `appsettings` in Web/MAUI.
- **Change:** Replace connection-string secrets with placeholders; load real values from
  environment variables / .NET user-secrets / a secret store. Document required env vars in
  `CONFIGURATION_GUIDE.md`. Add the secret files to `.gitignore` where appropriate.
- **Acceptance:** No usable credential is committed. App reads connection strings from env/secrets.
- **Verify:** `git grep -i "password=" -- '*.json'` returns only placeholders; app starts with env vars set.

### 🟠 H3 — Collapse to one 2FA implementation
- **Platforms:** API/Services (shared).
- **Problem:** `TwoFactorService` (hashed, correct) and `OtpService`/`AuthController` (zero-key) coexist.
- **Files:** `VaultGuard.Services/Services/OtpService.cs`, `AuthController.cs`, `ITwoFactorService`.
- **Change:** Make `TwoFactorService` the single source of truth; remove `OtpService` 2FA/backup
  logic (keep only any unrelated SMS/OTP if still used). Update DI registrations.
- **Acceptance:** Exactly one backup-code store and one verify path remain. Solution builds.
- **Verify:** Grep shows no remaining references to the removed path; `dotnet test`.

### 🟡 M1 — Make the auth model explicit and correct
- **Platforms:** API.
- **Problem:** `app.UseAuthentication()` is missing, so Identity bearer tokens/`JwtService` never
  authenticate controllers; API keys are the de-facto sole auth. The token path is dead/confusing.
- **Files:** `VaultGuard.API/Program.cs` (pipeline ~223–235), `JwtService.cs`.
- **Change:** Decide and document one model:
  - **(Preferred)** Keep API-key auth for service-to-service, and add `app.UseAuthentication()` +
    a real bearer scheme for interactive clients; ensure `[Authorize]` accepts both schemes. **Or**
  - Remove `MapIdentityApi`, `JwtService`, and the static refresh-token store entirely if unused.
  - Give API keys an **expiry** and a revocation check on every request.
- **Acceptance:** No dead auth code; `UseAuthentication` present iff token auth is used; API keys expire.
- **Verify:** Authenticated endpoints reject missing/expired credentials; `dotnet test`.

### 🟡 M2 — Restrict CORS
- **Platforms:** API.
- **Files:** `Program.cs` (~198–206, 225).
- **Change:** Replace `AllowAnyOrigin()` with an allow-list from config (the Web app origin and any
  trusted clients). Keep credentials off unless needed.
- **Acceptance:** Requests from unlisted origins are blocked by CORS.
- **Verify:** Preflight from an unlisted origin fails; from the Web origin succeeds.

### 🟡 M3 — Refresh tokens + dead interface method
- **Platforms:** API.
- **Files:** `JwtService.cs` (static `_refreshTokens` ~20; `IJwtService.GetPrincipalFromExpiredToken` ~157).
- **Change:** If keeping refresh tokens, persist them hashed with expiry (DB table), not a static
  dict; remove the `NotImplementedException` explicit-interface impl (or implement it). If tokens
  are removed under M1, delete this file.
- **Acceptance:** No in-memory token store; no method throws `NotImplementedException` in a live path.

### 🟡 M4 — Constant-time secret comparison
- **Platforms:** Crypto (shared by all).
- **Files:** `VaultGuard.Crypto/Services/PasswordCryptoService.cs` (`VerifyMasterPassword` ~195,
  `VerifyMasterKeyIdentifier` ~324).
- **Change:** Compare on raw bytes with `CryptographicOperations.FixedTimeEquals`, not
  `string.Equals(Ordinal)`.
- **Acceptance:** Verification uses fixed-time comparison; existing logins still succeed.
- **Verify:** Crypto unit tests pass.

### 🟡 M5 — Strengthen the master-key identifier
- **Platforms:** Crypto / Services (affects stored data — needs migration plan).
- **Files:** `PasswordCryptoService.CreateMasterKeyIdentifier` (~281–308).
- **Problem:** It's PBKDF2 of the raw master password at only 100k iterations — a cheaper offline
  brute-force target than the 600k auth hash.
- **Change:** Derive the lookup identifier from the already-stretched master key (not the raw
  password), or raise to ≥600k. Provide a one-time re-derivation/migration on next successful login.
- **Acceptance:** New identifiers are not a weaker password oracle; existing users migrate transparently.
- **Verify:** Login + master-key-login still work pre/post migration; unit test the new derivation.

### 🟡 M6 — Fix DELETE claim mismatch
- **Platforms:** API.
- **Files:** `PasswordItemsController.cs` (~348 reads `c.Type == "sub"`).
- **Change:** Use `ClaimTypes.NameIdentifier` consistently (what the API-key middleware sets); add
  the ownership check like H1. Sweep the controller for other `"sub"` reads.
- **Acceptance:** Owner can delete their item; others get `404`; non-owner cannot.
- **Verify:** Integration test for delete ownership.

### 🟡 M7 — Harden "remember this device"
- **Platforms:** Web (Blazor Server), MAUI, WPF — the cross-platform cache feature.
- **Problem:** On Blazor Server the cached master password is recoverable by the server
  (`ProtectedLocalStorage` uses server keys) — a threat-model shift. WPF (DPAPI) / MAUI
  (SecureStorage) are device-local and acceptable, but all three store the *password*.
- **Files:** `VaultGuard.Components.Shared/Services/IMasterKeyCacheService.cs` + impls
  (`VaultGuard.Web/Services/WebMasterKeyCacheService.cs`,
  `VaultGuard.App/Services/MauiMasterKeyCacheService.cs`,
  `VaultGuard.WPF/Services/MasterPasswordCacheService.cs`), `ProfileLogin.razor`, `LoginViewModel.cs`.
- **Change:** (1) Make remember-device **opt-in** with a visible toggle. (2) Add a **TTL** (e.g.
  expire after N days) and clear on master-password change. (3) Prefer caching a *wrapped vault key*
  over the raw password where feasible. (4) Ensure the Blazor Server **data-protection key ring is
  persisted and protected** (e.g. `PersistKeysToFileSystem` + DPAPI/host key vault), and document it.
- **Acceptance:** Remembering is opt-in, expires, and is cleared on password change / "Forget this
  device". Key ring persistence is configured for Web.
- **Verify:** Toggle off → password required next login; TTL expiry → password required; password
  change → cache cleared.

### 🟡 M8 — Android: disable `allowBackup`
- **Platforms:** MAUI (Android).
- **Files:** `VaultGuard.App/Platforms/Android/AndroidManifest.xml`.
- **Change:** Set `android:allowBackup="false"` and add `android:fullBackupContent`/
  `dataExtractionRules` excluding app data. Set `android:usesCleartextTraffic="false"` explicitly.
- **Acceptance:** `adb backup` cannot extract the vault DB; cleartext traffic blocked.
- **Verify:** Manifest contains the flags; app builds.

### 🟡 M9 — Don't ship Debug-level file logging in release (MAUI)
- **Platforms:** MAUI.
- **Files:** `VaultGuard.App/MauiProgram.cs` (~150, `FileLoggerProvider(minLevel: LogLevel.Debug)`).
- **Change:** Gate verbose file logging behind `#if DEBUG` (or read level from config); default
  release to `Warning`/`Information`. Audit shared services to confirm no secret/master-password/
  decrypted value is ever logged.
- **Acceptance:** Release builds don't write Debug logs to disk; no sensitive data in any log statement.
- **Verify:** Inspect a release-config log file is empty/minimal; grep logging calls for sensitive args.

### 🔵 L-batch — Hardening
- **L1 (API):** add `app.UseHsts()`; tighten `AllowedHosts` from `*` to real hosts. — `Program.cs`.
- **L2 (API):** Fido2 `ServerDomain`/`Origins` from config, not hardcoded `localhost`. — `Program.cs ~181`.
- **L3 (API):** stop logging API-key prefixes; review user-id logging. — `ApiKeyAuthenticationMiddleware.cs`.
- **L4 (Identity):** review password policy (length ≥ 12, consider `RequireConfirmedAccount`). — `Program.cs ~103`.
- **L5 (MAUI):** confirm Sentry `SendDefaultPii=false`; move the hardcoded DSN to config. — `MauiProgram.cs ~42`.

---

## 5. Cross-cutting / done criteria

- [ ] All affected projects build clean: `VaultGuard.API`, `VaultGuard.Web`, `VaultGuard.WPF`,
      `VaultGuard.WinUi`, `VaultGuard.App` (MAUI).
- [ ] `dotnet test` passes; new tests cover C1, H1, M6 ownership, M4 fixed-time compare, M7 TTL.
- [ ] `git grep` shows no committed credentials and no remaining zero-key crypto.
- [ ] Exactly one 2FA/recovery-code implementation remains.
- [ ] `SECURITY_SUMMARY.md` / `MASTER_PASSWORD_SECURITY.md` updated to match the new behavior
      (remember-device opt-in + TTL, single 2FA path, auth model).
- [ ] A short `CHANGELOG`/PR description lists each finding ID and how it was resolved.

## 6. Out of scope / non-goals

- Changing the vault encryption scheme (AES-256-GCM / PBKDF2-600k stays).
- Re-designing the UI or the multi-platform architecture.
- Penetration testing / dynamic analysis (recommended as a follow-up, separately).
- Browser-extension native host hardening (track separately).

## 7. Suggested sequencing

1. C1 + H3 (one secure 2FA path) → 2. H1 + M6 (authorization) → 3. H2 (secrets) →
4. M1 + M2 + M3 (auth model & CORS) → 5. M4 + M5 (crypto) → 6. M7 (remember-device) →
7. M8 + M9 (MAUI platform) → 8. L-batch.
