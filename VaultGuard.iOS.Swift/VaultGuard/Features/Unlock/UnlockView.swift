// UnlockView.swift — sign in (API mode) or unlock the local vault (local mode).
import SwiftUI

@MainActor
struct UnlockView: View {
    @EnvironmentObject var env: AppEnvironment
    @EnvironmentObject var configStore: ConfigStore

    @State private var email = ""
    @State private var password = ""
    @State private var twoFactor = ""
    @State private var needsTwoFactor = false
    @State private var loading = false
    @State private var error: String?
    @State private var showSetup = false

    private var isLocal: Bool { configStore.config.mode == .local }

    var body: some View {
        VStack(spacing: 20) {
            Spacer()
            Image(systemName: "lock.shield.fill")
                .font(.system(size: 56)).foregroundStyle(Theme.accent)
            Text("VaultGuard").font(.largeTitle.bold())
            Text(isLocal ? "Unlock your local vault" : "Sign in to your vault")
                .foregroundStyle(.secondary)

            VStack(spacing: 12) {
                if !isLocal {
                    TextField("Email", text: $email)
                        .textInputAutocapitalization(.never).autocorrectionDisabled()
                        .keyboardType(.emailAddress).textFieldStyle(.roundedBorder)
                }
                SecureField("Master password", text: $password).textFieldStyle(.roundedBorder)
                if needsTwoFactor {
                    TextField("2FA code", text: $twoFactor)
                        .keyboardType(.numberPad).textFieldStyle(.roundedBorder)
                }
            }
            .padding(.horizontal)

            if let error { Text(error).foregroundStyle(.red).font(.footnote) }

            Button(action: { Task { await submit() } }) {
                HStack {
                    if loading { ProgressView() }
                    Text(isLocal ? "Unlock" : "Sign in")
                }.frame(maxWidth: .infinity).padding(.vertical, 6)
            }
            .buttonStyle(.borderedProminent)
            .disabled(loading)
            .padding(.horizontal)

            Button("Connection settings") { showSetup = true }
                .font(.footnote)
            Spacer()
        }
        .padding()
        .sheet(isPresented: $showSetup) { ConnectionSetupView() }
    }

    private func submit() async {
        guard !password.isEmpty else { error = "Enter your master password."; return }
        loading = true; error = nil
        let result: LoginResult = isLocal
            ? env.repository.unlockLocal(masterPassword: password)
            : await env.repository.login(email: email, password: password, twoFactorCode: twoFactor)
        loading = false
        switch result {
        case .success: break
        case .needsTwoFactor: needsTwoFactor = true; error = "Enter your 2FA code."
        case .error(let m): error = m
        }
    }
}
