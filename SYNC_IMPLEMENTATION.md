# API Key Authentication and Sync Implementation

## Overview

This implementation ensures that the Password Manager API is only called when the Sync button is clicked, and all API endpoints are secured with API key authentication.

## Key Features

### 1. API Key Authentication
- **Middleware**: `ApiKeyAuthenticationMiddleware` validates API keys in the `X-API-Key` header
- **Automatic User Context**: API keys are linked to user records for user-scoped operations
- **Secure Validation**: API keys are hashed and stored securely

### 2. Sync Button on Home Page
- **Prominent Sync Button**: Added to the main app home page header
- **Modal Interface**: Comprehensive sync modal with API key configuration
- **Manual Trigger**: Sync only happens when the user clicks the sync button

### 3. API Key Generation
- **Endpoint**: `POST /api/authentication/generate-api-key`
- **User Linking**: API keys are linked to user records
- **Secure Storage**: Keys are hashed before storage

### 4. Database Provider Support
- **Multiple Providers**: Supports SqlServer, MySQL, PostgreSQL, Supabase
- **Configuration**: Database provider configured in appsettings.json

## Usage Instructions

### Step 1: Generate API Key
```bash
curl -X POST "https://localhost:7001/api/authentication/generate-api-key" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "MyPasswordManagerApp",
    "userId": "user123"
  }'
```

Response:
```json
{
  "apiKey": "ABC123XYZ789...",
  "userId": "user123",
  "keyName": "MyPasswordManagerApp",
  "instructions": "Store this API key securely. You won't be able to see it again. Include it in the 'X-API-Key' header when making requests to the sync API."
}
```

### Step 2: Configure API Key in App
1. Launch the Password Manager app
2. Click the **"🔄 Sync"** button on the home page
3. In the sync modal, paste your API key
4. Click **"Configure API Key"**

### Step 3: Sync Data
1. With the API key configured, the sync modal will show the API status
2. Click **"Start Sync"** to begin the sync process
3. The app will sync data bidirectionally with the configured database

## Security Features

### API Key Protection
- All sync endpoints require valid API key authentication
- API keys are hashed using SHA256 before storage
- User context is preserved through API key validation
- Invalid API key attempts are logged

### User-Scoped Operations
- All sync operations are scoped to the authenticated user
- Data isolation between users
- Secure master password handling

## Configuration

### App Configuration (appsettings.json)
```json
{
  "Api": {
    "BaseUrl": "https://localhost:7001"
  },
  "DatabaseProvider": "SqlServer",
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=PasswordManager;..."
  }
}
```

### API Configuration
The API automatically configures the database provider based on the `DatabaseProvider` setting and uses the appropriate connection string.

## Testing the Implementation

### 1. Manual Testing
1. Start the API: `dotnet run --project PasswordManager.API`
2. Start the app: `dotnet run --project PasswordManager.App`
3. Generate API key via the authentication endpoint
4. Configure API key in the app's sync modal
5. Click sync button to trigger sync

### 2. API Testing
```bash
# Generate API key
curl -X POST "https://localhost:7001/api/authentication/generate-api-key" \
  -H "Content-Type: application/json" \
  -d '{"name": "Test", "userId": "test123"}'

# Use API key for sync
curl -X POST "https://localhost:7001/api/sync/sync" \
  -H "Content-Type: application/json" \
  -H "X-API-Key: YOUR_API_KEY_HERE" \
  -d '{
    "sourceDatabase": "LocalSqlite",
    "targetDatabase": "SqlServer",
    "entitiesToSync": ["PasswordItems", "Collections"]
  }'
```

## Architecture

### Components
- **ApiKeyAuthenticationMiddleware**: Validates API keys and sets user context
- **AuthenticationController**: Handles API key generation and validation
- **SyncController**: Manages sync operations (requires authentication)
- **AppSyncService**: Client-side sync service with API key support
- **Home.razor**: UI with sync button and modal

### Flow
1. User clicks sync button → Opens sync modal
2. User configures API key → Stored in AppSyncService
3. User clicks start sync → Triggers `SyncManuallyAsync()`
4. Service calls API with API key → Middleware validates key
5. Sync operation executes → Data synchronized with database

## Compliance with Requirements

✅ **API only called when Sync button is clicked**
- Sync functionality moved to manual trigger
- No automatic sync calls without user interaction

✅ **Import data to different database providers**
- Supports SqlServer, MySQL, PostgreSQL, Supabase
- Database provider configurable

✅ **API key generation endpoint**
- `/api/authentication/generate-api-key` endpoint
- Links API keys to user records

✅ **Secure API endpoints**
- All sync endpoints require API key authentication
- User context preserved through validation