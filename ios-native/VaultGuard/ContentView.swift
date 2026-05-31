import SwiftUI

struct ContentView: View {
    @EnvironmentObject var appState: AppState

    var body: some View {
        Group {
            if appState.isFirstLaunch {
                SetupView()
            } else if !appState.isAuthenticated {
                LoginView()
            } else {
                MainView()
            }
        }
        .animation(.easeInOut(duration: 0.3), value: appState.isAuthenticated)
        .animation(.easeInOut(duration: 0.3), value: appState.isFirstLaunch)
    }
}

#Preview {
    ContentView()
        .environmentObject(AppState())
}
