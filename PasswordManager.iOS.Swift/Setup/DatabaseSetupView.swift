// DatabaseSetupView.swift
// PasswordManager – iOS Native Setup Wizard
// Target: iOS 16+  |  SwiftUI
//
// This file is the iOS-native equivalent of the multi-provider database
// setup wizard available on other platforms (C# / Uno / WinUI / WPF).
// It does NOT share code with the C# projects; all DB interaction is
// stubbed out with a TODO for a native HTTP call to the PasswordManager API.

import SwiftUI

// ---------------------------------------------------------------------------
// MARK: - Enumerations
// ---------------------------------------------------------------------------

enum DBProvider: String, CaseIterable, Identifiable {
    case sqlite      = "SQLite (Local)"
    case sqlServer   = "SQL Server"
    case mysql       = "MySQL"
    case postgresql  = "PostgreSQL"

    var id: String { rawValue }

    var systemImage: String {
        switch self {
        case .sqlite:     return "internaldrive"
        case .sqlServer:  return "server.rack"
        case .mysql:      return "cylinder.split.1x2"
        case .postgresql: return "cylinder"
        }
    }

    var isRecommended: Bool { self == .sqlite }
}

enum SQLServerAuthMode: String, CaseIterable {
    case windows   = "Windows Authentication"
    case sqlServer = "SQL Server Authentication"
}

enum SQLServerEncryption: String, CaseIterable {
    case none      = "None"
    case optional  = "Optional"
    case mandatory = "Mandatory"
    case strict    = "Strict (TLS 1.3)"
}

enum SQLServerNetworkProtocol: String, CaseIterable {
    case `default`    = "Default"
    case tcpIP        = "TCP/IP"
    case namedPipes   = "Named Pipes"
    case sharedMemory = "Shared Memory"
}

enum SQLServerAppIntent: String, CaseIterable {
    case readWrite = "ReadWrite"
    case readOnly  = "ReadOnly"
}

enum MySQLSSLMode: String, CaseIterable {
    case none       = "None"
    case preferred  = "Preferred"
    case required   = "Required"
    case verifyCA   = "VerifyCA"
    case verifyFull = "VerifyFull"
}

enum PostgreSQLSSLMode: String, CaseIterable {
    case disable    = "Disable"
    case allow      = "Allow"
    case prefer     = "Prefer"
    case require    = "Require"
    case verifyCA   = "VerifyCA"
    case verifyFull = "VerifyFull"
}

// ---------------------------------------------------------------------------
// MARK: - Accent colour
// ---------------------------------------------------------------------------

extension Color {
    /// Matches the app-wide #007ACC blue used across other platforms.
    static let pmAccent = Color(red: 0, green: 0.478, blue: 0.8)
}

// ---------------------------------------------------------------------------
// MARK: - ViewModel
// ---------------------------------------------------------------------------

@MainActor
final class DatabaseSetupViewModel: ObservableObject {

    // MARK: Step control
    @Published var currentStep: Int = 1

    // MARK: Provider
    @Published var selectedProvider: DBProvider = .sqlite

    // MARK: Sub-tab index (0 = General, 1 = Security, 2 = Advanced)
    @Published var configTab: Int = 0

    // MARK: Test / Save state
    @Published var isTesting: Bool = false
    @Published var testResult: (success: Bool, message: String)? = nil
    @Published var isSaving: Bool = false
    @Published var saveError: String? = nil

    // MARK: SQLite
    @Published var sqliteFilePath: String = ""

    // MARK: SQL Server – General
    @Published var ssServer: String = ""
    @Published var ssInstance: String = ""
    @Published var ssPort: String = "1433"
    @Published var ssDatabase: String = ""
    @Published var ssAuthMode: SQLServerAuthMode = .sqlServer
    @Published var ssUsername: String = ""
    @Published var ssPassword: String = ""

    // MARK: SQL Server – Security
    @Published var ssEncryption: SQLServerEncryption = .optional
    @Published var ssTrustCert: Bool = false
    @Published var ssServerCert: String = ""

    // MARK: SQL Server – Advanced
    @Published var ssNetworkProtocol: SQLServerNetworkProtocol = .default
    @Published var ssPacketSize: String = "4096"
    @Published var ssConnTimeout: String = "15"
    @Published var ssCmdTimeout: String = "30"
    @Published var ssAppName: String = "PasswordManager"
    @Published var ssMARS: Bool = false
    @Published var ssAppIntent: SQLServerAppIntent = .readWrite
    @Published var ssMultiSubnetFailover: Bool = false
    @Published var ssFailoverPartner: String = ""
    @Published var ssPooling: Bool = true
    @Published var ssMinPool: String = "0"
    @Published var ssMaxPool: String = "100"

