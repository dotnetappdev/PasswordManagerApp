// ConfigStore.swift — connection mode + API URL (UserDefaults) and API key (Keychain).
import Foundation
import Combine

enum ConnectionMode: String, Codable, CaseIterable {
    case api = "API"
    case local = "LOCAL"
}

struct ConnectionConfig {
    var mode: ConnectionMode = .api
    var apiBaseUrl: String = ""
    var hasApiKey: Bool = false

    var isApiConfigured: Bool { mode == .api && !apiBaseUrl.isEmpty && hasApiKey }

    var normalizedBaseUrl: String {
        guard !apiBaseUrl.isEmpty else { return apiBaseUrl }
        return apiBaseUrl.hasSuffix("/") ? apiBaseUrl : apiBaseUrl + "/"
    }
}

@MainActor
final class ConfigStore: ObservableObject {
    @Published private(set) var config = ConnectionConfig()

    private let keychain: Keychain
    private let defaults = UserDefaults.standard
    private enum K {
        static let mode = "cfg_mode"
        static let url = "cfg_api_url"
    }

    init(keychain: Keychain) {
        self.keychain = keychain
        reload()
    }

    var isConfigured: Bool {
        switch config.mode {
        case .api: return config.isApiConfigured
        case .local: return true
        }
    }

    func apiKey() -> String? { keychain.get(Keychain.Keys.apiKey) }

    func reload() {
        let mode = ConnectionMode(rawValue: defaults.string(forKey: K.mode) ?? "") ?? .api
        config = ConnectionConfig(
            mode: mode,
            apiBaseUrl: defaults.string(forKey: K.url) ?? "",
            hasApiKey: !(keychain.get(Keychain.Keys.apiKey) ?? "").isEmpty
        )
    }

    func save(mode: ConnectionMode, apiBaseUrl: String, apiKey: String?) {
        defaults.set(mode.rawValue, forKey: K.mode)
        defaults.set(apiBaseUrl.trimmingCharacters(in: .whitespaces), forKey: K.url)
        if let apiKey { keychain.set(apiKey.trimmingCharacters(in: .whitespaces), for: Keychain.Keys.apiKey) }
        reload()
    }
}
