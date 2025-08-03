# 2FA and Passkey API Documentation

## Authentication Endpoints

### Enhanced Login
**POST** `/api/auth/login/enhanced`

Enhanced login with 2FA and passkey support.

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "userpassword",
  "twoFactorCode": "123456",        // Optional: TOTP or backup code
  "isTwoFactorBackupCode": false    // Optional: true if using backup code
}
```

**Response:**
```json
{
  "requiresTwoFactor": false,
  "supportsPasskey": true,
  "authResponse": {
    "token": "jwt_token_here",
    "refreshToken": "refresh_token",
    "expiresAt": "2024-08-03T15:30:00Z",
    "user": {
      "id": "user_id",
      "email": "user@example.com",
      "firstName": "John",
      "lastName": "Doe"
    }
  },
  "twoFactorToken": null           // Only present if 2FA required
}
```

## Two-Factor Authentication

### Get 2FA Status
**GET** `/api/twofactor/status`

Returns the current 2FA configuration for the authenticated user.

**Response:**
```json
{
  "isEnabled": true,
  "enabledAt": "2024-08-01T10:30:00Z",
  "backupCodesRemaining": 8,
  "recoveryEmail": "backup@example.com"
}
```

### Start 2FA Setup
**POST** `/api/twofactor/setup/start`

Initiates 2FA setup process, generates secret key and QR code.

**Request Body:**
```json
{
  "masterPassword": "user_master_password"
}
```

**Response:**
```json
{
  "secretKey": "JBSWY3DPEHPK3PXP",
  "qrCodeUri": "otpauth://totp/PasswordManager:user@example.com?secret=JBSWY3DPEHPK3PXP&issuer=PasswordManager",
  "backupCodes": [
    "ABC12345",
    "DEF67890",
    "GHI23456",
    "..."
  ]
}
```

### Complete 2FA Setup
**POST** `/api/twofactor/setup/complete`

Completes 2FA setup by verifying the TOTP code.

**Request Body:**
```json
{
  "code": "123456",
  "secretKey": "JBSWY3DPEHPK3PXP"
}
```

**Response:**
```json
{
  "message": "2FA has been successfully enabled"
}
```

### Disable 2FA
**POST** `/api/twofactor/disable`

Disables 2FA for the authenticated user.

**Request Body:**
```json
{
  "masterPassword": "user_master_password",
  "code": "123456"
}
```

**Response:**
```json
{
  "message": "2FA has been successfully disabled"
}
```

### Verify 2FA Code
**POST** `/api/twofactor/verify`

Verifies a 2FA code (TOTP or backup code).

**Request Body:**
```json
{
  "code": "123456",
  "isBackupCode": false
}
```

**Response:**
```json
{
  "message": "2FA code verified successfully"
}
```

### Regenerate Backup Codes
**POST** `/api/twofactor/backup-codes/regenerate`

Generates new backup codes, invalidating old ones.

**Request Body:**
```json
{
  "masterPassword": "user_master_password",
  "code": "123456"
}
```

**Response:**
```json
{
  "backupCodes": [
    "NEW12345",
    "XYZ67890",
    "..."
  ],
  "message": "Backup codes have been regenerated successfully"
}
```

## Passkey Management

### Get Passkey Status
**GET** `/api/passkey/status`

Returns the current passkey configuration for the authenticated user.

**Response:**
```json
{
  "isEnabled": true,
  "enabledAt": "2024-08-01T10:30:00Z",
  "passkeyCount": 2,
  "storeInVault": true
}
```

### List User Passkeys
**GET** `/api/passkey`

Returns all passkeys for the authenticated user.

**Response:**
```json
{
  "passkeys": [
    {
      "id": 1,
      "name": "iPhone 15 Pro",
      "deviceType": "iPhone",
      "isBackedUp": true,
      "requiresUserVerification": true,
      "createdAt": "2024-08-01T10:30:00Z",
      "lastUsedAt": "2024-08-03T08:15:00Z",
      "isActive": true,
      "storeInVault": true
    },
    {
      "id": 2,
      "name": "Windows Hello",
      "deviceType": "Windows",
      "isBackedUp": false,
      "requiresUserVerification": true,
      "createdAt": "2024-08-02T14:20:00Z",
      "lastUsedAt": null,
      "isActive": true,
      "storeInVault": false
    }
  ],
  "passkeysEnabled": true,
  "passkeysEnabledAt": "2024-08-01T10:30:00Z"
}
```

### Start Passkey Registration
**POST** `/api/passkey/register/start`

Initiates passkey registration process.

**Request Body:**
```json
{
  "masterPassword": "user_master_password",
  "passkeyName": "iPhone 15 Pro",
  "storeInVault": true
}
```

**Response:**
```json
{
  "challenge": "base64_encoded_challenge",
  "credentialCreationOptions": "{webauthn_creation_options_json}"
}
```

### Complete Passkey Registration
**POST** `/api/passkey/register/complete`

Completes passkey registration with WebAuthn response.

**Request Body:**
```json
{
  "challenge": "base64_encoded_challenge",
  "credentialResponse": "{webauthn_credential_response_json}",
  "passkeyName": "iPhone 15 Pro",
  "storeInVault": true,
  "deviceType": "iPhone"
}
```

**Response:**
```json
{
  "message": "Passkey has been successfully registered"
}
```

### Start Passkey Authentication
**POST** `/api/passkey/authenticate/start`

Initiates passkey authentication process (no authentication required).

**Request Body:**
```json
{
  "email": "user@example.com"
}
```

**Response:**
```json
{
  "challenge": "base64_encoded_challenge",
  "credentialRequestOptions": "{webauthn_request_options_json}"
}
```

### Complete Passkey Authentication
**POST** `/api/passkey/authenticate/complete`

Completes passkey authentication (no authentication required).

**Request Body:**
```json
{
  "challenge": "base64_encoded_challenge",
  "credentialResponse": "{webauthn_assertion_response_json}"
}
```

**Response:**
```json
{
  "token": "jwt_token_here",
  "refreshToken": "refresh_token",
  "expiresAt": "2024-08-03T15:30:00Z",
  "user": {
    "id": "user_id",
    "email": "user@example.com",
    "firstName": "John",
    "lastName": "Doe"
  }
}
```

### Delete Passkey
**DELETE** `/api/passkey/{passkeyId}`

Deletes a specific passkey.

**Request Body:**
```json
{
  "masterPassword": "user_master_password"
}
```

**Response:**
```json
{
  "message": "Passkey has been successfully deleted"
}
```

### Update Passkey Settings
**PUT** `/api/passkey/settings`

Updates passkey settings for the user.

**Request Body:**
```json
{
  "storeInVault": true,
  "masterPassword": "user_master_password"
}
```

**Response:**
```json
{
  "message": "Passkey settings have been updated successfully"
}
```

## Error Responses

All endpoints return consistent error responses:

**400 Bad Request:**
```json
{
  "message": "Invalid request parameters",
  "errors": {
    "fieldName": ["Error message"]
  }
}
```

**401 Unauthorized:**
```json
{
  "message": "Authentication required"
}
```

**403 Forbidden:**
```json
{
  "message": "Access denied"
}
```

**500 Internal Server Error:**
```json
{
  "message": "An error occurred while processing your request"
}
```

## Authentication Requirements

- All endpoints except passkey authentication start/complete require authentication
- Authentication is provided via Bearer token in Authorization header
- Tokens can be obtained via `/api/auth/login` or `/api/auth/login/enhanced`

## Rate Limiting

- Authentication endpoints: 5 requests per minute per IP
- 2FA verification: 10 requests per minute per user
- Passkey operations: 20 requests per minute per user

## Cross-Platform Considerations

### Web Applications
- WebAuthn requires HTTPS in production
- Modern browser support required
- Fallback to 2FA if WebAuthn not supported

### Mobile Applications
- Platform-specific biometric authentication
- Secure enclave/keystore integration
- Cross-device synchronization support

### Desktop Applications
- Windows Hello integration
- macOS Touch ID/Face ID support
- Hardware security key support