// SettingsView.swift — the eight-tab Settings surface mirroring the WPF SettingsPage.
import SwiftUI
import UIKit

struct SettingsView: View {
    @EnvironmentObject var env: AppEnvironment
    @EnvironmentObject var settingsStore: SettingsStore
    @EnvironmentObject var configStore: ConfigStore
    @EnvironmentObject var session: Session
    @EnvironmentObject var passcodeStore: PasscodeStore
    @State private var showSetPasscode = false

    private let tabs = ["Appearance", "Accessibility", "Security", "Storage",
                        "Backup, Import & Export", "Maintenance", "Shortcuts", "About"]
    @State private var tab = 0

    // Storage tab editable state
    @State private var mode: ConnectionMode = .api
    @State private var apiUrl = ""
    @State private var apiKey = ""
    @State private var apiMessage: String?
    @State private var maintMessage: String?
    // Independent, combinable delete options — tick any mix (e.g. seed data + accounts, or just seed data).
    @State private var delSeed = false
    @State private var delItems = false
    @State private var delAccounts = false
    @State private var showDeleteConfirm = false
    @State private var preview = ""

    private var s: Binding<AppSettings> { $settingsStore.settings }

    var body: some View {
        Form {
            Picker("Section", selection: $tab) {
                ForEach(tabs.indices, id: \.self) { Text(tabs[$0]).tag($0) }
            }
            .pickerStyle(.menu)

            switch tab {
            case 0: appearance
            case 1: accessibility
            case 2: security
            case 3: storage
            case 4: backup
            case 5: maintenance
            case 6: shortcuts
            default: about
            }
        }
        .navigationTitle("Settings")
        .navigationBarTitleDisplayMode(.inline)
        .onAppear {
            mode = configStore.config.mode
            apiUrl = configStore.config.apiBaseUrl
            apiKey = configStore.apiKey() ?? ""
        }
        .sheet(isPresented: $showSetPasscode) {
            PasscodeView(
                store: passcodeStore,
                purpose: .create,
                onSuccess: { showSetPasscode = false },
                onCancel: { showSetPasscode = false }
            )
        }
    }

    // MARK: Appearance
    @ViewBuilder private var appearance: some View {
        Section("Theme") {
            Picker("Theme", selection: s.theme) {
                Text("Light").tag(AppTheme.light)
                Text("Dark").tag(AppTheme.dark)
                Text("System").tag(AppTheme.system)
                Text("High Contrast").tag(AppTheme.highContrast)
            }
        }
        Section("Default View") {
            Picker("Default view", selection: s.defaultView) {
                Text("Dashboard").tag(DefaultView.dashboard)
                Text("All Items").tag(DefaultView.allItems)
                Text("Favourites").tag(DefaultView.favorites)
            }
            Toggle("Show item count in sidebar", isOn: s.showItemCount)
            Toggle("Animate list transitions", isOn: s.animateTransitions)
        }
        Section("Password Generator") {
            Stepper("Length: \(settingsStore.settings.pwLength)", value: s.pwLength, in: 8...64)
            Toggle("Uppercase (A-Z)", isOn: s.pwUpper)
            Toggle("Lowercase (a-z)", isOn: s.pwLower)
            Toggle("Digits (0-9)", isOn: s.pwDigits)
            Toggle("Symbols (!@#…)", isOn: s.pwSymbols)
            Toggle("Avoid ambiguous characters", isOn: s.pwAvoidAmbiguous)
            Button("Generate preview") {
                let x = settingsStore.settings
                preview = PasswordGenerator.generate(PasswordOptions(length: x.pwLength, upper: x.pwUpper, lower: x.pwLower, digits: x.pwDigits, symbols: x.pwSymbols, avoidAmbiguous: x.pwAvoidAmbiguous))
            }
            if !preview.isEmpty { Text(preview).font(.system(.body, design: .monospaced)).textSelection(.enabled) }
        }
    }

    // MARK: Accessibility
    @ViewBuilder private var accessibility: some View {
        Section("Display & Zoom") {
            slider("UI zoom", s.uiZoom, 0.8...1.5, "\(Int(settingsStore.settings.uiZoom * 100))%")
        }
        Section("Text & Menu Size") {
            Stepper("Base font size: \(settingsStore.settings.fontSizePt) pt", value: s.fontSizePt, in: 10...24)
        }
        Section("Per-Section Text Size") {
            slider("Menu & navigation", s.scaleMenu, 0.8...1.4, pct(settingsStore.settings.scaleMenu))
            slider("Quick actions", s.scaleQuickActions, 0.8...1.4, pct(settingsStore.settings.scaleQuickActions))
            slider("Item details", s.scaleDetails, 0.8...1.4, pct(settingsStore.settings.scaleDetails))
            slider("Dialogs & popups", s.scaleDialogs, 0.8...1.4, pct(settingsStore.settings.scaleDialogs))
            slider("Global (item cards)", s.scaleGlobal, 0.8...1.4, pct(settingsStore.settings.scaleGlobal))
            slider("Card icons", s.scaleCardIcons, 0.8...1.4, pct(settingsStore.settings.scaleCardIcons))
        }
        Section("Motion & Contrast") {
            Toggle("Reduce motion", isOn: s.reduceMotion)
            Toggle("High contrast", isOn: s.highContrast)
        }
    }

