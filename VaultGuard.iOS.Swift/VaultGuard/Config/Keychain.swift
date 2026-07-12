// Keychain.swift — minimal secure string storage for secrets (API key, session token, local vault salt).
import Foundation
import Security

final class Keychain {
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

    enum Keys {
        static let apiKey = "api_key"
        static let session = "session_token"
        static let localSalt = "local_vault_salt"
        static let localVerifier = "local_verifier"
        static let passcode = "app_passcode"
        static let onePasswordHost = "onepassword_connect_host"
        static let onePasswordToken = "onepassword_connect_token"
    }
}