    // MARK: MySQL – General
    @Published var myServer: String = ""
    @Published var myPort: String = "3306"
    @Published var myDatabase: String = ""
    @Published var myUsername: String = ""
    @Published var myPassword: String = ""

    // MARK: MySQL – Security
    @Published var mySSLMode: MySQLSSLMode = .preferred
    @Published var myCAPath: String = ""
    @Published var myClientCertPath: String = ""
    @Published var myClientKeyPath: String = ""

    // MARK: MySQL – Advanced
    @Published var myConnTimeout: String = "15"
    @Published var myCmdTimeout: String = "30"
    @Published var myCharacterSet: String = "utf8mb4"
    @Published var myAllowZeroDateTime: Bool = false
    @Published var myAllowUserVariables: Bool = false
    @Published var myPooling: Bool = true
    @Published var myMinPool: String = "0"
    @Published var myMaxPool: String = "100"

    // MARK: PostgreSQL – General
    @Published var pgServer: String = ""
    @Published var pgPort: String = "5432"
    @Published var pgDatabase: String = ""
    @Published var pgUsername: String = ""
    @Published var pgPassword: String = ""

    // MARK: PostgreSQL – Security
    @Published var pgSSLMode: PostgreSQLSSLMode = .prefer
    @Published var pgSSLCertPath: String = ""
    @Published var pgSSLKeyPath: String = ""
    @Published var pgRootCertPath: String = ""

    // MARK: PostgreSQL – Advanced
    @Published var pgConnTimeout: String = "15"
    @Published var pgCmdTimeout: String = "30"
    @Published var pgAppName: String = "PasswordManager"
    @Published var pgSearchPath: String = ""
    @Published var pgPooling: Bool = true
    @Published var pgMinPool: String = "0"
    @Published var pgMaxPool: String = "100"

    // MARK: - Connection string preview

    var connectionStringPreview: String {
        switch selectedProvider {
        case .sqlite:
            let path = sqliteFilePath.isEmpty ? "~/Documents/passwordmanager.db" : sqliteFilePath
            return "Data Source=\(path);"

        case .sqlServer:
            var parts: [String] = []
            let host = ssInstance.isEmpty ? ssServer : "\(ssServer)\\\(ssInstance)"
            parts.append("Server=\(host),\(ssPort)")
            if !ssDatabase.isEmpty { parts.append("Database=\(ssDatabase)") }
            switch ssAuthMode {
            case .windows:
                parts.append("Integrated Security=True")
            case .sqlServer:
                parts.append("User Id=\(ssUsername)")
                parts.append("Password=***")
            }
            parts.append("Encrypt=\(ssEncryption.rawValue)")
            parts.append("TrustServerCertificate=\(ssTrustCert)")
            parts.append("Application Name=\(ssAppName)")
            return parts.joined(separator: ";")

        case .mysql:
            var parts: [String] = []
            parts.append("Server=\(myServer)")
            parts.append("Port=\(myPort)")
            if !myDatabase.isEmpty { parts.append("Database=\(myDatabase)") }
            parts.append("Uid=\(myUsername)")
            parts.append("Pwd=***")
            parts.append("SslMode=\(mySSLMode.rawValue)")
            parts.append("CharSet=\(myCharacterSet)")
            return parts.joined(separator: ";")

        case .postgresql:
            var parts: [String] = []
            parts.append("Host=\(pgServer)")
            parts.append("Port=\(pgPort)")
            if !pgDatabase.isEmpty { parts.append("Database=\(pgDatabase)") }
            parts.append("Username=\(pgUsername)")
            parts.append("Password=***")
            parts.append("SSL Mode=\(pgSSLMode.rawValue)")
            if !pgAppName.isEmpty { parts.append("Application Name=\(pgAppName)") }
            return parts.joined(separator: ";")
        }
    }

    // MARK: - Async stubs

    func testConnection() async {
        isTesting = true
        testResult = nil
        // TODO: replace with native HTTP call to PasswordManager API or a
        //       direct database driver (e.g. SQLite.swift, vapor/postgres-nio).
        try? await Task.sleep(nanoseconds: 1_500_000_000)
        testResult = (true, "Connection successful")
        isTesting = false
    }

