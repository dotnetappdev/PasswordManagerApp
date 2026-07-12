// PasscodeView.swift — a 6-digit app passcode with a proper confirm step, mirroring the Android flow.
//
//  - `.create` walks the user through Enter → Confirm (the two entries must match) before saving.
//  - `.unlock` verifies the stored passcode.
//
// The passcode is never stored in the clear: it is salted + PBKDF2-hashed with the same VaultCrypto
// used for the local vault, and the "salt:hash" pair lives in the Keychain.
import SwiftUI

@MainActor
final class PasscodeStore: ObservableObject {
    private let keychain: Keychain
    private let crypto: VaultCrypto

    @Published private(set) var isSet: Bool

    init(keychain: Keychain, crypto: VaultCrypto) {
        self.keychain = keychain
        self.crypto = crypto
        self.isSet = keychain.get(Keychain.Keys.passcode) != nil
    }

    private func hash(_ pin: String, salt: Data) -> String {
        crypto.deriveKey(masterPassword: pin, salt: salt)
            .withUnsafeBytes { Data($0) }
            .base64EncodedString()
    }

    func setPasscode(_ pin: String) {
        let salt = crypto.newSalt()
        keychain.set("\(salt.base64EncodedString()):\(hash(pin, salt: salt))", for: Keychain.Keys.passcode)
        isSet = true
    }

    func verify(_ pin: String) -> Bool {
        guard let stored = keychain.get(Keychain.Keys.passcode) else { return false }
        let parts = stored.split(separator: ":", omittingEmptySubsequences: false).map(String.init)
        guard parts.count == 2, let salt = Data(base64Encoded: parts[0]) else { return false }
        // Constant-time-ish compare on the base64 strings.
        let candidate = hash(pin, salt: salt)
        return candidate.count == parts[1].count &&
            zip(candidate.utf8, parts[1].utf8).reduce(UInt8(0)) { $0 | ($1.0 ^ $1.1) } == 0
    }

    func clear() {
        keychain.delete(Keychain.Keys.passcode)
        isSet = false
    }
}

struct PasscodeView: View {
    enum Purpose { case create, unlock }

    @ObservedObject var store: PasscodeStore
    let purpose: Purpose
    var onSuccess: () -> Void
    var onCancel: (() -> Void)? = nil

    @State private var entered = ""
    @State private var firstEntry: String?
    @State private var error: String?

    private let length = 6
    private var confirming: Bool { purpose == .create && firstEntry != nil }

    private var title: String {
        switch purpose {
        case .unlock: return "Enter passcode"
        case .create: return confirming ? "Confirm your passcode" : "Create a passcode"
        }
    }

    private var subtitle: String {
        switch purpose {
        case .unlock: return "Enter your 6-digit passcode to unlock"
        case .create: return confirming
            ? "Re-enter the 6 digits to confirm"
            : "Choose a 6-digit passcode you'll remember"
        }
    }

    var body: some View {
        VStack(spacing: 28) {
            Spacer(minLength: 24)
            Image(systemName: "lock.shield.fill")
                .font(.system(size: 48)).foregroundStyle(Theme.accent)
            VStack(spacing: 6) {
                Text(title).font(.title2.bold())
                Text(subtitle).font(.subheadline).foregroundStyle(.secondary)
                    .multilineTextAlignment(.center)
            }

            // Dots
            HStack(spacing: 18) {
                ForEach(0..<length, id: \.self) { i in
                    Circle()
                        .fill(i < entered.count ? Theme.accent : Color.secondary.opacity(0.25))
                        .frame(width: 16, height: 16)
                }
            }

            if let error { Text(error).font(.footnote).foregroundStyle(.red) }

            keypad
            Spacer()

            if let onCancel {
                Button("Cancel") { onCancel() }.font(.callout)
            }
        }
        .padding(24)
    }

    private var keypad: some View {
        let rows: [[Int]] = [[1, 2, 3], [4, 5, 6], [7, 8, 9]]
        return VStack(spacing: 20) {
            ForEach(rows, id: \.self) { row in
                HStack(spacing: 24) {
                    ForEach(row, id: \.self) { key(String($0)) { append($0) } }
                }
            }
            HStack(spacing: 24) {
                Color.clear.frame(width: 72, height: 72)
                key("0") { append(0) }
                Button(action: backspace) {
                    Image(systemName: "delete.left")
                        .font(.title2)
                        .frame(width: 72, height: 72)
                        .foregroundStyle(.primary)
                }
                .buttonStyle(.plain)
            }
        }
    }

    private func key(_ label: String, action: @escaping () -> Void) -> some View {
        Button(action: action) {
            Text(label)
                .font(.system(size: 30, weight: .regular))
                .frame(width: 72, height: 72)
                .background(Circle().fill(Color.secondary.opacity(0.12)))
                .foregroundStyle(.primary)
        }
        .buttonStyle(.plain)
    }

    private func append(_ digit: Int) {
        guard entered.count < length else { return }
        entered.append(String(digit))
        error = nil
        if entered.count == length { handleComplete() }
    }

    private func backspace() {
        guard !entered.isEmpty else { return }
        entered.removeLast()
    }

    private func handleComplete() {
        switch purpose {
        case .unlock:
            if store.verify(entered) { onSuccess() }
            else { error = "Incorrect passcode."; entered = "" }
        case .create:
            if let first = firstEntry {
                if entered == first {
                    store.setPasscode(entered)
                    onSuccess()
                } else {
                    error = "Those passcodes didn't match. Start again."
                    firstEntry = nil
                    entered = ""
                }
            } else {
                firstEntry = entered
                entered = ""
            }
        }
    }
}
