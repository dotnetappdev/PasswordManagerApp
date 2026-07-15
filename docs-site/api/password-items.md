# Password Items

`PasswordItemsController` - `/api/passworditems`. All endpoints require `X-API-Key` (see
[Authentication](/api/reference#authentication)); most also enforce a per-resource permission check.

## List & search

| Method | Route | Description |
| --- | --- | --- |
| `GET` | `/api/passworditems` | All items visible to the caller. Requires `Passwords.View`; child accounts get an extra restriction check. |
| `GET` | `/api/passworditems/{id}` | A single item by ID. `404` if missing **or** not owned by the caller (never `403` - avoids ID enumeration). |
| `GET` | `/api/passworditems/collection/{collectionId}` | Items in a collection, scoped to the caller. |
| `GET` | `/api/passworditems/category/{categoryId}` | Items in a category, scoped to the caller. |
| `GET` | `/api/passworditems/tag/{tagId}` | Items with a tag, scoped to the caller. |
| `GET` | `/api/passworditems/search?searchTerm=` | Free-text search, scoped to the caller. |

## Create, update, delete

| Method | Route | Description |
| --- | --- | --- |
| `POST` | `/api/passworditems` | Create an item. Body: `CreatePasswordItemDto` (`Title`, `Type`, `CategoryId`, `CollectionId`, plus a typed sub-object like `LoginItem`/`CreditCardItem` depending on `Type`). Requires `Passwords.Create`; blocked for child accounts. |
| `PUT` | `/api/passworditems/{id}` | Update an item. Body: `UpdatePasswordItemDto`. Requires `Passwords.Edit` on that item; blocked for child accounts. |
| `DELETE` | `/api/passworditems/{id}` | Permanently delete. Requires `Passwords.Delete`. |
| `PATCH` | `/api/passworditems/{id}/soft-delete` | Move to Recently Deleted. |
| `PATCH` | `/api/passworditems/{id}/restore` | Restore from Recently Deleted. |
| `PATCH` | `/api/passworditems/{id}/toggle-favorite` | Toggle the favorite flag. |
| `PATCH` | `/api/passworditems/{id}/archive` | Archive an item. |
| `PATCH` | `/api/passworditems/{id}/unarchive` | Unarchive an item. |

## Item types

`Type` on `CreatePasswordItemDto`/the returned `PasswordItemDto` is one of 24 values: `Login`,
`CreditCard`, `SecureNote`, `WiFi`, `Password`, `Passkey`, `Identity`, `SshKey`, `BankAccount`,
`Database`, `DriversLicense`, `EmailAccount`, `MedicalRecord`, `Membership`, `OutdoorLicense`,
`Passport`, `RewardsProgram`, `Server`, `SocialSecurityNumber`, `SoftwareLicense`, `WirelessRouter`,
`CryptoWallet`, `Document`, `ApiCredentials`.

## Decrypting & revealing secrets

Two different flows, both requiring a **session token** (`Authorization: Bearer <token>` from
`/api/auth/login/*`) in addition to `X-API-Key`:

| Method | Route | Description |
| --- | --- | --- |
| `POST` | `/api/passworditems/{id}/decrypt` | Session-based decrypt - uses the vault session established at sign-in. Returns a `DecryptedPasswordItemDto` with plaintext fields for a login item. |
| `POST` | `/api/passworditems/{id}/reveal` | Master-password-based reveal - body includes `masterPassword`, re-verified against the stored hash before decrypting. Requires `Passwords.ViewSensitive`; **never** available to child accounts. |
| `POST` | `/api/passworditems/encrypted` | Create an item where sensitive fields are encrypted client-side-adjacent using the session's master password before storage (`CreateEncryptedPasswordItemDto`). |

## Example: create a login item

```
POST /api/passworditems
X-API-Key: <your-key>
Content-Type: application/json

{
  "title": "GitHub",
  "type": "Login",
  "categoryId": 3,
  "collectionId": 1,
  "loginItem": {
    "website": "https://github.com",
    "username": "johndoe-dev",
    "password": "correct horse battery staple"
  },
  "tagIds": [5, 9]
}
```

Returns `201 Created` with the new `PasswordItemDto` and a `Location` header pointing at
`GET /api/passworditems/{id}`.
