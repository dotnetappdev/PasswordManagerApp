// ItemEditView.swift — create/edit a login item with a polished, sectioned form.
import SwiftUI

@MainActor
struct ItemEditView: View {
    @EnvironmentObject var env: AppEnvironment
    @EnvironmentObject var settingsStore: SettingsStore
    @Environment(\.dismiss) private var dismiss

    let itemId: Int?
    var onSaved: () -> Void

    @State private var title = ""
    @State private var descriptionText = ""
    @State private var username = ""
    @State private var email = ""
    @State private var website = ""
    @State private var loginUrl = ""
    @State private var password = ""
    @State private var totpSecret = ""
    @State private var notes = ""
    @State private var isFavorite = false
    @State private var customFields: [CustomFieldData] = []
    @State private var showPassword = false
    @State private var showScanner = false
    @State private var saving = false
    @State private var error: String?

    private var isNew: Bool { itemId == nil }

    var body: some View {
        Form {
            Section {
                HStack {
                    ItemIconTile(type: .login, size: 52)
                    TextField("Title", text: $title).font(.title3)
                }
            }

            Section("Login details") {
                labeled("Username", $username)
                labeled("Email", $email, keyboard: .emailAddress)
                labeled("Website", $website, keyboard: .URL)
                labeled("Login URL", $loginUrl, keyboard: .URL)
            }

            Section("Password") {
                HStack {
                    Group {
                        if showPassword { TextField(isNew ? "Password" : "Leave blank to keep", text: $password) }
                        else { SecureField(isNew ? "Password" : "Leave blank to keep", text: $password) }
                    }
                    .font(.system(.body, design: .monospaced))
                    Button { showPassword.toggle() } label: { Image(systemName: showPassword ? "eye.slash" : "eye") }
                    Button { generate() } label: { Image(systemName: "wand.and.stars") }
                }
                if !password.isEmpty { StrengthMeter(strength: PasswordGenerator.strength(password)) }
                Button { generate() } label: { Label("Generate strong password", systemImage: "wand.and.stars") }
            }

            Section("One-time password") {
                HStack {
                    TextField("Authenticator (TOTP) secret", text: $totpSecret)
                        .textInputAutocapitalization(.never).autocorrectionDisabled()
                    Button { showScanner = true } label: { Image(systemName: "qrcode.viewfinder") }
                }
            }

            Section("Custom fields") {
                if customFields.isEmpty {
                    Text("Add your own fields — a PIN, a recovery code, a membership number…")
                        .font(.caption).foregroundStyle(.secondary)
                }
                ForEach(customFields.indices, id: \.self) { i in
                    VStack(spacing: 6) {
                        TextField("Label", text: $customFields[i].name)
                            .font(.subheadline.weight(.medium))

                        // Field type picker — mirrors the WPF/Blazor/Android custom-field types exactly.
                        Picker("Type", selection: Binding(
                            get: { customFields[i].fieldType },
                            set: { customFields[i].type = $0.code
                                   if $0.isSecret { customFields[i].secret = true } })
                        ) {
                            ForEach(CustomFieldType.allCases) { t in Text(t.label).tag(t) }
                        }
                        .font(.caption)

                        customFieldValueEditor(index: i)
                    }
                    .padding(.vertical, 2)
                }
                .onDelete { customFields.remove(atOffsets: $0) }
                Button {
                    customFields.append(CustomFieldData(name: "", value: ""))
                } label: {
                    Label("Add custom field", systemImage: "plus.circle")
                }
            }

            Section("More") {
                labeled("Description", $descriptionText)
                VStack(alignment: .leading) { Text("Notes").font(.caption).foregroundStyle(.secondary); TextEditor(text: $notes).frame(minHeight: 80) }
                Toggle("Favourite", isOn: $isFavorite)
            }

            if let error { Text(error).foregroundStyle(.red) }
        }
        .navigationTitle(isNew ? "New Item" : "Edit Item")
        .navigationBarTitleDisplayMode(.inline)
        .toolbar {
            ToolbarItem(placement: .navigationBarTrailing) {
                Button("Save") { Task { await save() } }.disabled(saving)
            }
        }
        .sheet(isPresented: $showScanner) {
            QRScannerView { value in totpSecret = Totp.secretFromUri(value); showScanner = false } onCancel: { showScanner = false }
        }
        .task { await loadIfEditing() }
    }

