// Theme.swift — shared colours, tuned to the clean blue of the 1Password mobile apps.
import SwiftUI

enum Theme {
    /// Brand accent (matches WPF/Android).
    static let accent = Color(red: 0x0A / 255, green: 0x84 / 255, blue: 0xFF / 255)

    static func categoryTint(_ hex: String?) -> Color {
        Color(hex: hex ?? "#0A84FF") ?? accent
    }
}

extension Color {
    /// Init from "#RRGGBB" / "#AARRGGBB".
    init?(hex: String) {
        var s = hex.trimmingCharacters(in: .whitespacesAndNewlines)
        if s.hasPrefix("#") { s.removeFirst() }
        guard let value = UInt64(s, radix: 16) else { return nil }
        let r, g, b, a: Double
        switch s.count {
        case 6:
            r = Double((value & 0xFF0000) >> 16) / 255
            g = Double((value & 0x00FF00) >> 8) / 255
            b = Double(value & 0x0000FF) / 255
            a = 1
        case 8:
            a = Double((value & 0xFF000000) >> 24) / 255
            r = Double((value & 0x00FF0000) >> 16) / 255
            g = Double((value & 0x0000FF00) >> 8) / 255
            b = Double(value & 0x000000FF) / 255
        default:
            return nil
        }
        self = Color(.sRGB, red: r, green: g, blue: b, opacity: a)
    }
}
