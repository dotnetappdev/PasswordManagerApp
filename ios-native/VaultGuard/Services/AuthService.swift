import Foundation
import LocalAuthentication
import CryptoKit

class AuthService {
    static let shared = AuthService()
    private init() {}

    // MARK: - State

    var isBiometricsEnabled: Bool {
        get { UserDefaults.standard.bool(forKey: "biometricsEnabled") }
        set { UserDefaults.standard.set(newValue, forKey: "biometricsEnabled") }
    }

    // MARK: - First Launch Check

    func hasMasterPassword() -> Bool {
        KeychainService.shared.hasMasterPassword()
    }

    // MARK: - Setup Master Password

    func setupMasterPassword(_ password: String) throws {
        guard password.count >= 8 else {
            throw AuthError.passwordTooShort
        }

        // Generate salts
        let passwordSalt = EncryptionService.shared.generateSalt(length: 32)
        let encKeySalt = EncryptionService.shared.generateSalt(length: 32)

        // Derive password hash for verification
        let passwordHash = try EncryptionService.shared.derivePasswordHash(
            password: password,
            salt: passwordSalt
        )

        // Derive encryption key
        let encryptionKey = try EncryptionService.shared.deriveKey(
            from: password,
            salt: encKeySalt,
            iterations: 310_000
        )

        // Store in Keychain
        try KeychainService.shared.storeMasterPasswordHash(passwordHash)
        try KeychainService.shared.storeMasterPasswordSalt(passwordSalt)
        try KeychainService.shared.storeEncryptionKeySalt(encKeySalt)

        // Set session key
        try EncryptionService.shared.setSessionKey(encryptionKey)
    }

    // MARK: - Verify Master Password

    func verifyMasterPassword(_ password: String) throws -> Bool {
        guard let storedHash = KeychainService.shared.getMasterPasswordHash(),
              let salt = KeychainService.shared.getMasterPasswordSalt() else {
            throw AuthError.noMasterPasswordSet
        }

        let inputHash = try EncryptionService.shared.derivePasswordHash(
            password: password,
            salt: salt
        )

        guard inputHash == storedHash else {
            return false
        }

        // Derive and set session encryption key
        guard let encKeySalt = KeychainService.shared.getEncryptionKeySalt() else {
            throw AuthError.keySaltMissing
        }

        let encryptionKey = try EncryptionService.shared.deriveKey(
            from: password,
            salt: encKeySalt,
            iterations: 310_000
        )

        try EncryptionService.shared.setSessionKey(encryptionKey)
        return true
    }

    // MARK: - Biometric Authentication

    func biometricType() -> LABiometryType {
        let context = LAContext()
        var error: NSError?
        guard context.canEvaluatePolicy(.deviceOwnerAuthenticationWithBiometrics, error: &error) else {
            return .none
        }
        return context.biometryType
    }

    func authenticateWithBiometrics() async throws -> Bool {
        guard isBiometricsEnabled else {
            throw AuthError.biometricsNotEnabled
        }

        let context = LAContext()
        var error: NSError?

        guard context.canEvaluatePolicy(.deviceOwnerAuthenticationWithBiometrics, error: &error) else {
            throw AuthError.biometricsNotAvailable(error?.localizedDescription ?? "Unknown error")
        }

        let reason = "Unlock VaultGuard to access your passwords"

        do {
            let success = try await context.evaluatePolicy(
                .deviceOwnerAuthenticationWithBiometrics,
                localizedReason: reason
            )

            if success {
                // Biometrics verified — we need the session key to be available
                // In practice, the key should be stored in Keychain with biometric protection
                // For this implementation, we check if a session key exists (set on last password login)
                if KeychainService.shared.getSessionKey() == nil {
                    throw AuthError.sessionKeyMissing
                }
            }

            return success
        } catch let laError as LAError {
            switch laError.code {
            case .userCancel, .appCancel:
                throw AuthError.userCancelled
            case .biometryLockout:
                throw AuthError.biometricsLocked
            default:
                throw AuthError.biometricsFailed(laError.localizedDescription)
            }
        }
    }

    // MARK: - Change Master Password

    func changeMasterPassword(current: String, new: String) throws {
        guard try verifyMasterPassword(current) else {
            throw AuthError.incorrectPassword
        }

        guard new.count >= 8 else {
            throw AuthError.passwordTooShort
        }

        // Generate new salts
        let newPasswordSalt = EncryptionService.shared.generateSalt(length: 32)
        let newEncKeySalt = EncryptionService.shared.generateSalt(length: 32)

        // Old encryption key is still in session
        // New encryption key
        let newEncKey = try EncryptionService.shared.deriveKey(
            from: new,
            salt: newEncKeySalt,
            iterations: 310_000
        )

        // TODO: Re-encrypt all items with new key
        // For now, update keys
        let newPasswordHash = try EncryptionService.shared.derivePasswordHash(
            password: new,
            salt: newPasswordSalt
        )

        try KeychainService.shared.storeMasterPasswordHash(newPasswordHash)
        try KeychainService.shared.storeMasterPasswordSalt(newPasswordSalt)
        try KeychainService.shared.storeEncryptionKeySalt(newEncKeySalt)
        try EncryptionService.shared.setSessionKey(newEncKey)
    }

    // MARK: - Lock

    func lock() {
        KeychainService.shared.clearSessionKey()
    }
}

// MARK: - Errors

enum AuthError: LocalizedError {
    case passwordTooShort
    case noMasterPasswordSet
    case keySaltMissing
    case sessionKeyMissing
    case incorrectPassword
    case biometricsNotEnabled
    case biometricsNotAvailable(String)
    case biometricsFailed(String)
    case biometricsLocked
    case userCancelled

    var errorDescription: String? {
        switch self {
        case .passwordTooShort:
            return "Master password must be at least 8 characters."
        case .noMasterPasswordSet:
            return "No master password has been set up."
        case .keySaltMissing:
            return "Encryption key salt is missing. Please re-enter your master password."
        case .sessionKeyMissing:
            return "Session key missing. Please log in with your master password."
        case .incorrectPassword:
            return "Incorrect master password."
        case .biometricsNotEnabled:
            return "Biometric authentication is not enabled."
        case .biometricsNotAvailable(let reason):
            return "Biometrics unavailable: \(reason)"
        case .biometricsFailed(let reason):
            return "Biometric authentication failed: \(reason)"
        case .biometricsLocked:
            return "Biometrics is locked. Please use your master password."
        case .userCancelled:
            return "Authentication was cancelled."
        }
    }
}
