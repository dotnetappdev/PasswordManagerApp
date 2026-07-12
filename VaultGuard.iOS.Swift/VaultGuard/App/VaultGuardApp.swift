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
                .environmentObject(environment.accountsStore)
                .environmentObject(environment.passcodeStore)
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
    let accountsStore: AccountsStore
    let session: Session
    let api: ApiClient
    let localStore: LocalStore
    let crypto = VaultCrypto()
    let repository: VaultRepository
    let passcodeStore: PasscodeStore

    init() {
        let kc = keychain
        let cfg = ConfigStore(keychain: kc)
        configStore = cfg
        accountsStore = AccountsStore(keychain: kc, configStore: cfg)
        session = Session(keychain: kc)
        passcodeStore = PasscodeStore(keychain: kc, crypto: crypto)
        api = ApiClient(configStore: cfg, keychain: kc, session: session)
        localStore = LocalStore()
        repository = VaultRepository(
            api: api, local: localStore, crypto: crypto,
            session: session, keychain: kc, configStore: cfg, accountsStore: accountsStore
        )
    }
}
