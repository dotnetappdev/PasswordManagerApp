// RootView.swift — top-level routing by configuration + unlock state.
import SwiftUI

struct RootView: View {
    @EnvironmentObject var env: AppEnvironment
    @EnvironmentObject var session: Session
    @EnvironmentObject var configStore: ConfigStore
    @EnvironmentObject var settingsStore: SettingsStore
    @EnvironmentObject var passcodeStore: PasscodeStore

    @State private var passcodeCleared = false

    private var needsPasscode: Bool {
        passcodeStore.isSet && settingsStore.settings.requirePasscodeOnLaunch && !passcodeCleared
    }

    var body: some View {
        Group {
            if !configStore.isConfigured {
                ConnectionSetupView()
            } else if !session.unlocked {
                UnlockView()
            } else if needsPasscode {
                PasscodeView(store: passcodeStore, purpose: .unlock, onSuccess: { passcodeCleared = true })
            } else {
                HomeView()
            }
        }
        // Re-arm the passcode gate whenever the vault re-locks.
        .onChange(of: session.unlocked) { unlocked in if !unlocked { passcodeCleared = false } }
        .animation(.default, value: session.unlocked)
        .animation(.default, value: configStore.isConfigured)
    }
}