    func saveConfiguration() async {
        isSaving = true
        saveError = nil
        // TODO: persist to Keychain / UserDefaults / app group container.
        try? await Task.sleep(nanoseconds: 800_000_000)
        isSaving = false
    }
}

// ---------------------------------------------------------------------------
// MARK: - Root view
// ---------------------------------------------------------------------------

struct DatabaseSetupView: View {
    @StateObject private var vm = DatabaseSetupViewModel()

    var body: some View {
        NavigationStack {
            Group {
                switch vm.currentStep {
                case 1:  ProviderSelectionStep(vm: vm)
                case 2:  ConfigurationStep(vm: vm)
                case 3:  TestAndSaveStep(vm: vm)
                default: ProviderSelectionStep(vm: vm)
                }
            }
            .navigationTitle(stepTitle)
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .principal) {
                    StepIndicator(current: vm.currentStep, total: 3)
                }
            }
        }
        .accentColor(.pmAccent)
    }

    private var stepTitle: String {
        switch vm.currentStep {
        case 1: return "Database Setup"
        case 2: return "Configuration"
        case 3: return "Review & Save"
        default: return "Database Setup"
        }
    }
}

// ---------------------------------------------------------------------------
// MARK: - Step indicator
// ---------------------------------------------------------------------------

private struct StepIndicator: View {
    let current: Int
    let total: Int

    var body: some View {
        HStack(spacing: 6) {
            ForEach(1...total, id: \.self) { step in
                Circle()
                    .fill(step <= current ? Color.pmAccent : Color(.systemGray4))
                    .frame(width: 8, height: 8)
            }
        }
    }
}

// ---------------------------------------------------------------------------
// MARK: - Step 1: Provider Selection
// ---------------------------------------------------------------------------

private struct ProviderSelectionStep: View {
    @ObservedObject var vm: DatabaseSetupViewModel

    private let columns = [GridItem(.flexible()), GridItem(.flexible())]

    var body: some View {
        ScrollView {
            VStack(spacing: 24) {
                // Header
                VStack(spacing: 6) {
                    Text("Choose Database Provider")
                        .font(.title2).bold()
                    Text("Select where your passwords will be stored")
                        .font(.subheadline)
                        .foregroundColor(.secondary)
                        .multilineTextAlignment(.center)
                }
                .padding(.top, 16)
                .padding(.horizontal)

                // Provider grid
                LazyVGrid(columns: columns, spacing: 16) {
                    ForEach(DBProvider.allCases) { provider in
                        ProviderCard(
                            provider: provider,
                            isSelected: vm.selectedProvider == provider
                        )
                        .onTapGesture {
                            withAnimation(.easeInOut(duration: 0.15)) {
                                vm.selectedProvider = provider
                            }
                        }
                    }
                }
                .padding(.horizontal)

                // Continue button
                Button {
                    withAnimation {
                        vm.currentStep = 2
                        vm.configTab = 0
                    }
                } label: {
                    Label("Continue", systemImage: "chevron.right")
                        .labelStyle(TrailingIconLabelStyle())
                        .frame(maxWidth: .infinity)
                        .padding(.vertical, 14)
                        .background(Color.pmAccent)
                        .foregroundColor(.white)
                        .clipShape(RoundedRectangle(cornerRadius: 12))
                }
                .padding(.horizontal)
                .padding(.bottom, 32)
            }
        }
    }
}

// MARK: Provider card

private struct ProviderCard: View {
    let provider: DBProvider
    let isSelected: Bool

    var body: some View {
        VStack(spacing: 12) {
            ZStack(alignment: .topTrailing) {
                Image(systemName: provider.systemImage)
                    .font(.system(size: 36))
                    .foregroundColor(isSelected ? .pmAccent : .secondary)
                    .frame(maxWidth: .infinity, alignment: .center)
                    .padding(.top, 4)

                if provider.isRecommended {
                    Text("Recommended")
                        .font(.caption2).bold()
                        .padding(.horizontal, 6)
                        .padding(.vertical, 3)
                        .background(Color.pmAccent.opacity(0.15))
                        .foregroundColor(.pmAccent)
                        .clipShape(Capsule())
                        .offset(x: 4, y: -4)
                }
            }

            Text(provider.rawValue)
                .font(.subheadline).bold()
                .multilineTextAlignment(.center)
                .foregroundColor(isSelected ? .pmAccent : .primary)
        }
        .padding(16)
        .frame(maxWidth: .infinity, minHeight: 110)
        .background(Color(.secondarySystemGroupedBackground))
        .clipShape(RoundedRectangle(cornerRadius: 12))
        .overlay(
            RoundedRectangle(cornerRadius: 12)
                .stroke(
                    isSelected ? Color.pmAccent : Color(.systemGray5),
                    lineWidth: isSelected ? 2 : 1
                )
        )
        .shadow(color: isSelected ? Color.pmAccent.opacity(0.15) : .clear, radius: 6)
    }
}

