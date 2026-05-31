import SwiftUI

@main
struct VaultGuardApp: App {
    @StateObject private var appState = AppState()
    @Environment(\.scenePhase) private var scenePhase

    var body: some Scene {
        WindowGroup {
            ContentView()
                .environmentObject(appState)
                .preferredColorScheme(nil)
                .onAppear {
                    setupApp()
                }
        }
        .onChange(of: scenePhase) { _, newPhase in
            handleScenePhaseChange(newPhase)
        }
    }

    private func setupApp() {
        do {
            try DatabaseService.shared.setup()
        } catch {
            print("Database setup failed: \(error)")
        }
    }

    private func handleScenePhaseChange(_ phase: ScenePhase) {
        switch phase {
        case .background:
            // Clear session key when app goes to background
            KeychainService.shared.clearSessionKey()
            appState.lockApp()
        case .active:
            break
        case .inactive:
            break
        @unknown default:
            break
        }
    }
}

// MARK: - App State

@MainActor
class AppState: ObservableObject {
    @Published var isAuthenticated: Bool = false
    @Published var isFirstLaunch: Bool = false
    @Published var isLocked: Bool = true

    init() {
        checkFirstLaunch()
    }

    func checkFirstLaunch() {
        isFirstLaunch = !AuthService.shared.hasMasterPassword()
    }

    func lockApp() {
        isAuthenticated = false
        isLocked = true
    }

    func unlockApp() {
        isAuthenticated = true
        isLocked = false
    }
}