    // MARK: Security
    @ViewBuilder private var security: some View {
        Section("Two-Factor Authentication") {
            Text("Managed on your account. Enter your 2FA code at sign-in when enabled.")
                .font(.footnote).foregroundStyle(.secondary)
        }
        Section("App Lock") {
            Toggle("Require unlock on launch", isOn: s.requirePasscodeOnLaunch)
            Toggle("Biometric unlock (Face ID / Touch ID)", isOn: s.biometricUnlock)
            Stepper(settingsStore.settings.autoLockMinutes == 0 ? "Auto-lock: Never" : "Auto-lock: \(settingsStore.settings.autoLockMinutes) min",
                    value: s.autoLockMinutes, in: 0...60)
        }
        Section("Passcode") {
            Button {
                showSetPasscode = true
            } label: {
                Label(passcodeStore.isSet ? "Change passcode" : "Set a passcode", systemImage: "lock.rectangle")
            }
            if passcodeStore.isSet {
                Button(role: .destructive) { passcodeStore.clear() } label: {
                    Label("Remove passcode", systemImage: "lock.slash")
                }
            }
        }
        Section("Approvals") {
            Toggle("Number-matching approvals", isOn: s.numberMatchApprovals)
            Text("2FA-style number matching on important actions — editing, deleting or saving password items and categories, and changing your master password. Approve by tapping the matching number on this phone. Codes are valid for 60 seconds.")
                .font(.footnote).foregroundStyle(.secondary)
        }
        Section("Autofill") {
            Text("To fill passwords in other apps, enable VaultGuard under Settings › General › AutoFill Passwords.")
                .font(.footnote).foregroundStyle(.secondary)
            if let url = URL(string: UIApplication.openSettingsURLString) {
                Link(destination: url) { Label("Open iOS Settings", systemImage: "key.horizontal") }
            }
        }
        Section("Clipboard & Deletion") {
            Stepper(settingsStore.settings.clipboardClearSeconds == 0 ? "Clear clipboard: Never" : "Clear clipboard: \(settingsStore.settings.clipboardClearSeconds)s",
                    value: s.clipboardClearSeconds, in: 0...120, step: 5)
            Stepper(settingsStore.settings.passwordAutoHideSeconds == 0 ? "Hide revealed password: Off" : "Hide revealed password: \(settingsStore.settings.passwordAutoHideSeconds)s",
                    value: s.passwordAutoHideSeconds, in: 0...120, step: 5)
            Toggle("Confirm before deleting", isOn: s.confirmOnDelete)
        }
        Section {
            Button { session.lock() } label: { Label("Switch account", systemImage: "person.2.circle") }
            Button(role: .destructive) { session.lock() } label: { Label("Lock vault now", systemImage: "lock") }
        }
    }

    // MARK: Storage
    @ViewBuilder private var storage: some View {
        Section("Connection Mode") {
            Picker("Mode", selection: $mode) {
                Text("API Server").tag(ConnectionMode.api)
                Text("Local (SQLite)").tag(ConnectionMode.local)
            }.pickerStyle(.segmented)
        }
        if mode == .api {
            Section("API Configuration") {
                TextField("API URL", text: $apiUrl).textInputAutocapitalization(.never).autocorrectionDisabled().keyboardType(.URL)
                SecureField("API Key", text: $apiKey)
                Button("Test connection") { Task { apiMessage = await env.api.testConnection(baseUrl: apiUrl, apiKey: apiKey) ?? "Connection successful." } }
                Button("Save") { configStore.save(mode: mode, apiBaseUrl: apiUrl, apiKey: apiKey); apiMessage = "Saved." }
                if let apiMessage { Text(apiMessage).font(.footnote).foregroundStyle(Theme.accent) }
            } footer: {
                Text("Generate an API key in the VaultGuard web app: Settings → API Keys.")
            }
        } else {
            Section("Local Database") {
                Text("Your vault is stored only on this device in an encrypted SQLite database.")
                    .font(.footnote).foregroundStyle(.secondary)
                Button("Use local mode") { configStore.save(mode: mode, apiBaseUrl: apiUrl, apiKey: nil); apiMessage = "Saved." }
                if let apiMessage { Text(apiMessage).font(.footnote).foregroundStyle(Theme.accent) }
            }
        }
        Section("Database Provider (Local Mode Only)") {
            Text("Local mode uses an on-device encrypted SQLite database. Server providers (SQL Server, MySQL, PostgreSQL) are selected on the desktop/web app.")
                .font(.footnote).foregroundStyle(.secondary)
        }
    }