// ---------------------------------------------------------------------------
// MARK: - Step 2: Configuration
// ---------------------------------------------------------------------------

private struct ConfigurationStep: View {
    @ObservedObject var vm: DatabaseSetupViewModel

    var body: some View {
        VStack(spacing: 0) {
            // Provider header
            Text(vm.selectedProvider.rawValue)
                .font(.headline)
                .frame(maxWidth: .infinity, alignment: .leading)
                .padding(.horizontal)
                .padding(.top, 12)
                .padding(.bottom, 8)

            // Sub-tab picker (General / Security / Advanced)
            Picker("Tab", selection: $vm.configTab) {
                Text("General").tag(0)
                Text("Security").tag(1)
                Text("Advanced").tag(2)
            }
            .pickerStyle(.segmented)
            .padding(.horizontal)
            .padding(.bottom, 8)

            // Tab content
            Form {
                switch vm.selectedProvider {
                case .sqlite:     SQLiteConfigView(vm: vm)
                case .sqlServer:  SQLServerConfigView(vm: vm)
                case .mysql:      MySQLConfigView(vm: vm)
                case .postgresql: PostgreSQLConfigView(vm: vm)
                }
            }
            .formStyle(.grouped)

            // Bottom nav row
            ConfigNavRow(vm: vm)
        }
    }
}

// MARK: Bottom navigation row for Step 2

private struct ConfigNavRow: View {
    @ObservedObject var vm: DatabaseSetupViewModel

    var body: some View {
        HStack(spacing: 12) {
            // Back
            Button {
                withAnimation { vm.currentStep = 1 }
            } label: {
                Label("Back", systemImage: "chevron.left")
                    .foregroundColor(.pmAccent)
            }

            Spacer()

            // Test Connection
            Button {
                Task { await vm.testConnection() }
            } label: {
                if vm.isTesting {
                    HStack(spacing: 6) {
                        ProgressView()
                            .progressViewStyle(.circular)
                            .scaleEffect(0.8)
                        Text("Testing…")
                    }
                } else if let result = vm.testResult {
                    HStack(spacing: 4) {
                        Image(systemName: result.success
                              ? "checkmark.circle.fill"
                              : "xmark.circle.fill")
                            .foregroundColor(result.success ? .green : .red)
                        Text("Tested")
                    }
                } else {
                    Text("Test Connection")
                }
            }
            .buttonStyle(.bordered)
            .disabled(vm.isTesting)

            // Continue
            Button {
                withAnimation { vm.currentStep = 3 }
            } label: {
                Label("Continue", systemImage: "chevron.right")
                    .labelStyle(TrailingIconLabelStyle())
            }
            .buttonStyle(.borderedProminent)
            .tint(.pmAccent)
        }
        .padding(.horizontal)
        .padding(.vertical, 12)
        .background(Color(.systemGroupedBackground))
    }
}

// ---------------------------------------------------------------------------
// MARK: - SQLite config
// ---------------------------------------------------------------------------

private struct SQLiteConfigView: View {
    @ObservedObject var vm: DatabaseSetupViewModel

    var body: some View {
        // Only "General" is meaningful for SQLite
        Section(header: Text("General").font(.headline)) {
            LabeledTextField(
                label: "Database File Path",
                placeholder: "Leave empty for default",
                text: $vm.sqliteFilePath
            )
            InfoNote(text: "Default path: ~/Documents/passwordmanager.db")
        }
    }
}

// ---------------------------------------------------------------------------
// MARK: - SQL Server config
// ---------------------------------------------------------------------------

private struct SQLServerConfigView: View {
    @ObservedObject var vm: DatabaseSetupViewModel

    var body: some View {
        switch vm.configTab {
        case 0: ssGeneral
        case 1: ssSecurity
        default: ssAdvanced
        }
    }

