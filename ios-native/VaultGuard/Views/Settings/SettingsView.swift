import SwiftUI
import LocalAuthentication

struct SettingsView: View {
    @Environment(\.dismiss) var dismiss
    @State private var apiBaseURL: String = UserDefaults.standard.string(forKey: "apiBaseURL") ?? ""
    @State private var biometricsEnabled: Bool = UserDefaults.standard.bool(forKey: "biometricsEnabled")
    @State private var autoLockMinutes: Int = UserDefaults.standard.integer(forKey: "autoLockMinutes")
    @State private var showExportAlert = false
    @State private var showLockConfirm = false
    @State private var isSyncing = false
    @State private var syncStatus: String? = nil
    @State private var biometryType: LABiometryType = .none

    private let autoLockOptions = [1, 5, 15, 30, 60, 0]

    var body: some View {
        NavigationView {
            Form {
                // MARK: - Security
                Section {
                    Toggle(isOn: $biometricsEnabled) {
                        Label(biometryLabel, systemImage: biometryIcon)
                    }
                    .tint(Color(hex: "7C3AED"))
                    .onChange(of: biometricsEnabled) { _, enabled in
                        UserDefaults.standard.set(enabled, forKey: "biometricsEnabled")
                    }

                    Picker("Auto-Lock", selection: $autoLockMinutes) {
                        ForEach(autoLockOptions, id: \.self) { mins in
                            Text(autoLockLabel(mins)).tag(mins)
                        }
                    }
                    .onChange(of: autoLockMinutes) { _, val in
                        UserDefaults.standard.set(val, forKey: "autoLockMinutes")
                    }

                    Button(role: .destructive) {
                        showLockConfirm = true
                    } label: {
                        Label("Lock Vault Now", systemImage: "lock.fill")
                            .foregroundColor(.red)
                    }
                } header: {
                    Text("Security")
                } footer: {
                    Text("The vault locks automatically after the chosen idle period.")
                }

                // MARK: - Self-Host / Sync
                Section {
                    HStack {
                        Image(systemName: "server.rack")
                            .foregroundColor(Color(hex: "7C3AED"))
                            .frame(width: 24)
                        TextField("https://myserver.com/api", text: $apiBaseURL)
                            .textContentType(.URL)
                            .keyboardType(.URL)
                            .autocapitalization(.none)
                            .autocorrectionDisabled()
                    }

                    if !apiBaseURL.isEmpty {
                        Button {
                            syncNow()
                        } label: {
                            HStack {
                                if isSyncing {
                                    ProgressView()
                                        .tint(Color(hex: "7C3AED"))
                                        .frame(width: 20, height: 20)
                                } else {
                                    Image(systemName: "arrow.triangle.2.circlepath")
                                        .foregroundColor(Color(hex: "7C3AED"))
                                }
                                Text(isSyncing ? "Syncing…" : "Sync Now")
                                    .foregroundColor(Color(hex: "7C3AED"))
                            }
                        }
                        .disabled(isSyncing)

                        if let status = syncStatus {
                            Text(status)
                                .font(.system(size: 13))
                                .foregroundColor(.secondary)
                        }
                    }
                } header: {
                    Text("Self-Hosted Server")
                } footer: {
                    Text("Optionally sync with your VaultGuard .NET backend. Leave empty to use local SQLite only.")
                }

                // MARK: - Export / Import
                Section("Data") {
                    Button {
                        showExportAlert = true
                    } label: {
                        Label("Export Vault (JSON)", systemImage: "square.and.arrow.up")
                            .foregroundColor(Color(hex: "7C3AED"))
                    }
                }

                // MARK: - About
                Section("About") {
                    LabeledContent("Version") {
                        Text(Bundle.main.infoDictionary?["CFBundleShortVersionString"] as? String ?? "1.0.0")
                            .foregroundColor(.secondary)
                    }
                    LabeledContent("Build") {
                        Text(Bundle.main.infoDictionary?["CFBundleVersion"] as? String ?? "1")
                            .foregroundColor(.secondary)
                    }
                    LabeledContent("Database") {
                        Text("SQLite (GRDB)")
                            .foregroundColor(.secondary)
                    }
                    LabeledContent("Encryption") {
                        Text("AES-256-GCM")
                            .foregroundColor(.secondary)
                    }
                }
            }
            .navigationTitle("Settings")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .confirmationAction) {
                    Button("Done") {
                        saveAndDismiss()
                    }
                    .fontWeight(.semibold)
                    .foregroundColor(Color(hex: "7C3AED"))
                }
            }
            .onAppear { detectBiometry() }
            .alert("Lock Vault", isPresented: $showLockConfirm) {
                Button("Lock", role: .destructive) { lockVault() }
                Button("Cancel", role: .cancel) { }
            } message: {
                Text("You will need to enter your master password to unlock.")
            }
            .alert("Export Vault", isPresented: $showExportAlert) {
                Button("Export") { exportVault() }
                Button("Cancel", role: .cancel) { }
            } message: {
                Text("This will export all items as unencrypted JSON. Only share via secure channels.")
            }
        }
    }

    // MARK: - Helpers

    private var biometryLabel: String {
        switch biometryType {
        case .faceID: return "Use Face ID"
        case .touchID: return "Use Touch ID"
        case .opticID: return "Use Optic ID"
        default: return "Biometric Unlock"
        }
    }

    private var biometryIcon: String {
        switch biometryType {
        case .faceID: return "faceid"
        case .touchID: return "touchid"
        default: return "lock.open.fill"
        }
    }

    private func autoLockLabel(_ minutes: Int) -> String {
        if minutes == 0 { return "Never" }
        if minutes == 1 { return "1 minute" }
        if minutes < 60 { return "\(minutes) minutes" }
        return "1 hour"
    }

    private func detectBiometry() {
        let context = LAContext()
        var error: NSError?
        if context.canEvaluatePolicy(.deviceOwnerAuthenticationWithBiometrics, error: &error) {
            biometryType = context.biometryType
        }
    }

    private func saveAndDismiss() {
        UserDefaults.standard.set(apiBaseURL, forKey: "apiBaseURL")
        SyncService.shared.apiBaseURL = apiBaseURL.isEmpty ? nil : apiBaseURL
        dismiss()
    }

    private func lockVault() {
        AuthService.shared.lock()
        dismiss()
    }

    private func syncNow() {
        guard !apiBaseURL.isEmpty else { return }
        isSyncing = true
        syncStatus = nil
        Task {
            do {
                try await SyncService.shared.syncItems()
                await MainActor.run {
                    isSyncing = false
                    syncStatus = "Synced at \(Date().formatted(date: .omitted, time: .shortened))"
                }
            } catch {
                await MainActor.run {
                    isSyncing = false
                    syncStatus = "Sync failed: \(error.localizedDescription)"
                }
            }
        }
    }

    private func exportVault() {
        let items = (try? PasswordItemService.shared.fetchAll()) ?? []
        guard let data = try? JSONEncoder().encode(items),
              let json = String(data: data, encoding: .utf8) else { return }

        let url = FileManager.default.temporaryDirectory.appendingPathComponent("vaultguard-export.json")
        try? json.write(to: url, atomically: true, encoding: .utf8)

        let av = UIActivityViewController(activityItems: [url], applicationActivities: nil)
        if let scene = UIApplication.shared.connectedScenes.first as? UIWindowScene,
           let vc = scene.windows.first?.rootViewController {
            vc.present(av, animated: true)
        }
    }
}
