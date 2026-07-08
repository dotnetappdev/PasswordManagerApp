// ConnectionSetupView.swift — choose API (URL + key) or local SQLite mode.
import SwiftUI

@MainActor
struct ConnectionSetupView: View {
    @EnvironmentObject var env: AppEnvironment
    @EnvironmentObject var configStore: ConfigStore

    @State private var mode: ConnectionMode = .api
    @State private var apiUrl = ""
    @State private var apiKey = ""
    @State private var testing = false
    @State private var message: String?
    @State private var messageOK = false

    var body: some View {
        NavigationStack {
            Form {
                Section {
                    Picker("Mode", selection: $mode) {
                        Text("API Server").tag(ConnectionMode.api)
                        Text("Local (SQLite)").tag(ConnectionMode.local)
                    }
                    .pickerStyle(.segmented)
                } footer: {
                    Text("API mode syncs with your VaultGuard server. Local mode keeps an encrypted vault on this device only.")
                }

                if mode == .api {
                    Section("API Configuration") {
                        TextField("API URL (https://your-server:7001)", text: $apiUrl)
                            .textInputAutocapitalization(.never)
                            .autocorrectionDisabled()
                            .keyboardType(.URL)
                        SecureField("API Key", text: $apiKey)
                        Button {
                            Task { await test() }
                        } label: {
                            HStack {
                                if testing { ProgressView() }
                                Text("Test connection")
                            }
                        }
                        .disabled(testing)
                    } footer: {
                        Text("Generate an API key in the VaultGuard web app: Settings → API Keys.")
                    }
                } else {
                    Section {
                        Text("You'll set a master password on the next screen. Your vault is encrypted with AES-256-GCM and stored only on this device.")
                            .font(.footnote).foregroundStyle(.secondary)
                    }
                }

                if let message {
                    Section {
                        Label(message, systemImage: messageOK ? "checkmark.circle.fill" : "exclamationmark.triangle.fill")
                            .foregroundStyle(messageOK ? .green : .red)
                    }
                }
            }
            .navigationTitle("Connect VaultGuard")
            .safeAreaInset(edge: .bottom) {
                Button(action: save) {
                    Text("Save & continue").frame(maxWidth: .infinity).padding(.vertical, 6)
                }
                .buttonStyle(.borderedProminent)
                .padding()
            }
            .onAppear {
                mode = configStore.config.mode
                apiUrl = configStore.config.apiBaseUrl
                apiKey = configStore.apiKey() ?? ""
            }
        }
    }

    private func test() async {
        guard !apiUrl.isEmpty, !apiKey.isEmpty else {
            message = "Enter both an API URL and API key."; messageOK = false; return
        }
        testing = true; message = nil
        let error = await env.api.testConnection(baseUrl: apiUrl.trimmingCharacters(in: .whitespaces),
                                                 apiKey: apiKey.trimmingCharacters(in: .whitespaces))
        testing = false
        messageOK = (error == nil)
        message = error ?? "Connection successful."
    }

    private func save() {
        if mode == .api && (apiUrl.isEmpty || apiKey.isEmpty) {
            message = "Enter both an API URL and API key."; messageOK = false; return
        }
        configStore.save(mode: mode, apiBaseUrl: apiUrl, apiKey: mode == .api ? apiKey : nil)
    }
}
