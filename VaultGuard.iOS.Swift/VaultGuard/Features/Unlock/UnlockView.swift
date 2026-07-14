// UnlockView.swift — sign in (API mode) or unlock the local vault (local mode).
import SwiftUI

@MainActor
struct UnlockView: View {
    @EnvironmentObject var env: AppEnvironment
    @EnvironmentObject var configStore: ConfigStore
    @EnvironmentObject var accountsStore: AccountsStore

    @State private var email = ""
    @State private var password = ""
    @State private var twoFactor = ""
    @State private var needsTwoFactor = false
    @State private var loading = false
    @State private var error: String?
    @State private var showSetup = false
    @State private var hasQuickUnlock = false
    @State private var biometricBusy = false

    private var isLocal: Bool { configStore.config.mode == .local }

    var body: some View {
        VStack(spacing: 20) {
            Spacer()
            Image(systemName: "lock.shield.fill")
                .font(.system(size: 56)).foregroundStyle(Theme.accent)
            Text("VaultGuard").font(.largeTitle.bold())
            Text(isLocal ? "Unlock your local vault" : "Sign in to your vault")
                .foregroundStyle(.secondary)

            if !accountsStore.accounts.isEmpty {
                accountSwitcher
            }

            VStack(spacing: 12) {
                if !isLocal {
                    TextField("Email", text: $email)
                        .textInputAutocapitalization(.never).autocorrectionDisabled()
                        .keyboardType(.emailAddress).textFieldStyle(.roundedBorder)
                }
                // The master key is the primary field — give it a large, comfortable target on mobile.
                HStack(spacing: 12) {
                    Image(systemName: "lock.fill").foregroundStyle(Theme.accent)
                    SecureField("Master password", text: $password)
                        .font(.title3)
                        .textInputAutocapitalization(.never)
                        .autocorrectionDisabled()
                }
                .padding(.horizontal, 16)
                .padding(.vertical, 18)
                .background(RoundedRectangle(cornerRadius: 16).fill(Color.secondary.opacity(0.12)))
                .overlay(RoundedRectangle(cornerRadius: 16).stroke(Color.secondary.opacity(0.25), lineWidth: 1))
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

            if isLocal && hasQuickUnlock {
                Button(action: { Task { await unlockWithBiometrics() } }) {
                    HStack {
                        if biometricBusy { ProgressView() } else { Image(systemName: "faceid") }
                        Text("Unlock with \(BiometricAuth.displayName)")
                    }.frame(maxWidth: .infinity).padding(.vertical, 6)
                }
                .buttonStyle(.bordered)
                .disabled(loading || biometricBusy)
                .padding(.horizontal)
            }

            Button("Connection settings") { showSetup = true }
                .font(.footnote)
            Spacer()
        }
        .padding()
        .sheet(isPresented: $showSetup) { ConnectionSetupView() }
        .onAppear { hasQuickUnlock = isLocal && env.repository.hasQuickUnlock }
    }

    private func unlockWithBiometrics() async {
        biometricBusy = true; error = nil
        let result = await env.repository.unlockWithBiometrics()
        biometricBusy = false
        switch result {
        case .success: break
        case .needsTwoFactor: break // Local mode never needs this branch.
        case .error(let m):
            error = m
            hasQuickUnlock = env.repository.hasQuickUnlock // reflect if a stale item was just cleared
        }
    }

    private var accountSwitcher: some View {
        ScrollView(.horizontal, showsIndicators: false) {
            HStack(spacing: 18) {
                ForEach(accountsStore.accounts) { account in
                    let selected = account.id == accountsStore.currentId
                    Button {
                        accountsStore.select(account.id)
                        email = account.email ?? ""
                    } label: {
                        VStack(spacing: 6) {
                            ZStack {
                                Circle().fill(selected ? Theme.accent : Color.secondary.opacity(0.2))
                                    .frame(width: 52, height: 52)
                                Text(account.initials).font(.title3.bold())
                                    .foregroundStyle(selected ? .white : .primary)
                            }
                            Text(account.email ?? account.label)
                                .font(.caption2).lineLimit(1)
                                .foregroundStyle(selected ? Theme.accent : .secondary)
                                .frame(maxWidth: 64)
                        }
                    }
                    .buttonStyle(.plain)
                }
                Button { showSetup = true } label: {
                    VStack(spacing: 6) {
                        ZStack {
                            Circle().fill(Color.secondary.opacity(0.15)).frame(width: 52, height: 52)
                            Image(systemName: "plus").foregroundStyle(.secondary)
                        }
                        Text("Add").font(.caption2).foregroundStyle(.secondary)
                    }
                }
                .buttonStyle(.plain)
            }
            .padding(.horizontal)
        }
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
