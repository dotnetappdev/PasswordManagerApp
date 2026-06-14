import SwiftUI

// MARK: - Field Row (plain text)

struct FieldRowView: View {
    let label: String
    let value: String
    var isURL: Bool = false
    var isCopyable: Bool = true
    var onCopy: (() -> Void)? = nil

    var body: some View {
        HStack(alignment: .top, spacing: 12) {
            VStack(alignment: .leading, spacing: 5) {
                Text(label)
                    .font(.system(size: 11, weight: .semibold))
                    .foregroundColor(Color(hex: "7C3AED"))
                    .tracking(0.5)

                if isURL, let url = URL(string: value.hasPrefix("http") ? value : "https://\(value)") {
                    Link(value, destination: url)
                        .font(.system(size: 15))
                        .foregroundColor(Color(hex: "7C3AED"))
                        .lineLimit(2)
                } else {
                    Text(value)
                        .font(.system(size: 15))
                        .foregroundColor(.primary)
                        .textSelection(.enabled)
                        .lineLimit(3)
                }
            }

            Spacer()

            if isCopyable {
                Button {
                    UIPasteboard.general.string = value
                    onCopy?()
                } label: {
                    Image(systemName: "doc.on.doc")
                        .font(.system(size: 15))
                        .foregroundColor(Color(hex: "7C3AED").opacity(0.7))
                }
                .buttonStyle(.plain)
                .padding(.top, 16)
            }
        }
        .padding(.horizontal, 20)
        .padding(.vertical, 14)
    }
}

// MARK: - Password Field Row (masked + reveal toggle)

struct PasswordFieldRowView: View {
    let label: String
    let value: String
    var onCopy: (() -> Void)? = nil

    @State private var isRevealed = false

    var body: some View {
        HStack(alignment: .top, spacing: 12) {
            VStack(alignment: .leading, spacing: 5) {
                Text(label)
                    .font(.system(size: 11, weight: .semibold))
                    .foregroundColor(Color(hex: "7C3AED"))
                    .tracking(0.5)

                if isRevealed {
                    Text(value)
                        .font(.system(size: 15, design: .monospaced))
                        .foregroundColor(.primary)
                        .textSelection(.enabled)
                        .lineLimit(2)
                } else {
                    Text(String(repeating: "•", count: min(value.count, 16)))
                        .font(.system(size: 18))
                        .foregroundColor(.secondary)
                }
            }

            Spacer()

            HStack(spacing: 16) {
                Button {
                    isRevealed.toggle()
                } label: {
                    Image(systemName: isRevealed ? "eye.slash" : "eye")
                        .font(.system(size: 15))
                        .foregroundColor(Color(hex: "7C3AED").opacity(0.7))
                }
                .buttonStyle(.plain)

                Button {
                    UIPasteboard.general.string = value
                    onCopy?()
                } label: {
                    Image(systemName: "doc.on.doc")
                        .font(.system(size: 15))
                        .foregroundColor(Color(hex: "7C3AED").opacity(0.7))
                }
                .buttonStyle(.plain)
            }
            .padding(.top, 16)
        }
        .padding(.horizontal, 20)
        .padding(.vertical, 14)
    }
}

// MARK: - Password Strength

extension EncryptionService {
    enum PasswordStrength: Int {
        case veryWeak = 0, weak = 1, fair = 2, strong = 3, veryStrong = 4

        var label: String {
            switch self {
            case .veryWeak: return "Very Weak"
            case .weak: return "Weak"
            case .fair: return "Fair"
            case .strong: return "Strong"
            case .veryStrong: return "Very Strong"
            }
        }

        var color: Color {
            switch self {
            case .veryWeak: return .red
            case .weak: return .orange
            case .fair: return .yellow
            case .strong: return Color(hex: "10B981")
            case .veryStrong: return Color(hex: "7C3AED")
            }
        }
    }

    func passwordStrength(_ password: String) -> PasswordStrength {
        var score = 0
        if password.count >= 8 { score += 1 }
        if password.count >= 14 { score += 1 }
        if password.rangeOfCharacter(from: .decimalDigits) != nil { score += 1 }
        if password.rangeOfCharacter(from: CharacterSet(charactersIn: "!@#$%^&*()_+-=[]{}|;':\",./<>?")) != nil { score += 1 }
        return PasswordStrength(rawValue: min(score, 4)) ?? .veryWeak
    }

    func generatePassword(length: Int, includeSymbols: Bool, includeNumbers: Bool) -> String {
        var chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"
        if includeNumbers { chars += "0123456789" }
        if includeSymbols { chars += "!@#$%^&*()-_=+[]{}|;:,.<>?" }
        return String((0..<length).map { _ in chars.randomElement()! })
    }
}
