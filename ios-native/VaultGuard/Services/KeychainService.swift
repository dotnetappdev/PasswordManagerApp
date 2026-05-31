import Foundation
import Security

class KeychainService {
    static let shared = KeychainService()
    private init() {}

    private let service = "com.vaultguard.app"

    // MARK: - Keys

    private enum Keys {
        static let masterPasswordHash = "masterPasswordHash"
        static let masterPasswordSalt = "masterPasswordSalt"
        static let encryptionKeySalt = "encryptionKeySalt"
        static let sessionEncryptionKey = "sessionEncryptionKey"
        static let biometricsEnabled = "biometricsEnabled"
    }

    // MARK: - Generic CRUD

    func set(_ value: Data, for key: String, biometricProtected: Bool = false) throws {
        let query: [CFString: Any] = [
            kSecClass: kSecClassGenericPassword,
            kSecAttrService: service,
            kSecAttrAccount: key,
        ]

        var attributes: [CFString: Any] = [
            kSecValueData: value,
        ]

        if biometricProtected {
            guard let access = SecAccessControlCreateWithFlags(
                nil,
                kSecAttrAccessibleWhenPasscodeSetThisDeviceOnly,
                .biometryCurrentSet,
                nil
            ) else {
                throw KeychainError.unableToCreateAccessControl
            }
            attributes[kSecAttrAccessControl] = access
        } else {
            attributes[kSecAttrAccessible] = kSecAttrAccessibleAfterFirstUnlockThisDeviceOnly
        }

        // First try updating existing
        let updateStatus = SecItemUpdate(query as CFDictionary, attributes as CFDictionary)

        if updateStatus == errSecItemNotFound {
            // Add new item
            var addQuery = query
            addQuery.merge(attributes) { $1 }
            let addStatus = SecItemAdd(addQuery as CFDictionary, nil)
            guard addStatus == errSecSuccess else {
                throw KeychainError.unableToStore(addStatus)
            }
        } else if updateStatus != errSecSuccess {
            throw KeychainError.unableToStore(updateStatus)
        }
    }

    func get(_ key: String) throws -> Data? {
        let query: [CFString: Any] = [
            kSecClass: kSecClassGenericPassword,
            kSecAttrService: service,
            kSecAttrAccount: key,
            kSecReturnData: true,
            kSecMatchLimit: kSecMatchLimitOne,
        ]

        var result: AnyObject?
        let status = SecItemCopyMatching(query as CFDictionary, &result)

        if status == errSecItemNotFound {
            return nil
        }

        guard status == errSecSuccess else {
            throw KeychainError.unableToRetrieve(status)
        }

        return result as? Data
    }

    func delete(_ key: String) {
        let query: [CFString: Any] = [
            kSecClass: kSecClassGenericPassword,
            kSecAttrService: service,
            kSecAttrAccount: key,
        ]
        SecItemDelete(query as CFDictionary)
    }

    // MARK: - Master Password Hash

    func storeMasterPasswordHash(_ hash: Data) throws {
        try set(hash, for: Keys.masterPasswordHash)
    }

    func getMasterPasswordHash() -> Data? {
        try? get(Keys.masterPasswordHash)
    }

    // MARK: - Salts

    func storeMasterPasswordSalt(_ salt: Data) throws {
        try set(salt, for: Keys.masterPasswordSalt)
    }

    func getMasterPasswordSalt() -> Data? {
        try? get(Keys.masterPasswordSalt)
    }

    func storeEncryptionKeySalt(_ salt: Data) throws {
        try set(salt, for: Keys.encryptionKeySalt)
    }

    func getEncryptionKeySalt() -> Data? {
        try? get(Keys.encryptionKeySalt)
    }

    // MARK: - Session Encryption Key

    func storeSessionKey(_ keyData: Data) throws {
        try set(keyData, for: Keys.sessionEncryptionKey)
    }

    func getSessionKey() -> Data? {
        try? get(Keys.sessionEncryptionKey)
    }

    func clearSessionKey() {
        delete(Keys.sessionEncryptionKey)
    }

    // MARK: - Has Items

    func hasMasterPassword() -> Bool {
        getMasterPasswordHash() != nil
    }

    // MARK: - Clear All

    func clearAll() {
        let keys = [
            Keys.masterPasswordHash,
            Keys.masterPasswordSalt,
            Keys.encryptionKeySalt,
            Keys.sessionEncryptionKey,
        ]
        keys.forEach { delete($0) }
    }
}

// MARK: - Errors

enum KeychainError: LocalizedError {
    case unableToStore(OSStatus)
    case unableToRetrieve(OSStatus)
    case unableToCreateAccessControl
    case itemNotFound

    var errorDescription: String? {
        switch self {
        case .unableToStore(let status):
            return "Unable to store item in Keychain (status: \(status))"
        case .unableToRetrieve(let status):
            return "Unable to retrieve item from Keychain (status: \(status))"
        case .unableToCreateAccessControl:
            return "Unable to create biometric access control"
        case .itemNotFound:
            return "Item not found in Keychain"
        }
    }
}
