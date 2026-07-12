// VaultCrypto.swift — on-device encryption for LOCAL mode, byte-for-byte compatible with the desktop
// VaultGuard.Crypto scheme:
//  - Key derivation: PBKDF2-HMAC-SHA256, 600,000 iterations, 32-byte key (CommonCrypto).
//  - Cipher: AES-256-GCM, 12-byte nonce, 16-byte tag, no associated data (CryptoKit).
//  - Container: the three WPF EncryptedPasswordData components joined as
//    base64(nonce):base64(ciphertext):base64(tag) — matching Nonce / EncryptedPassword / AuthenticationTag.
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

    /// Returns base64(nonce):base64(ciphertext):base64(tag), matching WPF's EncryptedPasswordData fields.
    func encrypt(_ plaintext: String, key: SymmetricKey) throws -> String {
        let sealed = try AES.GCM.seal(Data(plaintext.utf8), using: key)
        let nonce = Data(sealed.nonce)
        return "\(nonce.base64EncodedString()):\(sealed.ciphertext.base64EncodedString()):\(sealed.tag.base64EncodedString())"
    }

    func decrypt(_ encoded: String, key: SymmetricKey) throws -> String {
        let parts = encoded.split(separator: ":", omittingEmptySubsequences: false).map(String.init)
        guard parts.count == 3,
              let nonceData = Data(base64Encoded: parts[0]),
              let ct = Data(base64Encoded: parts[1]),
              let tag = Data(base64Encoded: parts[2]) else { throw CryptoError.badData }
        let box = try AES.GCM.SealedBox(nonce: try AES.GCM.Nonce(data: nonceData), ciphertext: ct, tag: tag)
        let opened = try AES.GCM.open(box, using: key)
        guard let s = String(data: opened, encoding: .utf8) else { throw CryptoError.badData }
        return s
    }

    enum CryptoError: Error { case encryptFailed, badData }
}
