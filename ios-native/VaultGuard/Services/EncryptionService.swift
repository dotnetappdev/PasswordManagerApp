import Foundation
import CryptoKit

class EncryptionService {
    static let shared = EncryptionService()
    private init() {}

    // MARK: - Session Key

    private var sessionKey: SymmetricKey? {
        guard let keyData = KeychainService.shared.getSessionKey() else { return nil }
        return SymmetricKey(data: keyData)
    }

    func setSessionKey(_ key: SymmetricKey) throws {
        let keyData = key.withUnsafeBytes { Data($0) }
        try KeychainService.shared.storeSessionKey(keyData)
    }

    // MARK: - Key Derivation

    /// Derives a SymmetricKey from a password using PBKDF2-SHA256
    func deriveKey(from password: String, salt: Data, iterations: Int = 310_000) throws -> SymmetricKey {
        guard let passwordData = password.data(using: .utf8) else {
            throw EncryptionError.invalidPassword
        }

        var derivedKey = Data(repeating: 0, count: 32) // 256 bits

        let result = derivedKey.withUnsafeMutableBytes { derivedKeyBytes in
            passwordData.withUnsafeBytes { passwordBytes in
                salt.withUnsafeBytes { saltBytes in
                    CCKeyDerivationPBKDF(
                        CCPBKDFAlgorithm(kCCPBKDF2),
                        passwordBytes.baseAddress, passwordBytes.count,
                        saltBytes.baseAddress, saltBytes.count,
                        CCPseudoRandomAlgorithm(kCCPRFHmacAlgSHA256),
                        UInt32(iterations),
                        derivedKeyBytes.baseAddress, derivedKeyBytes.count
                    )
                }
            }
        }

        guard result == kCCSuccess else {
            throw EncryptionError.keyDerivationFailed
        }

        return SymmetricKey(data: derivedKey)
    }

    /// Derives a hash for master password verification
    func derivePasswordHash(password: String, salt: Data) throws -> Data {
        let key = try deriveKey(from: password, salt: salt, iterations: 310_000)
        return key.withUnsafeBytes { Data($0) }
    }

    // MARK: - Random Salt / Nonce Generation

    func generateSalt(length: Int = 32) -> Data {
        var salt = Data(count: length)
        _ = salt.withUnsafeMutableBytes { SecRandomCopyBytes(kSecRandomDefault, length, $0.baseAddress!) }
        return salt
    }

    // MARK: - AES-256-GCM Encrypt

    func encrypt(_ plaintext: String) throws -> String {
        guard let key = sessionKey else {
            throw EncryptionError.noSessionKey
        }

        guard let data = plaintext.data(using: .utf8) else {
            throw EncryptionError.invalidInput
        }

        let sealedBox = try AES.GCM.seal(data, using: key)

        guard let combined = sealedBox.combined else {
            throw EncryptionError.encryptionFailed
        }

        return combined.base64EncodedString()
    }

    func encryptData(_ data: Data) throws -> String {
        guard let key = sessionKey else {
            throw EncryptionError.noSessionKey
        }

        let sealedBox = try AES.GCM.seal(data, using: key)

        guard let combined = sealedBox.combined else {
            throw EncryptionError.encryptionFailed
        }

        return combined.base64EncodedString()
    }

    // MARK: - AES-256-GCM Decrypt

    func decrypt(_ ciphertext: String) throws -> String {
        guard let key = sessionKey else {
            throw EncryptionError.noSessionKey
        }

        guard let combined = Data(base64Encoded: ciphertext) else {
            throw EncryptionError.invalidCiphertext
        }

        let sealedBox = try AES.GCM.SealedBox(combined: combined)
        let decryptedData = try AES.GCM.open(sealedBox, using: key)

        guard let plaintext = String(data: decryptedData, encoding: .utf8) else {
            throw EncryptionError.decryptionFailed
        }

        return plaintext
    }

    func decryptToData(_ ciphertext: String) throws -> Data {
        guard let key = sessionKey else {
            throw EncryptionError.noSessionKey
        }

        guard let combined = Data(base64Encoded: ciphertext) else {
            throw EncryptionError.invalidCiphertext
        }

        let sealedBox = try AES.GCM.SealedBox(combined: combined)
        return try AES.GCM.open(sealedBox, using: key)
    }

    // MARK: - Password Strength

    func passwordStrength(_ password: String) -> PasswordStrength {
        var score = 0

        if password.count >= 8 { score += 1 }
        if password.count >= 12 { score += 1 }
        if password.count >= 16 { score += 1 }

        let hasLowercase = password.range(of: "[a-z]", options: .regularExpression) != nil
        let hasUppercase = password.range(of: "[A-Z]", options: .regularExpression) != nil
        let hasDigit = password.range(of: "[0-9]", options: .regularExpression) != nil
        let hasSpecial = password.range(of: "[^a-zA-Z0-9]", options: .regularExpression) != nil

        if hasLowercase { score += 1 }
        if hasUppercase { score += 1 }
        if hasDigit { score += 1 }
        if hasSpecial { score += 2 }

        switch score {
        case 0...2: return .weak
        case 3...4: return .fair
        case 5...6: return .good
        default: return .strong
        }
    }

    // MARK: - Password Generation

    func generatePassword(length: Int = 20, includeSymbols: Bool = true, includeNumbers: Bool = true) -> String {
        var charset = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"
        if includeNumbers { charset += "0123456789" }
        if includeSymbols { charset += "!@#$%^&*()-_=+[]{}|;:,.<>?" }

        let charArray = Array(charset)
        var password = ""

        for _ in 0..<length {
            var randomIndex: UInt32 = 0
            _ = SecRandomCopyBytes(kSecRandomDefault, 4, &randomIndex)
            password.append(charArray[Int(randomIndex) % charArray.count])
        }

        return password
    }
}

// MARK: - Password Strength

enum PasswordStrength: Int {
    case weak = 0
    case fair = 1
    case good = 2
    case strong = 3

    var label: String {
        switch self {
        case .weak: return "Weak"
        case .fair: return "Fair"
        case .good: return "Good"
        case .strong: return "Strong"
        }
    }

    var color: SwiftUI.Color {
        switch self {
        case .weak: return .red
        case .fair: return .orange
        case .good: return .yellow
        case .strong: return .green
        }
    }

    var fraction: Double {
        switch self {
        case .weak: return 0.25
        case .fair: return 0.5
        case .good: return 0.75
        case .strong: return 1.0
        }
    }
}

// MARK: - Errors

enum EncryptionError: LocalizedError {
    case noSessionKey
    case invalidPassword
    case invalidInput
    case invalidCiphertext
    case encryptionFailed
    case decryptionFailed
    case keyDerivationFailed

    var errorDescription: String? {
        switch self {
        case .noSessionKey: return "No encryption key loaded. Please authenticate first."
        case .invalidPassword: return "Invalid password encoding."
        case .invalidInput: return "Invalid input data."
        case .invalidCiphertext: return "Invalid ciphertext format."
        case .encryptionFailed: return "Encryption failed."
        case .decryptionFailed: return "Decryption failed."
        case .keyDerivationFailed: return "Key derivation failed."
        }
    }
}

// MARK: - CommonCrypto Bridge

import CommonCrypto
