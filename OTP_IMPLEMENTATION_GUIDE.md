# OTP (One-Time Passcode) Implementation Guide

## Overview

This implementation adds SMS-based Two-Factor Authentication (2FA) to the Password Manager application. The OTP functionality is available on **web browsers**, **Android**, and **iOS** platforms only, excluding desktop applications as per requirements.

## Features

- **SMS OTP**: Six-digit verification codes sent via SMS
- **Multiple SMS Providers**: Support for Twilio, AWS SNS, and Azure Communication Services
- **Platform Detection**: Automatically restricts OTP to supported platforms
- **Backup Codes**: Eight-digit recovery codes for account access
- **Rate Limiting**: Configurable limits to prevent abuse
- **Security**: Hashed OTP codes, encrypted backup codes

## API Endpoints

### Setup OTP
```http
POST /api/auth/otp/setup
Authorization: Bearer {session_token}
Content-Type: application/json

{
  "phoneNumber": "+1234567890"
}
```

### Verify OTP Setup
```http
POST /api/auth/otp/verify-setup
Authorization: Bearer {session_token}
Content-Type: application/json

{
  "phoneNumber": "+1234567890",
  "code": "123456"
}
```

### Send OTP for Login
```http
POST /api/auth/otp/send
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "user_password"
}
```

### Login with OTP
```http
POST /api/auth/otp/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "user_password",
  "otpCode": "123456"
}
```

### Disable OTP
```http
POST /api/auth/otp/disable
Authorization: Bearer {session_token}
Content-Type: application/json

{
  "password": "user_password",
  "otpCode": "123456"
  // OR use backup code instead:
  // "backupCode": "12345678"
}
```

## Configuration

Add the following configuration to `appsettings.json`:

```json
{
  "SmsSettings": {
    "Provider": "Twilio",
    "Enabled": true,
    "DefaultCountryCode": "+1",
    "CodeLength": 6,
    "ExpirationMinutes": 5,
    "MaxAttempts": 3,
    "MaxSmsPerHour": 10,
    "MessageTemplate": "Your Password Manager verification code is: {code}. This code will expire in {expiration} minutes.",
    "Twilio": {
      "AccountSid": "your_twilio_account_sid",
      "AuthToken": "your_twilio_auth_token",
      "FromPhoneNumber": "+1234567890"
    },
    "AwsSns": {
      "AccessKeyId": "your_aws_access_key",
      "SecretAccessKey": "your_aws_secret_key",
      "Region": "us-east-1",
      "SenderName": "Password Manager"
    },
    "AzureCommunication": {
      "ConnectionString": "your_azure_connection_string",
      "FromPhoneNumber": "+1234567890"
    }
  }
}
```

## Platform Support

| Platform | OTP Support | Reason |
|----------|-------------|---------|
| Web Browsers | ✅ | Supported - Users can receive SMS on their phones |
| Android Mobile | ✅ | Supported - Native mobile platform |
| iOS Mobile | ✅ | Supported - Native mobile platform |
| Windows Desktop | ❌ | Not supported - Desktop app restriction |
| macOS Desktop | ❌ | Not supported - Desktop app restriction |
| Linux Desktop | ❌ | Not supported - Desktop app restriction |

The platform detection is automatic based on the User-Agent string.

## Security Features

1. **Code Hashing**: OTP codes are hashed before storage using SHA-256
2. **Expiration**: Codes expire after a configurable time (default: 5 minutes)
3. **Rate Limiting**: Maximum number of SMS per hour per user
4. **Attempt Limiting**: Maximum verification attempts before lockout
5. **Backup Codes**: Encrypted recovery codes for emergency access
6. **Platform Restriction**: OTP only works on approved platforms

## Database Schema

The implementation adds an `OtpCodes` table with the following structure:

```sql
CREATE TABLE OtpCodes (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId NVARCHAR(450) NOT NULL,
    CodeHash NVARCHAR(500) NOT NULL,
    PhoneNumber NVARCHAR(20) NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    ExpiresAt DATETIME2 NOT NULL,
    AttemptCount INT NOT NULL DEFAULT 0,
    IsUsed BIT NOT NULL DEFAULT 0,
    UsedAt DATETIME2 NULL,
    RequestIpAddress NVARCHAR(45) NULL,
    RequestUserAgent NVARCHAR(500) NULL,
    
    FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE
);
```

Additional fields added to `ApplicationUser`:

- `IsTwoFactorEnabled`: Boolean flag
- `PhoneNumber`: E.164 formatted phone number
- `PhoneNumberConfirmed`: Verification status
- `PhoneNumberConfirmedAt`: Verification timestamp
- `BackupCodes`: Encrypted JSON array of recovery codes
- `BackupCodesUsed`: Count of used backup codes

## SMS Providers

### Twilio Setup
1. Create a Twilio account at https://www.twilio.com/
2. Get your Account SID and Auth Token from the console
3. Purchase a phone number for sending SMS
4. Update the configuration with your credentials

### AWS SNS Setup
1. Create an AWS account and enable SNS
2. Create IAM credentials with SNS permissions
3. Update the configuration with your credentials

### Azure Communication Services Setup
1. Create an Azure Communication Services resource
2. Get the connection string from the Azure portal
3. Purchase a phone number for SMS
4. Update the configuration with your credentials

## Error Handling

The implementation includes comprehensive error handling:

- Invalid phone number format
- SMS sending failures
- Rate limit exceeded
- Platform not supported
- OTP code expired or invalid
- Maximum attempts exceeded

## Testing

Run the OTP tests with:

```bash
dotnet test PasswordManager.Tests.OTP
```

The test suite includes:
- Platform detection tests
- OTP code generation tests
- Service configuration tests

## Next Steps

1. **UI Components**: Add OTP setup and verification UI to web and mobile apps
2. **Database Migration**: Create and run migration for OTP tables
3. **Documentation**: Update user documentation with OTP setup instructions
4. **Monitoring**: Add logging and monitoring for OTP operations

## Security Considerations

- Store SMS provider credentials securely (use Azure Key Vault, AWS Secrets Manager, etc.)
- Monitor for unusual OTP request patterns
- Implement CAPTCHA for repeated failed attempts
- Consider implementing device fingerprinting
- Regular security audits of OTP implementation