    // MARK: General tab
    @ViewBuilder
    private var ssGeneral: some View {
        Section(header: Text("Connection").font(.headline)) {
            LabeledTextField(label: "Server",   placeholder: "hostname or IP",  text: $vm.ssServer)
            LabeledTextField(label: "Instance", placeholder: "optional",         text: $vm.ssInstance)
            LabeledTextField(label: "Port",     placeholder: "1433",             text: $vm.ssPort)
                .keyboardType(.numberPad)
            LabeledTextField(label: "Database", placeholder: "database name",    text: $vm.ssDatabase)
        }

        Section(header: Text("Authentication").font(.headline)) {
            Picker("Auth Mode", selection: $vm.ssAuthMode) {
                ForEach(SQLServerAuthMode.allCases, id: \.self) { mode in
                    Text(mode.rawValue).tag(mode)
                }
            }

            if vm.ssAuthMode == .sqlServer {
                LabeledTextField(label: "Username", placeholder: "sa", text: $vm.ssUsername)
                LabeledSecureField(label: "Password", text: $vm.ssPassword)
            }
        }
    }

    // MARK: Security tab
    @ViewBuilder
    private var ssSecurity: some View {
        Section(header: Text("Encryption").font(.headline)) {
            Picker("Encryption", selection: $vm.ssEncryption) {
                ForEach(SQLServerEncryption.allCases, id: \.self) { enc in
                    Text(enc.rawValue).tag(enc)
                }
            }
            Toggle("Trust Server Certificate", isOn: $vm.ssTrustCert)

            if vm.ssEncryption == .mandatory || vm.ssEncryption == .strict {
                LabeledTextField(
                    label: "Server Certificate",
                    placeholder: "Thumbprint or file path",
                    text: $vm.ssServerCert
                )
            }
        }
    }

    // MARK: Advanced tab
    @ViewBuilder
    private var ssAdvanced: some View {
        Section(header: Text("Network").font(.headline)) {
            Picker("Network Protocol", selection: $vm.ssNetworkProtocol) {
                ForEach(SQLServerNetworkProtocol.allCases, id: \.self) { proto in
                    Text(proto.rawValue).tag(proto)
                }
            }
            LabeledTextField(label: "Packet Size",    placeholder: "4096",  text: $vm.ssPacketSize)
                .keyboardType(.numberPad)
        }

        Section(header: Text("Timeouts").font(.headline)) {
            LabeledTextField(label: "Connection Timeout (s)", placeholder: "15", text: $vm.ssConnTimeout)
                .keyboardType(.numberPad)
            LabeledTextField(label: "Command Timeout (s)",    placeholder: "30", text: $vm.ssCmdTimeout)
                .keyboardType(.numberPad)
        }

        Section(header: Text("Application").font(.headline)) {
            LabeledTextField(label: "Application Name", placeholder: "PasswordManager", text: $vm.ssAppName)
            Toggle("MARS (Multiple Active Result Sets)", isOn: $vm.ssMARS)
            Picker("Application Intent", selection: $vm.ssAppIntent) {
                ForEach(SQLServerAppIntent.allCases, id: \.self) { intent in
                    Text(intent.rawValue).tag(intent)
                }
            }
            Toggle("Multi-Subnet Failover", isOn: $vm.ssMultiSubnetFailover)
            LabeledTextField(label: "Failover Partner", placeholder: "optional", text: $vm.ssFailoverPartner)
        }

        Section(header: Text("Connection Pooling").font(.headline)) {
            Toggle("Enable Pooling", isOn: $vm.ssPooling)
            if vm.ssPooling {
                LabeledTextField(label: "Min Pool Size", placeholder: "0",   text: $vm.ssMinPool)
                    .keyboardType(.numberPad)
                LabeledTextField(label: "Max Pool Size", placeholder: "100", text: $vm.ssMaxPool)
                    .keyboardType(.numberPad)
            }
        }
    }
}

// ---------------------------------------------------------------------------
// MARK: - MySQL config
// ---------------------------------------------------------------------------

private struct MySQLConfigView: View {
    @ObservedObject var vm: DatabaseSetupViewModel

    var body: some View {
        switch vm.configTab {
        case 0: myGeneral
        case 1: mySecurity
        default: myAdvanced
        }
    }

