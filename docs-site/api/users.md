# Users, Auth & API Keys

## Sign-in - `/api/auth`

| Method | Route | Description |
| --- | --- | --- |
| `POST` | `/api/auth/login/masterkey` | Sign in with just the master key (no email). Returns a session token, or `RequiresTwoFactor: true` if 2FA is enabled. |
| `POST` | `/api/auth/login/enhanced` | Sign in with email + master password, with 2FA/passkey support. |
| `POST` | `/api/auth/otp/send` | Send an SMS OTP code (if SMS 2FA is configured). |
| `POST` | `/api/auth/otp/login` | Complete sign-in with an OTP code. |
| `POST` | `/api/auth/otp/setup` / `/api/auth/otp/verify-setup` | Enable SMS-based 2FA for the signed-in user. |
| `POST` | `/api/auth/otp/disable` | Disable 2FA (requires master password + a valid code or backup code). |
| `POST` | `/api/auth/qr/generate-anonymous` | Generate an anonymous QR code shown on a device that isn't signed in yet. |
| `POST` | `/api/auth/qr/authenticate` | A second device scans the QR and authenticates, signing the first device in. |
| `DELETE` | `/api/auth/account` | Permanently delete the signed-in user's account and all their data. Requires the master password. |

Every login response includes an `AuthResponseDto` with a session `Token` valid for 8 hours, used as
`Authorization: Bearer <token>` on endpoints that decrypt or reveal secrets.

## API keys - `/api/apikeys`

The credential mobile apps and the browser extension use instead of a full login session - see
[Getting Started](/guide/getting-started#getting-an-api-key-for-mobile-the-browser-extension) for the
end-user flow.

| Method | Route | Auth | Description |
| --- | --- | --- | --- |
| `POST` | `/api/apikeys/issue` | none (`[AllowAnonymous]`) | Issue a key from email + master password. Returns the plaintext key **once**. |
| `GET` | `/api/apikeys` | session | List the caller's keys. The `KeyHash` field is masked (`***`) - the plaintext is never retrievable again. |
| `POST` | `/api/apikeys` | session | Create a named key for the signed-in user. |
| `DELETE` | `/api/apikeys/{keyId}` | session | Revoke a key. Scoped to the caller - you cannot revoke another user's key by guessing its ID. |

Keys are stored **hash-only**; validation matches by hash, and revocation is a soft delete (`IsActive =
false`) scoped to the owning user.

## User profile & admin - `/api/userprofile`

Admin-only (`[Authorize(Roles = "Admin")]`) management of user accounts.

| Method | Route | Description |
| --- | --- | --- |
| `GET` | `/api/userprofile` | All users in the system. |
| `GET` | `/api/userprofile/{id}` | A user's profile details. |
| `POST` | `/api/userprofile` | Create a new user. Body: `CreateUserProfileDto` (email, password, confirm password, name, master-password hint). Returns `201` with a `Location` header. |
| `PUT` | `/api/userprofile/{id}` | Update profile fields (email, name, hint). Body: `UpdateUserProfileDto`. |
| `PUT` | `/api/userprofile/{id}/password` | Change a user's master password. Body: `ChangePasswordDto` (current + new password). |
| `PUT` | `/api/userprofile/{id}/deactivate` | Deactivate an account (soft, reversible). |
| `PUT` | `/api/userprofile/{id}/reactivate` | Reactivate a deactivated account. |
| `DELETE` | `/api/userprofile/{id}` | Permanently delete a user account. |
