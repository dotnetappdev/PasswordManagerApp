# VaultGuard iOS AutoFill Credential Provider

Lets VaultGuard fill saved logins system-wide (Settings › General › AutoFill & Passwords › VaultGuard).

## What's in the repo
- `AutoFillExtension/CredentialProviderViewController.swift` — the `ASCredentialProviderViewController`.
- `AutoFillExtension/Info.plist` — declares the credential-provider extension point.
- `AutoFillExtension/AutoFill.entitlements` — AutoFill capability + shared keychain group + App Group.
- `VaultGuard/VaultGuard.entitlements` — same capabilities on the main app.
- `Shared/AutoFillCredentialStore.swift` — compiled into **both** targets; the app writes the credential
  set + registers QuickType identities, the extension reads them.
- `project.yml` already declares the `VaultGuardAutoFill` app-extension target and wires it as a dependency
  of the app.

The app publishes credentials after each vault load via `VaultRepository.syncAutoFill()` (LOCAL mode).

## One-time setup in Xcode / Apple Developer
1. **Team & bundle IDs.** Set `DEVELOPMENT_TEAM` in `project.yml`. The app is `com.vaultguard.app`; the
   extension is `com.vaultguard.app.autofill` (must be a child of the app id).
2. **Regenerate the project:** `cd VaultGuard.iOS.Swift && xcodegen generate`, then open `VaultGuard.xcodeproj`.
3. **App Group:** in the Apple Developer portal create App Group `group.com.vaultguard.app` and enable it on
   both App IDs (already referenced in both entitlements files).
4. **Shared keychain group:** set `AutoFillCredentialStore.accessGroup` to `"<YOUR_TEAM_ID>.com.vaultguard.shared"`
   (the value that `$(AppIdentifierPrefix)com.vaultguard.shared` resolves to). Until then the store falls back
   to the per-process default keychain, so the extension won't see the app's items across the process boundary.
5. **Capabilities:** confirm both targets show *AutoFill Credential Provider*, *Keychain Sharing*
   (`com.vaultguard.shared`), and *App Groups* (`group.com.vaultguard.app`) in Signing & Capabilities.

## Enable on device
Settings › General › AutoFill & Passwords → turn on **VaultGuard**. Saved logins then appear in the QuickType
bar and in the per-field password menu.

## Security note
Credentials are stored as individual generic-password keychain items in the shared access group (OS-encrypted,
`ThisDeviceOnly`). For an extra hardening step you can gate `provideCredentialWithoutUserInteraction` behind a
`LAContext` biometric check before returning the password.