    private func labeled(_ label: String, _ text: Binding<String>, keyboard: UIKeyboardType = .default) -> some View {
        TextField(label, text: text)
            .keyboardType(keyboard)
            .textInputAutocapitalization(keyboard == .emailAddress || keyboard == .URL ? .never : .sentences)
            .autocorrectionDisabled(keyboard == .emailAddress || keyboard == .URL)
    }

    /// A value editor adapted to the field's type — Yes/No toggle, multi-line text, secure entry, or a
    /// keyboard-appropriate text field — matching the desktop custom-field types.
    @ViewBuilder
    private func customFieldValueEditor(index i: Int) -> some View {
        let type = customFields[i].fieldType
        switch type {
        case .toggle:
            Toggle(isOn: Binding(
                get: { customFields[i].value.lowercased() == "true" || customFields[i].value == "1" },
                set: { customFields[i].value = $0 ? "true" : "false" })
            ) { Text("Yes / No") }
                .font(.subheadline)
        case .textArea, .address:
            TextField("Value", text: $customFields[i].value, axis: .vertical)
                .lineLimit(3...6)
                .autocorrectionDisabled()
        default:
            HStack {
                Group {
                    if customFields[i].isMasked {
                        SecureField("Value", text: $customFields[i].value)
                    } else {
                        TextField("Value", text: $customFields[i].value)
                            .keyboardType(keyboardType(for: type))
                    }
                }
                .autocorrectionDisabled()
                .textInputAutocapitalization(.never)
                if type.isSecret == false {
                    Button { customFields[i].secret.toggle() } label: {
                        Image(systemName: customFields[i].secret ? "eye.slash" : "eye")
                    }
                    .buttonStyle(.borderless)
                }
            }
        }
    }

    private func keyboardType(for type: CustomFieldType) -> UIKeyboardType {
        switch type {
        case .number: return .numberPad
        case .email: return .emailAddress
        case .phone: return .phonePad
        case .url, .signInWith: return .URL
        default: return .default
        }
    }

    private func loadIfEditing() async {
        guard let itemId else { return }
        if let item = await env.repository.get(id: itemId) {
            title = item.title; descriptionText = item.description ?? ""
            username = item.username ?? ""; email = item.email ?? ""
            website = item.website ?? ""; loginUrl = item.loginUrl ?? ""
            notes = item.notes ?? ""; isFavorite = item.isFavorite
            customFields = item.customFields
        }
    }

    private func generate() {
        let s = settingsStore.settings
        password = PasswordGenerator.generate(PasswordOptions(
            length: s.pwLength, upper: s.pwUpper, lower: s.pwLower,
            digits: s.pwDigits, symbols: s.pwSymbols, avoidAmbiguous: s.pwAvoidAmbiguous))
        showPassword = true
    }

    private func save() async {
        guard !title.isEmpty else { error = "Title is required."; return }
        saving = true; error = nil
        let input = LoginItemInput(
            title: title.trimmingCharacters(in: .whitespaces),
            description: descriptionText.isEmpty ? nil : descriptionText,
            type: .login, isFavorite: isFavorite,
            username: username.isEmpty ? nil : username,
            email: email.isEmpty ? nil : email,
            website: website.isEmpty ? nil : website,
            loginUrl: loginUrl.isEmpty ? nil : loginUrl,
            password: password.isEmpty ? nil : password,
            totpSecret: totpSecret.isEmpty ? nil : totpSecret,
            notes: notes.isEmpty ? nil : notes,
            customFields: customFields
                .map { CustomFieldData(name: $0.name.trimmingCharacters(in: .whitespaces),
                                       value: $0.value.trimmingCharacters(in: .whitespaces),
                                       secret: $0.secret, type: $0.type) }
                .filter { !$0.name.isEmpty || !$0.value.isEmpty })
        do {
            if let itemId { try await env.repository.update(id: itemId, input) }
            else { try await env.repository.create(input) }
            onSaved(); dismiss()
        } catch { self.error = error.localizedDescription }
        saving = false
    }
}
