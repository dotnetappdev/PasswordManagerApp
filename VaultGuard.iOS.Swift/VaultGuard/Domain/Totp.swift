// Totp.swift — RFC 6238 TOTP (SHA-1, 6 digits, 30s), matching VaultGuard authenticator codes.
import Foundation
import CryptoKit

enum Totp {
    struct Code { let value: String; let secondsRemaining: Int }

    static func generate(base32Secret: String, date: Date = Date(), period: Int = 30, digits: Int = 6) -> Code? {
        guard let key = base32Decode(base32Secret.replacingOccurrences(of: " ", with: "")) else { return nil }
        let counter = UInt64(date.timeIntervalSince1970) / UInt64(period)
        var bigEndian = counter.bigEndian
        let counterData = Data(bytes: &bigEndian, count: 8)

        let mac = HMAC<Insecure.SHA1>.authenticationCode(for: counterData, using: SymmetricKey(data: key))
        let hash = Data(mac)
        let offset = Int(hash[hash.count - 1] & 0x0f)
        let binary = (UInt32(hash[offset] & 0x7f) << 24)
            | (UInt32(hash[offset + 1]) << 16)
            | (UInt32(hash[offset + 2]) << 8)
            | UInt32(hash[offset + 3])
        let otp = binary % UInt32(pow(10.0, Double(digits)))
        let code = String(format: "%0\(digits)d", otp)
        let remaining = period - Int(UInt64(date.timeIntervalSince1970) % UInt64(period))
        return Code(value: code, secondsRemaining: remaining)
    }

    /// Extract the secret from an `otpauth://` URI, or return the raw string.
    static func secretFromUri(_ scanned: String) -> String {
        guard scanned.lowercased().hasPrefix("otpauth://"),
              let comps = URLComponents(string: scanned),
              let secret = comps.queryItems?.first(where: { $0.name.lowercased() == "secret" })?.value
        else { return scanned }
        return secret
    }

    private static func base32Decode(_ input: String) -> Data? {
        let alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567"
        let clean = input.uppercased().replacingOccurrences(of: "=", with: "")
        if clean.isEmpty { return nil }
        var bits = 0, value = 0
        var out = [UInt8]()
        for ch in clean {
            guard let idx = alphabet.firstIndex(of: ch) else { return nil }
            value = (value << 5) | alphabet.distance(from: alphabet.startIndex, to: idx)
            bits += 5
            if bits >= 8 { bits -= 8; out.append(UInt8((value >> bits) & 0xff)) }
        }
        return Data(out)
    }
}
