# Authentication Service Context Fix

This document explains the resolution for the `System.InvalidOperationException` error when trying to resolve `IJSRuntime` while activating `IdentityAuthService`.

## Problem

The original error occurred because:

1. `IdentityAuthService` and `AuthService` both require `IJSRuntime` in their constructors
2. `IJSRuntime` is only available in Blazor contexts (Blazor Server, Blazor WebAssembly, MAUI Blazor WebView)
3. When these services were used in non-Blazor contexts (Web APIs, console apps, background services), the DI container couldn't resolve `IJSRuntime`

## Solution

### 1. New ServerAuthService

Created a new `ServerAuthService` class that implements `IAuthService` without requiring `IJSRuntime`. This service:

- Uses `ISecureStorageService` for platform-specific secure storage when available
- Falls back to database storage for user salt information
- Manages authentication state without JavaScript interop
- Is suitable for API contexts, console applications, and other non-Blazor environments

### 2. Contextual Service Registration

Added `AuthServiceExtensions` class with methods for smart service registration:

- `AddContextualAuthService()`: Automatically chooses the appropriate implementation based on whether `IJSRuntime` is registered
- `AddBlazorAuthService()`: Explicitly registers `IdentityAuthService` for Blazor contexts
- `AddServerAuthService()`: Explicitly registers `ServerAuthService` for non-Blazor contexts

## Usage

### For Blazor Applications (MAUI, WinUI with Blazor WebView)

```csharp
services.AddBlazorAuthService();
```

### For Web Applications (Blazor Server)

```csharp
services.AddContextualAuthService();
// Will automatically select IdentityAuthService since IJSRuntime is available
```

### For Web APIs and Console Applications

```csharp
services.AddContextualAuthService();
// Will automatically select ServerAuthService since IJSRuntime is not available
```

### Explicit Registration

If you want to be explicit about which implementation to use:

```csharp
// For contexts where JavaScript interop is available
services.AddBlazorAuthService();

// For contexts without JavaScript interop
services.AddServerAuthService();
```

## Implementation Details

### ServerAuthService Features

- **No JavaScript Dependencies**: Does not require `IJSRuntime`
- **Secure Storage**: Uses `ISecureStorageService` when available, falls back to database
- **Session Management**: Manages authentication state using the existing `IVaultSessionService`
- **Platform Agnostic**: Works in any .NET environment
- **Full IAuthService Compatibility**: Implements all methods from the interface

### Storage Strategy

1. **User Salt Storage**:
   - Primary: Uses `ISecureStorageService` for platform-specific secure storage
   - Fallback: Retrieves from database `UserSalt` field

2. **Authentication State**:
   - Primary: Uses `ISecureStorageService` for session information
   - Fallback: Can be extended to use Redis, database, or other server-side storage

## Migration

Existing projects are automatically updated to use the contextual registration:

1. **PasswordManager.API**: Uses `AddContextualAuthService()` → `ServerAuthService`
2. **PasswordManager.Web**: Uses `AddContextualAuthService()` → `IdentityAuthService` (Blazor Server has IJSRuntime)
3. **PasswordManager.App**: Uses `AddBlazorAuthService()` → `IdentityAuthService`  
4. **PasswordManager.WinUi**: Uses `AddBlazorAuthService()` → `IdentityAuthService`

## Testing

Added unit tests in `PasswordManager.BackEnd.Tests` to verify:

- Contextual registration works correctly
- Appropriate service is selected based on context
- Explicit registration methods work as expected

## Benefits

1. **Backward Compatibility**: Existing Blazor applications continue to use JavaScript interop features
2. **Platform Flexibility**: APIs and console apps can now use authentication services
3. **Automatic Selection**: No manual configuration needed in most cases
4. **Clean Architecture**: Separation of concerns between client-side and server-side auth logic
5. **Future Proof**: Easy to extend for new contexts or storage mechanisms