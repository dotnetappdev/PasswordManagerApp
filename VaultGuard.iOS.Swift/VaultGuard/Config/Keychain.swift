// Keychain.swift — minimal secure string storage for secrets (API key, session token, local vault salt).
import Foundation
import Security
import LocalAuthentication

// @unchecked Sendable: every method is stateless (re-queries the OS keychain each call) and the only
// stored property is an immutable `let` — genuinely safe to call from a background Task (needed so
// the blocking Face ID/Touch ID prompt in getBiometricProtected doesn't run on the main actor).
final class Keychain: @unchecked Sendable {
    private let service = "com.vaultguard.app"

    func set(_ value: String?, for key: String) {
        let query: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrService as String: service,
            kSecAttrAccount as String: key,
        ]
        SecItemDelete(query as CFDictionary)
        guard let value, let data = value.data(using: .utf8) else { return }
        var add = query
        add[kSecValueData as String] = data
        add[kSecAttrAccessible as String] = kSecAttrAccessibleAfterFirstUnlockThisDeviceOnly
        SecItemAdd(add as CFDictionary, nil)
    }

    func get(_ key: String) -> String? {
        let query: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrService as String: service,
            kSecAttrAccount as String: key,
            kSecReturnData as String: true,
            kSecMatchLimit as String: kSecMatchLimitOne,
        ]
        var result: AnyObject?
        guard SecItemCopyMatching(query as CFDictionary, &result) == errSecSuccess,
              let data = result as? Data else { return nil }
        return String(data: data, encoding: .utf8)
    }

    func delete(_ key: String) { set(nil, for: key) }

    // MARK: - Biometric-gated storage
    //
    // Unlike `set`/`get` above, these items carry a `kSecAttrAccessControl` of `.biometryCurrentSet`.
    // The OS keychain daemon — not this code — refuses to return the data without a fresh, successful
    // Face ID/Touch ID (or device-passcode fallback) evaluation, and auto-invalidates the item if the
    // enrolled biometrics change. This is the same security property as a hardware-bound key on
    // Android/Windows: the gate is enforced by the platform, not by an app-level boolean check.

    /// Stores [value] behind Face ID/Touch ID. Deletes any existing item under [key] first.
    func setBiometricProtected(_ value: String?, for key: String) {
        let query: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrService as String: service,
            kSecAttrAccount as String: key,
        ]
        SecItemDelete(query as CFDictionary)
        guard let value, let data = value.data(using: .utf8) else { return }
        guard let access = SecAccessControlCreateWithFlags(
            nil, kSecAttrAccessibleWhenUnlockedThisDeviceOnly, .biometryCurrentSet, nil
        ) else { return }
        var add = query
        add[kSecValueData as String] = data
        add[kSecAttrAccessControl as String] = access
        SecItemAdd(add as CFDictionary, nil)
    }

    /// Reads a value stored via `setBiometricProtected`, triggering the system Face ID/Touch ID prompt.
    /// Blocks while the OS shows its UI — call from a background thread/Task, never the main thread.
    func getBiometricProtected(_ key: String, prompt: String) -> String? {
        let context = LAContext()
        context.localizedReason = prompt
        let query: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrService as String: service,
            kSecAttrAccount as String: key,
            kSecReturnData as String: true,
            kSecMatchLimit as String: kSecMatchLimitOne,
            kSecUseAuthenticationContext as String: context,
        ]
        var result: AnyObject?
        guard SecItemCopyMatching(query as CFDictionary, &result) == errSecSuccess,
              let data = result as? Data else { return nil }
        return String(data: data, encoding: .utf8)
    }

    /// True if a biometric-protected item exists under [key] — checked WITHOUT triggering the Face
    /// ID/Touch ID prompt (`kSecUseAuthenticationUISkip`), so it's safe to call on every screen load.
    func hasBiometricProtected(_ key: String) -> Bool {
        let query: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrService as String: service,
            kSecAttrAccount as String: key,
            kSecReturnData as String: false,
            kSecMatchLimit as String: kSecMatchLimitOne,
            kSecUseAuthenticationUI as String: kSecUseAuthenticationUISkip,
        ]
        var result: AnyObject?
        let status = SecItemCopyMatching(query as CFDictionary, &result)
        // errSecInteractionNotAllowed = the item exists but needs the (skipped) auth UI to read it.
        return status == errSecSuccess || status == errSecInteractionNotAllowed
    }

    enum Keys {
        static let apiKey = "api_key"
        static let session = "session_token"
        static let localSalt = "local_vault_salt"
        static let localVerifier = "local_verifier"
        static let passcode = "app_passcode"
        static let onePasswordHost = "onepassword_connect_host"
        static let onePasswordToken = "onepassword_connect_token"
        static let quickUnlockBiometric = "quick_unlock_master_biometric"
    }
}
