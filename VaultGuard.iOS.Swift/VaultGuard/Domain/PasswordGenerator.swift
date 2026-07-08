// PasswordGenerator.swift — mirrors the WPF/Blazor generator.
import Foundation

struct PasswordOptions {
    var length = 20
    var upper = true
    var lower = true
    var digits = true
    var symbols = true
    var avoidAmbiguous = false
}

enum PasswordGenerator {
    static func generate(_ options: PasswordOptions) -> String {
        let ambiguous = Set("O0oIl1|`'\"")
        func filter(_ s: String) -> [Character] {
            options.avoidAmbiguous ? s.filter { !ambiguous.contains($0) } : Array(s)
        }
        var pools: [[Character]] = []
        if options.upper { pools.append(filter("ABCDEFGHIJKLMNOPQRSTUVWXYZ")) }
        if options.lower { pools.append(filter("abcdefghijklmnopqrstuvwxyz")) }
        if options.digits { pools.append(filter("0123456789")) }
        if options.symbols { pools.append(filter("!@#$%^&*()-_=+[]{};:,.?/")) }
        pools = pools.filter { !$0.isEmpty }
        guard !pools.isEmpty else { return "" }

        let length = min(max(options.length, 4), 128)
        let all = pools.flatMap { $0 }
        var chars: [Character] = pools.map { $0.randomElement()! }
        while chars.count < length { chars.append(all.randomElement()!) }
        return String(chars.shuffled())
    }

    /// 0..4 strength estimate for a live meter.
    static func strength(_ password: String) -> Int {
        if password.isEmpty { return 0 }
        var score = 0
        if password.count >= 12 { score += 1 }
        if password.count >= 16 { score += 1 }
        if password.contains(where: \.isNumber) && password.contains(where: \.isLetter) { score += 1 }
        if password.contains(where: { !$0.isLetter && !$0.isNumber }) { score += 1 }
        return min(max(score, 0), 4)
    }
}
