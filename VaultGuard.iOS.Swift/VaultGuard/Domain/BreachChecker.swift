import Foundation
import CryptoKit

/// Privacy-preserving breach check via the Have I Been Pwned "Pwned Passwords" range API with
/// **k-anonymity**: the password is SHA-1 hashed on-device and only the first five hex characters of the
/// hash are ever sent. `Add-Padding` hides whether a match exists. No API key required.
///
/// Returns the number of times the password appears in known breaches, or `nil` when the lookup itself
/// failed (offline/timeout) — callers must treat `nil` as "unknown", never "safe".
enum BreachChecker {
    static func timesSeen(_ password: String) async -> Int? {
        if password.isEmpty { return 0 }

        let hash = sha1Hex(password)
        let prefix = String(hash.prefix(5))
        let suffix = String(hash.dropFirst(5))

        guard let url = URL(string: "https://api.pwnedpasswords.com/range/\(prefix)") else { return nil }
        var request = URLRequest(url: url)
        request.setValue("true", forHTTPHeaderField: "Add-Padding")
        request.setValue("VaultGuard-iOS", forHTTPHeaderField: "User-Agent")
        request.timeoutInterval = 15

        do {
            let (data, response) = try await URLSession.shared.data(for: request)
            guard (response as? HTTPURLResponse)?.statusCode == 200,
                  let body = String(data: data, encoding: .utf8) else { return nil }

            for line in body.split(separator: "\n") {
                let parts = line.split(separator: ":", maxSplits: 1)
                guard parts.count == 2 else { continue }
                if parts[0].trimmingCharacters(in: .whitespaces).caseInsensitiveCompare(suffix) == .orderedSame {
                    return Int(parts[1].trimmingCharacters(in: .whitespacesAndNewlines)) ?? 0
                }
            }
            return 0
        } catch {
            return nil
        }
    }

    private static func sha1Hex(_ input: String) -> String {
        Insecure.SHA1.hash(data: Data(input.utf8))
            .map { String(format: "%02X", $0) }
            .joined()
    }
}
