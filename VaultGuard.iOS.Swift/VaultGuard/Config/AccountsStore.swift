// AccountsStore.swift — saved accounts for the login account-switcher.
import Foundation
import Combine

struct Account: Codable, Identifiable {
    let id: String
    let label: String
    var email: String?
    var mode: ConnectionMode = .api
    var apiBaseUrl: String = ""

    var initials: String {
        let base = label.isEmpty ? (email ?? "?") : label
        return String(base.prefix(1)).uppercased()
    }
}

@MainActor
final class AccountsStore: ObservableObject {
    @Published private(set) var accounts: [Account] = []
    @Published private(set) var currentId: String?

    private let keychain: Keychain
    private let configStore: ConfigStore
    private let defaults = UserDefaults.standard
    private let listKey = "accounts_json"
    private let currentKey = "current_account_id"

    init(keychain: Keychain, configStore: ConfigStore) {
        self.keychain = keychain
        self.configStore = configStore
        load()
    }

    private func load() {
        if let data = defaults.data(forKey: listKey),
           let decoded = try? JSONDecoder().decode([Account].self, from: data) {
            accounts = decoded
        }
        currentId = defaults.string(forKey: currentKey)
    }

    private func persist() {
        if let data = try? JSONEncoder().encode(accounts) { defaults.set(data, forKey: listKey) }
        defaults.set(currentId, forKey: currentKey)
    }

    /// Add or update an account (matched by id), remember its key, and mark it current.
    func upsert(_ account: Account, apiKey: String?) {
        accounts.removeAll { $0.id == account.id }
        accounts.append(account)
        currentId = account.id
        persist()
        if let apiKey { keychain.set(apiKey, for: "acct_key_\(account.id)") }
    }

    /// Make the given account active: apply its connection config + key.
    func select(_ id: String) {
        guard let account = accounts.first(where: { $0.id == id }) else { return }
        currentId = id
        persist()
        configStore.save(
            mode: account.mode,
            apiBaseUrl: account.apiBaseUrl,
            apiKey: account.mode == .api ? keychain.get("acct_key_\(id)") : nil
        )
    }

    func remove(_ id: String) {
        accounts.removeAll { $0.id == id }
        if currentId == id { currentId = nil }
        persist()
        keychain.delete("acct_key_\(id)")
    }
}