    @ViewBuilder
    private var myGeneral: some View {
        Section(header: Text("Connection").font(.headline)) {
            LabeledTextField(label: "Server",   placeholder: "hostname or IP", text: $vm.myServer)
            LabeledTextField(label: "Port",     placeholder: "3306",           text: $vm.myPort)
                .keyboardType(.numberPad)
            LabeledTextField(label: "Database", placeholder: "database name",  text: $vm.myDatabase)
            LabeledTextField(label: "Username", placeholder: "root",           text: $vm.myUsername)
            LabeledSecureField(label: "Password", text: $vm.myPassword)
        }
    }

    @ViewBuilder
    private var mySecurity: some View {
        Section(header: Text("SSL / TLS").font(.headline)) {
            Picker("SSL Mode", selection: $vm.mySSLMode) {
                ForEach(MySQLSSLMode.allCases, id: \.self) { mode in
                    Text(mode.rawValue).tag(mode)
                }
            }

            if vm.mySSLMode == .verifyCA || vm.mySSLMode == .verifyFull {
                LabeledTextField(label: "CA Certificate Path",     placeholder: "/path/to/ca.pem",   text: $vm.myCAPath)
                LabeledTextField(label: "Client Certificate Path", placeholder: "/path/to/cert.pem", text: $vm.myClientCertPath)
                LabeledTextField(label: "Client Key Path",         placeholder: "/path/to/key.pem",  text: $vm.myClientKeyPath)
            }
        }
    }

    @ViewBuilder
    private var myAdvanced: some View {
        Section(header: Text("Timeouts").font(.headline)) {
            LabeledTextField(label: "Connection Timeout (s)", placeholder: "15", text: $vm.myConnTimeout)
                .keyboardType(.numberPad)
            LabeledTextField(label: "Command Timeout (s)",    placeholder: "30", text: $vm.myCmdTimeout)
                .keyboardType(.numberPad)
        }

        Section(header: Text("Options").font(.headline)) {
            LabeledTextField(label: "Character Set", placeholder: "utf8mb4", text: $vm.myCharacterSet)
            Toggle("Allow Zero DateTime",   isOn: $vm.myAllowZeroDateTime)
            Toggle("Allow User Variables",  isOn: $vm.myAllowUserVariables)
        }

        Section(header: Text("Connection Pooling").font(.headline)) {
            Toggle("Enable Pooling", isOn: $vm.myPooling)
            if vm.myPooling {
                LabeledTextField(label: "Min Pool Size", placeholder: "0",   text: $vm.myMinPool)
                    .keyboardType(.numberPad)
                LabeledTextField(label: "Max Pool Size", placeholder: "100", text: $vm.myMaxPool)
                    .keyboardType(.numberPad)
            }
        }
    }
}

// ---------------------------------------------------------------------------
// MARK: - PostgreSQL config
// ---------------------------------------------------------------------------

private struct PostgreSQLConfigView: View {
    @ObservedObject var vm: DatabaseSetupViewModel

    var body: some View {
        switch vm.configTab {
        case 0: pgGeneral
        case 1: pgSecurity
        default: pgAdvanced
        }
    }

    @ViewBuilder
    private var pgGeneral: some View {
        Section(header: Text("Connection").font(.headline)) {
            LabeledTextField(label: "Server",   placeholder: "hostname or IP", text: $vm.pgServer)
            LabeledTextField(label: "Port",     placeholder: "5432",           text: $vm.pgPort)
                .keyboardType(.numberPad)
            LabeledTextField(label: "Database", placeholder: "database name",  text: $vm.pgDatabase)
            LabeledTextField(label: "Username", placeholder: "postgres",       text: $vm.pgUsername)
            LabeledSecureField(label: "Password", text: $vm.pgPassword)
        }
    }

    @ViewBuilder
    private var pgSecurity: some View {
        Section(header: Text("SSL / TLS").font(.headline)) {
            Picker("SSL Mode", selection: $vm.pgSSLMode) {
                ForEach(PostgreSQLSSLMode.allCases, id: \.self) { mode in
                    Text(mode.rawValue).tag(mode)
                }
            }

            if vm.pgSSLMode == .verifyCA || vm.pgSSLMode == .verifyFull {
                LabeledTextField(label: "SSL Certificate Path", placeholder: "/path/to/cert.pem", text: $vm.pgSSLCertPath)
                LabeledTextField(label: "SSL Key Path",         placeholder: "/path/to/key.pem",  text: $vm.pgSSLKeyPath)
                LabeledTextField(label: "Root Certificate Path",placeholder: "/path/to/ca.pem",   text: $vm.pgRootCertPath)
            }
        }
    }