    // MARK: Backup / Import / Export
    @ViewBuilder private var backup: some View {
        Section("Import") {
            NavigationLink { ImportView() } label: { Label("Open import", systemImage: "square.and.arrow.down") }
            NavigationLink { OnePasswordImportView() } label: {
                Label("1Password (Connect API)", systemImage: "icloud.and.arrow.down")
            }
        }
        Section("Export") {
            Text("Export is available on the desktop and web apps (Settings → Backup, Import & Export).")
                .font(.footnote).foregroundStyle(.secondary)
        }
        Section("Cloud Backup") {
            Text("Encrypted cloud backups (Google Drive, OneDrive, iCloud) are configured on the desktop/web apps and sync automatically.")
                .font(.footnote).foregroundStyle(.secondary)
        }
    }

    // MARK: Maintenance
    @ViewBuilder private var maintenance: some View {
        Section("Seed Data") {
            Text("Populate the default \"Personal\" vault with sample logins, a card, Wi-Fi and a secure note — in the same categories as the desktop app. Local mode only.")
                .font(.footnote).foregroundStyle(.secondary)
            Button("Seed demo data") { env.repository.seedLocalDemo(); maintMessage = "Demo data seeded into the Personal vault." }
        }
        Section("Delete Data") {
            Text("Choose one or more things to remove on this device, then confirm.")
                .font(.footnote).foregroundStyle(.secondary)
            Toggle("Seed / demo data", isOn: $delSeed)
            Toggle("All my items", isOn: $delItems)
            Toggle("User accounts", isOn: $delAccounts)
            Button(role: .destructive) { showDeleteConfirm = true } label: {
                Label("Delete selected", systemImage: "trash")
            }
            .disabled(!(delSeed || delItems || delAccounts))
            .confirmationDialog("Delete the selected data?",
                                isPresented: $showDeleteConfirm, titleVisibility: .visible) {
                Button("Delete", role: .destructive) { performMaintenanceDelete() }
                Button("Cancel", role: .cancel) {}
            } message: {
                Text("\(deleteSelectionSummary)\n\nThis can’t be undone.")
            }
            if let maintMessage { Text(maintMessage).font(.footnote).foregroundStyle(Theme.accent) }
        }
        Section("Database Management") {
            Text("Schema is kept up to date automatically. In local mode the encrypted SQLite database lives in the app's private storage.")
                .font(.footnote).foregroundStyle(.secondary)
        }
    }

    // MARK: Shortcuts
    @ViewBuilder private var shortcuts: some View {
        Section("Keyboard Shortcuts") {
            Text("Keyboard shortcuts apply to the desktop (WPF) app. On mobile, use search and the navigation sidebar.")
                .font(.footnote).foregroundStyle(.secondary)
        }
        Section {
            shortcut("New item", "⌘N"); shortcut("Search", "⌘F")
            shortcut("Copy password", "⌘C"); shortcut("Lock vault", "⌘L")
        }
    }

    // MARK: About
    @ViewBuilder private var about: some View {
        Section {
            LabeledContent("Version", value: "1.0.0")
            Text("VaultGuard for iOS — a native companion to the VaultGuard desktop and web apps, with feature parity with the WPF app.")
                .font(.footnote).foregroundStyle(.secondary)
        }
    }

    // MARK: Helpers
    private func slider(_ label: String, _ value: Binding<Double>, _ range: ClosedRange<Double>, _ valueLabel: String) -> some View {
        VStack(alignment: .leading) {
            HStack { Text(label); Spacer(); Text(valueLabel).foregroundStyle(Theme.accent) }
            Slider(value: value, in: range)
        }
    }
    private func pct(_ v: Double) -> String { "\(Int(v * 100))%" }
    private func shortcut(_ label: String, _ key: String) -> some View {
        HStack { Text(label); Spacer(); Text(key).font(.system(.body, design: .monospaced)).foregroundStyle(Theme.accent) }
    }

    /// Human-readable list of the ticked delete options for the confirmation dialog.
    private var deleteSelectionSummary: String {
        var lines: [String] = []
        if delSeed { lines.append("• Seed / demo data") }
        if delItems { lines.append("• All your items") }
        if delAccounts { lines.append("• All user accounts and saved keys") }
        return lines.isEmpty ? "Nothing selected." : "This will remove:\n" + lines.joined(separator: "\n")
    }

    /// Apply the ticked options. Where they overlap the most destructive one wins (accounts ⊃ all items ⊃ seed).
    private func performMaintenanceDelete() {
        if delAccounts {
            env.repository.wipeAllLocal(clearAppSecrets: true)
            maintMessage = "All local accounts, their vault items and saved keys were removed."
        } else if delItems {
            env.repository.resetLocal()
            maintMessage = "All items on this device were removed. Your accounts were kept."
        } else if delSeed {
            let n = env.repository.deleteSeedData()
            maintMessage = n > 0
                ? "Removed \(n) demo item(s). Your own items and accounts were kept."
                : "No demo items to remove."
        }
    }
}
