// VaultGuardApp.swift — app entry point.
import SwiftUI

@main
struct VaultGuardApp: App {
    @StateObject private var environment = AppEnvironment()

    var body: some Scene {
        WindowGroup {
            RootView()
                .environmentObject(environment)
                .environmentObject(environment.session)
                .environmentObject(environment.settingsStore)
                .environmentObject(environment.configStore)
                .tint(Theme.accent)
                .preferredColorScheme(environment.settingsStore.settings.colorScheme)
        }
    }
}

/// Composition root: owns the shared stores and repository (a lightweight DI container).
@MainActor
final class AppEnvironment: ObservableObject {
    let keychain = Keychain()
    let configStore: ConfigStore
    let settingsStore = SettingsStore()
    let session: Session
    let api: ApiClient
    let localStore: LocalStore
    let crypto = VaultCrypto()
    let repository: VaultRepository

    init() {
        let kc = keychain
        configStore = ConfigStore(keychain: kc)
        session = Session(keychain: kc)
        api = ApiClient(configStore: configStore, keychain: kc, session: session)
        localStore = LocalStore()
        repository = VaultRepository(
            api: api, local: localStore, crypto: crypto,
            session: session, keychain: kc, configStore: configStore
        )
    }
}