    @ViewBuilder
    private var pgAdvanced: some View {
        Section(header: Text("Timeouts").font(.headline)) {
            LabeledTextField(label: "Connection Timeout (s)", placeholder: "15", text: $vm.pgConnTimeout)
                .keyboardType(.numberPad)
            LabeledTextField(label: "Command Timeout (s)",    placeholder: "30", text: $vm.pgCmdTimeout)
                .keyboardType(.numberPad)
        }

        Section(header: Text("Application").font(.headline)) {
            LabeledTextField(label: "Application Name", placeholder: "PasswordManager", text: $vm.pgAppName)
            LabeledTextField(label: "Search Path",      placeholder: "public",           text: $vm.pgSearchPath)
        }

        Section(header: Text("Connection Pooling").font(.headline)) {
            Toggle("Enable Pooling", isOn: $vm.pgPooling)
            if vm.pgPooling {
                LabeledTextField(label: "Min Pool Size", placeholder: "0",   text: $vm.pgMinPool)
                    .keyboardType(.numberPad)
                LabeledTextField(label: "Max Pool Size", placeholder: "100", text: $vm.pgMaxPool)
                    .keyboardType(.numberPad)
            }
        }
    }
}

// ---------------------------------------------------------------------------
// MARK: - Step 3: Test & Save
// ---------------------------------------------------------------------------

private struct TestAndSaveStep: View {
    @ObservedObject var vm: DatabaseSetupViewModel
    @Environment(\.dismiss) private var dismiss

    var body: some View {
        ScrollView {
            VStack(spacing: 24) {
                // Status card
                StatusCard(result: vm.testResult, isTesting: vm.isTesting)
                    .padding(.horizontal)

                // Connection string preview
                VStack(alignment: .leading, spacing: 8) {
                    Text("Connection String Preview")
                        .font(.headline)
                        .padding(.horizontal)

                    ScrollView(.horizontal, showsIndicators: true) {
                        Text(vm.connectionStringPreview)
                            .font(.system(.footnote, design: .monospaced))
                            .foregroundColor(.secondary)
                            .padding(12)
                    }
                    .background(Color(.secondarySystemGroupedBackground))
                    .clipShape(RoundedRectangle(cornerRadius: 10))
                    .padding(.horizontal)

                    Text("Passwords are masked in the preview above.")
                        .font(.caption)
                        .foregroundColor(.secondary)
                        .padding(.horizontal)
                }

                // Action buttons
                VStack(spacing: 12) {
                    // Re-test
                    Button {
                        Task { await vm.testConnection() }
                    } label: {
                        HStack {
                            if vm.isTesting {
                                ProgressView()
                                    .progressViewStyle(.circular)
                                    .scaleEffect(0.85)
                                Text("Testing…")
                            } else {
                                Image(systemName: "arrow.clockwise")
                                Text("Re-test Connection")
                            }
                        }
                        .frame(maxWidth: .infinity)
                        .padding(.vertical, 14)
                        .overlay(
                            RoundedRectangle(cornerRadius: 12)
                                .stroke(Color.pmAccent, lineWidth: 1.5)
                        )
                        .foregroundColor(.pmAccent)
                    }
                    .disabled(vm.isTesting || vm.isSaving)

                    // Save
                    Button {
                        Task {
                            await vm.saveConfiguration()
                            if vm.saveError == nil {
                                dismiss()
                            }
                        }
                    } label: {
                        HStack {
                            if vm.isSaving {
                                ProgressView()
                                    .progressViewStyle(.circular)
                                    .scaleEffect(0.85)
                                    .tint(.white)
                                Text("Saving…")
                            } else {
                                Image(systemName: "checkmark.circle.fill")
                                Text("Save Configuration")
                            }
                        }
                        .frame(maxWidth: .infinity)
                        .padding(.vertical, 14)
                        .background(Color.pmAccent)
                        .foregroundColor(.white)
                        .clipShape(RoundedRectangle(cornerRadius: 12))
                    }
                    .disabled(vm.isTesting || vm.isSaving)

                    if let error = vm.saveError {
                        Text(error)
                            .font(.footnote)
                            .foregroundColor(.red)
                            .multilineTextAlignment(.center)
                    }

                    // Back
                    Button {
                        withAnimation { vm.currentStep = 2 }
                    } label: {
                        Label("Back", systemImage: "chevron.left")
                            .foregroundColor(.pmAccent)
                    }
                    .disabled(vm.isTesting || vm.isSaving)
                }
                .padding(.horizontal)
                .padding(.bottom, 40)
            }
            .padding(.top, 16)
        }
        .task {
            // Auto-run a first test when the step loads if not already done.
            if vm.testResult == nil && !vm.isTesting {
                await vm.testConnection()
            }
        }
    }
}

