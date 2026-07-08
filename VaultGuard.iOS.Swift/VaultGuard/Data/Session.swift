// Session.swift — unlocked-session state. Master password lives in memory only.
import Foundation
import Combine

@MainActor
final class Session: ObservableObject {
    @Published private(set) var user: UserDto?
    @Published private(set) var unlocked = false

    private(set) var masterPassword: String?
    private let keychain: Keychain

    init(keychain: Keychain) { self.keychain = keychain }

    var sessionToken: String? { keychain.get(Keychain.Keys.session) }

    func onLoggedIn(token: String, user: UserDto?, masterPassword: String) {
        keychain.set(token, for: Keychain.Keys.session)
        self.masterPassword = masterPassword
        self.user = user
        unlocked = true
    }

    func onLocalUnlocked(masterPassword: String) {
        self.masterPassword = masterPassword
        unlocked = true
    }

    func lock() {
        masterPassword = nil
        unlocked = false
        keychain.delete(Keychain.Keys.session)
    }
}
