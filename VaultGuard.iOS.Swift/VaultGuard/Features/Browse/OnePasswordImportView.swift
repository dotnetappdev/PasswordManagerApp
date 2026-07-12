// OnePasswordImportView.swift — enter a 1Password Connect server URL + token and import over the air.
import SwiftUI

@MainActor
struct OnePasswordImportView: View {
    @EnvironmentObject var env: AppEnvironment

    @State private var host = ""
    @State private var token = ""
    @State private var remember = true
    @State private var running = false
    @State private var imported = 0
    @State private var total = 0
    @State private var done = false
    @State private var error: String?

    var body: some View {
        Form {
            Section {
                Text("Connect to a 1Password Connect server (or a Service Account) to import your items directly. On desktop and web you can still import a 1PUX/CSV export file instead.")
                    .font(.footnote).foregroundStyle(.secondary)
            }
            Section("1Password Connect") {
                TextField("Connect server URL", text: $host)
                    .textInputAutocapitalization(.never).autocorrectionDisabled().keyboardType(.URL)
                SecureField("Access token", text: $token)
                Toggle("Remember this connection", isOn: $remember)
            }
            if let error {
                Section { Text(error).foregroundStyle(.red) }
            }
            if done {
                Section {
                    Label("Imported \(imported) item\(imported == 1 ? "" : "s") from 1Password.",
                          systemImage: "checkmark.circle.fill")
                        .foregroundStyle(Theme.accent)
                }
            }
            Section {
                Button { Task { await run() } } label: {
                    HStack {
                        if running { ProgressView() }
                        Text(running ? "Importing \(imported)/\(total)…" : (done ? "Import again" : "Start import"))
                    }
                }
                .disabled(running)
            }
        }
        .navigationTitle("Import from 1Password")
        .navigationBarTitleDisplayMode(.inline)
        .onAppear {
            // Prefill from the saved connection.
            if host.isEmpty { host = env.keychain.get(Keychain.Keys.onePasswordHost) ?? "" }
            if token.isEmpty { token = env.keychain.get(Keychain.Keys.onePasswordToken) ?? "" }
        }
    }

    private func run() async {
        running = true; error = nil; imported = 0; total = 0; done = false
        let result = await OnePasswordConnectImporter().fetch(host: host, token: token)
        if let e = result.error { error = e; running = false; return }
        // Persist (or clear) the connection for next time.
        if remember {
            env.keychain.set(host.trimmingCharacters(in: .whitespaces), for: Keychain.Keys.onePasswordHost)
            env.keychain.set(token.trimmingCharacters(in: .whitespaces), for: Keychain.Keys.onePasswordToken)
        } else {
            env.keychain.delete(Keychain.Keys.onePasswordHost)
            env.keychain.delete(Keychain.Keys.onePasswordToken)
        }
        total = result.items.count
        var count = 0
        for input in result.items {
            do { try await env.repository.create(input); count += 1; imported = count }
            catch { /* skip a single failed item and keep going */ }
        }
        running = false; done = true; imported = count
    }
}