// MARK: Status card

private struct StatusCard: View {
    let result: (success: Bool, message: String)?
    let isTesting: Bool

    var body: some View {
        HStack(spacing: 16) {
            Group {
                if isTesting {
                    ProgressView()
                        .progressViewStyle(.circular)
                        .scaleEffect(1.3)
                } else if let result {
                    Image(systemName: result.success
                          ? "checkmark.circle.fill"
                          : "xmark.circle.fill")
                        .font(.system(size: 40))
                        .foregroundColor(result.success ? .green : .red)
                } else {
                    Image(systemName: "questionmark.circle.fill")
                        .font(.system(size: 40))
                        .foregroundColor(.secondary)
                }
            }
            .frame(width: 48)

            VStack(alignment: .leading, spacing: 4) {
                if isTesting {
                    Text("Testing connection…")
                        .font(.headline)
                    Text("Please wait")
                        .font(.subheadline)
                        .foregroundColor(.secondary)
                } else if let result {
                    Text(result.success ? "Connection Successful" : "Connection Failed")
                        .font(.headline)
                        .foregroundColor(result.success ? .green : .red)
                    Text(result.message)
                        .font(.subheadline)
                        .foregroundColor(.secondary)
                } else {
                    Text("Not tested yet")
                        .font(.headline)
                    Text("Tap Re-test to verify your connection")
                        .font(.subheadline)
                        .foregroundColor(.secondary)
                }
            }

            Spacer()
        }
        .padding(16)
        .background(Color(.secondarySystemGroupedBackground))
        .clipShape(RoundedRectangle(cornerRadius: 14))
        .overlay(
            RoundedRectangle(cornerRadius: 14)
                .stroke(cardBorderColor, lineWidth: 1.5)
        )
    }

    private var cardBorderColor: Color {
        guard !isTesting, let result else { return Color(.systemGray5) }
        return result.success ? .green.opacity(0.6) : .red.opacity(0.6)
    }
}

// ---------------------------------------------------------------------------
// MARK: - Reusable helpers
// ---------------------------------------------------------------------------

/// TextField with a fixed leading label, matching SSMS-style aligned layout.
private struct LabeledTextField: View {
    let label: String
    let placeholder: String
    @Binding var text: String
    var keyboardType: UIKeyboardType = .default

    var body: some View {
        HStack {
            Text(label)
                .foregroundColor(.primary)
                .frame(width: 160, alignment: .leading)
            TextField(placeholder, text: $text)
                .keyboardType(keyboardType)
                .autocorrectionDisabled()
                .textInputAutocapitalization(.never)
        }
    }
}

/// SecureField with a fixed leading label.
private struct LabeledSecureField: View {
    let label: String
    @Binding var text: String

    var body: some View {
        HStack {
            Text(label)
                .foregroundColor(.primary)
                .frame(width: 160, alignment: .leading)
            SecureField("••••••••", text: $text)
        }
    }
}

/// Inline info note, shown below a field.
private struct InfoNote: View {
    let text: String

    var body: some View {
        Label(text, systemImage: "info.circle")
            .font(.caption)
            .foregroundColor(.secondary)
            .padding(.top, 2)
    }
}

/// Label style that places the icon after the title (for "Continue →").
private struct TrailingIconLabelStyle: LabelStyle {
    func makeBody(configuration: Configuration) -> some View {
        HStack(spacing: 6) {
            configuration.title
            configuration.icon
        }
    }
}

// ---------------------------------------------------------------------------
// MARK: - Preview
// ---------------------------------------------------------------------------

#if DEBUG
struct DatabaseSetupView_Previews: PreviewProvider {
    static var previews: some View {
        Group {
            DatabaseSetupView()
                .previewDisplayName("Step 1 – Light")

            DatabaseSetupView()
                .preferredColorScheme(.dark)
                .previewDisplayName("Step 1 – Dark")
        }
    }
}
#endif
