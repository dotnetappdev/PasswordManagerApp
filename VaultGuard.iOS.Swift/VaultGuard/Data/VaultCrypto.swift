// VaultCrypto.swift — on-device encryption for LOCAL mode.
// PBKDF2-HMAC-SHA256 (600k) via CommonCrypto + AES-256-GCM via CryptoKit.
import Foundation
import CryptoKit
import CommonCrypto

struct VaultCrypto {
    private let iterations: UInt32 = 600_000

    func newSalt() -> Data {
        var bytes = [UInt8](repeating: 0, count: 16)
        _ = SecRandomCopyBytes(kSecRandomDefault, bytes.count, &bytes)
        return Data(bytes)
    }

    func deriveKey(masterPassword: String, salt: Data) -> SymmetricKey {
        var derived = [UInt8](repeating: 0, count: 32)
        let pw = Array(masterPassword.utf8)
        let saltBytes = [UInt8](salt)
        _ = pw.withUnsafeBufferPointer { pwPtr in
            saltBytes.withUnsafeBufferPointer { saltPtr in
                CCKeyDerivationPBKDF(
                    CCPBKDFAlgorithm(kCCPBKDF2),
                    pwPtr.baseAddress, pw.count,
                    saltPtr.baseAddress, saltBytes.count,
                    CCPseudoRandomAlgorithm(kCCPRFHmacAlgSHA256),
                    iterations,
                    &derived, derived.count
                )
            }
        }
        return SymmetricKey(data: Data(derived))
    }

    /// Returns base64(nonce(12) + ciphertext + tag(16)).
    func encrypt(_ plaintext: String, key: SymmetricKey) throws -> String {
        let sealed = try AES.GCM.seal(Data(plaintext.utf8), using: key)
        guard let combined = sealed.combined else { throw CryptoError.encryptFailed }
        return combined.base64EncodedString()
    }

    func decrypt(_ encoded: String, key: SymmetricKey) throws -> String {
        guard let data = Data(base64Encoded: encoded) else { throw CryptoError.badData }
        let box = try AES.GCM.SealedBox(combined: data)
        let opened = try AES.GCM.open(box, using: key)
        guard let s = String(data: opened, encoding: .utf8) else { throw CryptoError.badData }
        return s
    }

    enum CryptoError: Error { case encryptFailed, badData }
}
