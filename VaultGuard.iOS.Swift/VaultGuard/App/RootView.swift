// RootView.swift — top-level routing by configuration + unlock state.
import SwiftUI

struct RootView: View {
    @EnvironmentObject var env: AppEnvironment
    @EnvironmentObject var session: Session
    @EnvironmentObject var configStore: ConfigStore

    var body: some View {
        Group {
            if !configStore.isConfigured {
                ConnectionSetupView()
            } else if !session.unlocked {
                UnlockView()
            } else {
                HomeView()
            }
        }
        .animation(.default, value: session.unlocked)
        .animation(.default, value: configStore.isConfigured)
    }
}
