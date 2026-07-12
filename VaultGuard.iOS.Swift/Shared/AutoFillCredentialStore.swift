// AutoFillCredentialStore.swift — the bridge between the main app and the AutoFill extension.
//
// Compiled into BOTH targets (the app writes; the extension reads). Credentials are stored as individual
// generic-password Keychain items in a **shared keychain access group** so the extension process can read
// them; the OS keeps them encrypted at rest. The app also registers ASPasswordCredentialIdentity entries so
// VaultGuard's saved logins appear in the iOS QuickType bar.
//
// SETUP: set `accessGroup` below to "<YOUR_TEAM_ID>.com.vaultguard.shared" (the value declared in both
// targets' `keychain-access-groups` entitlement). Until then it falls back to the target's default keychain,
// which works within a single process but not across the app↔extension boundary.
import Foundation
import AuthenticationServices

struct AutoFillCred: Codable {
    let identifier: String   // service/domain, e.g. "github.com"
    let username: String
    let password: String
}

enum AutoFillCredentialStore {
    /// Shared keychain access group — set to "<TEAM_ID>.com.vaultguard.shared" once your team id is known.
    static let accessGroup: String? = nil
    private static let service = "com.vaultguard.autofill"

    // MARK: - Write (app side)

    /// Replace the stored credential set and refresh the QuickType identity store.
    static func save(_ creds: [AutoFillCred]) {
        clearAll()
        for c in creds where !c.identifier.isEmpty && !c.username.isEmpty {
            add(c)
        }
        registerIdentities(creds)
    }

    private static func add(_ c: AutoFillCred) {
        guard let data = try? JSONEncoder().encode(c) else { return }
        var query: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrService as String: service,
            kSecAttrAccount as String: account(c.identifier, c.username),
            kSecValueData as String: data,
            kSecAttrAccessible as String: kSecAttrAccessibleAfterFirstUnlockThisDeviceOnly,
        ]
        if let g = accessGroup { query[kSecAttrAccessGroup as String] = g }
        SecItemDelete(query as CFDictionary)
        SecItemAdd(query as CFDictionary, nil)
    }

    static func clearAll() {
        var query: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrService as String: service,
        ]
        if let g = accessGroup { query[kSecAttrAccessGroup as String] = g }
        SecItemDelete(query as CFDictionary)
    }

    // MARK: - Read (extension side)

    static func all() -> [AutoFillCred] {
        var query: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrService as String: service,
            kSecReturnData as String: true,
            kSecMatchLimit as String: kSecMatchLimitAll,
        ]
        if let g = accessGroup { query[kSecAttrAccessGroup as String] = g }
        var result: AnyObject?
        guard SecItemCopyMatching(query as CFDictionary, &result) == errSecSuccess else { return [] }
        let items = (result as? [Data]) ?? []
        return items.compactMap { try? JSONDecoder().decode(AutoFillCred.self, from: $0) }
    }

    static func password(for identifier: String, username: String) -> String? {
        let key = normalize(identifier)
        return all().first {
            $0.username == username &&
            (normalize($0.identifier) == key || key.contains(normalize($0.identifier)) || normalize($0.identifier).contains(key))
        }?.password
    }

    // MARK: - QuickType identities

    static func registerIdentities(_ creds: [AutoFillCred]) {
        let identities = creds.map {
            ASPasswordCredentialIdentity(
                serviceIdentifier: ASCredentialServiceIdentifier(identifier: $0.identifier, type: .domain),
                user: $0.username,
                recordIdentifier: nil
            )
        }
        ASCredentialIdentityStore.shared.getState { state in
            guard state.isEnabled else { return }
            ASCredentialIdentityStore.shared.replaceCredentialIdentities(with: identities, completion: nil)
        }
    }

    // MARK: - Helpers

    private static func account(_ identifier: String, _ username: String) -> String { "\(identifier)\u{1F}\(username)" }
    private static func normalize(_ s: String) -> String {
        s.lowercased().replacingOccurrences(of: "https://", with: "")
            .replacingOccurrences(of: "http://", with: "")
            .replacingOccurrences(of: "www.", with: "")
            .split(separator: "/").first.map(String.init) ?? s.lowercased()
    }
}
