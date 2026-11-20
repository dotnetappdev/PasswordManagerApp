# Vault encryption at rest

Every secret in a vault item is encrypted before it reaches the database, by the standalone apps (WPF, MAUI, Blazor Web) and by the API alike, with the same key and format. Ciphertext written by one is readable by the other.

## What is encrypted

| Item | Secret fields |
|---|---|
| Login | password, TOTP secret, security answers 1–3, notes |
| Credit card | card number, CVV, PIN, online-banking password |
| Wi‑Fi | network password, router admin password, guest network password |
| Secure note | content |
| Passkey | credential id (Windows Hello key name), notes |
| Custom fields | the authenticator (TOTP) secret, and any field of type Password or One-time password, or marked protected |

The single source of truth for this list is `VaultGuard.Services/Security/VaultItemSecrets.cs`.

## How

- **Key:** the user's vault key, derived with PBKDF2‑SHA256 (600,000 iterations) from the master password and the user's random salt. The apps derive it once at unlock and keep it in the vault session. The API derives it per request from the `masterPassword` the client sends, then discards it.
- **Cipher:** AES‑256‑GCM, with a fresh random 96‑bit nonce per encryption and a 128‑bit authentication tag, so any tampering is detected.
- **Storage:** ciphertext, nonce and tag are stored Base64 in separate columns (`EncryptedX`, `XNonce`, `XAuthTag`). Readers also accept the older single-column `cipher|nonce|tag` layout.
- **Plaintext never persists:** the plaintext properties (`CardNumber`, `CVV`, `Password`, `Content`, …) are `[NotMapped]`. They only exist in memory after an item is opened, so a save cannot write a secret in clear.

## Custom fields

A custom field keeps its value in one column. `CustomField.StoredValue` is that column. For secret fields it holds `vgenc1:` followed by `cipher|nonce|tag`; other fields are stored as-is. `CustomField.Value` is the in-memory value:

- It stays empty for an encrypted field until the item is opened, and decrypting never touches the column.
- Setting it to empty on an encrypted field changes nothing, so a form that couldn't decrypt a value can't wipe it.
- `TotpHelper.SetSecret(item, "")` keeps an encrypted secret that was never decrypted.

When copying fields, copy `StoredValue`, not `Value`.

## Rules for code

- **Saving:** call `IPasswordRevealService.TrySealItem(item, sessionId)` (apps) or let `PasswordItemApiService` do it (API) before saving. If it returns false, don't save; tell the user the vault is locked. As a safety net, `PasswordItemService.CreateAsync/UpdateAsync` also seals anything still in plaintext using the unlocked session, and refuses the save if there is none.
- **Displaying:** call `OpenItem(item, sessionId)` once when an item is opened, not on every render.
- **Copying or syncing items:** use `VaultItemSecrets.CopyStored`. Move ciphertext, never plaintext.
- **Encrypting:** only ever use the caller's own session. Never fall back to "any active session"; on the web host that could be another user's.

## Existing data

- **Legacy plaintext:** the old plaintext columns are kept in the schema, mapped as shadow properties `Legacy{Name}` (`VaultGuard.DAL/Security/LegacySecretColumns.cs`). The first unlock after updating runs `VaultSecretsMigrator`, which:
  - encrypts anything still in those columns, plus demo-seeded login passwords stored in clear;
  - rewrites single-column values into the split layout;
  - wipes the plaintext;
  - on SQLite, enables `secure_delete` and checkpoints the WAL, so old values are zeroed on disk.
- **Demo data:** the seeder runs before any key exists, so it parks secrets in the legacy columns and they are encrypted at first unlock.
- **Schema:** SQLite gets the new columns from `SqliteSchemaGuard`. SQL Server gets them from the `EncryptItemSecretsAtRest` migration.

## Changing the master password

`VaultKeyRotation` decrypts every secret with the old key and re-encrypts it with the new one, staged in the same transaction as the new salt and hash. If anything can't be decrypted with the current password, the change is refused. Before this existed, changing the master password left all data permanently unreadable.

## API

- `POST /api/passworditems` and `PUT /api/passworditems/{id}` accept `masterPassword`. It is required whenever the request contains a secret. Without it (or with a wrong one) the request is rejected with 400, never stored unencrypted.
- `PATCH /api/credentials/{id}/totp` needs `masterPassword` too. The browser extension's direct-to-API fallback doesn't have it, so that path now fails cleanly; its normal native-host path encrypts with the session key.
- The API also runs the plaintext migration whenever it verifies a master password (login, secret writes and reveals), so users who only use the API are upgraded too.
- `POST /api/passworditems/{id}/secrets` with `{ "masterPassword": "…" }` returns that item's decrypted secrets, for the caller's own items only.
