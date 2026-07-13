// ItemDetailView.swift — read a vault item; reveal password + live TOTP on demand.
import SwiftUI

@MainActor
struct ItemDetailView: View {
    @EnvironmentObject var env: AppEnvironment
    @EnvironmentObject var settingsStore: SettingsStore
    @Environment(\.dismiss) private var dismiss
    let itemId: Int
    @Binding var path: [HomeRoute]

    @State private var item: VaultItem?
    @State private var revealed: String?
    @State private var revealing = false
    @State private var revealSecondsLeft = 0
    @State private var totpCode: String?
    @State private var totpRemaining = 0
    @State private var totpSecret: String?
    @State private var error: String?

    private let timer = Timer.publish(every: 1, on: .main, in: .common).autoconnect()

    var body: some View {
        Form {
            if let item {
                // 1Password-style item header: icon tile + title + category.
                Section {
                    HStack(spacing: 14) {
                        ItemIconTile(type: item.type, size: 52)
                        VStack(alignment: .leading, spacing: 2) {
                            Text(item.title).font(.title3.bold()).lineLimit(2)
                            if let cat = item.categoryName, !cat.isEmpty {
                                Label(cat, systemImage: "folder")
                                    .font(.caption).foregroundStyle(.secondary).labelStyle(.titleAndIcon)
                            }
                        }
                        Spacer()
                    }
                    .padding(.vertical, 4)
                }

                if let d = item.description, !d.isEmpty { field("Description", d) }
                if let v = item.username { copyRow("Username", v) }
                if let v = item.email { copyRow("Email", v) }
                if let v = item.website { copyRow("Website", v) }
                if let v = item.loginUrl { copyRow("Login URL", v) }

                Section("Password") {
                    if let revealed {
                        HStack(spacing: 16) {
                            Text(revealed).font(.system(.body, design: .monospaced)).textSelection(.enabled)
                            Spacer()
                            Button { hidePassword() } label: { Image(systemName: "eye.slash") }.buttonStyle(.borderless)
                            Button { copy(revealed) } label: { Image(systemName: "doc.on.doc") }.buttonStyle(.borderless)
                        }
                        if revealSecondsLeft > 0 {
                            Text("Hides in \(revealSecondsLeft)s").font(.caption).foregroundStyle(.secondary)
                        }
                    } else {
                        Button { Task { await reveal() } } label: {
                            Label(revealing ? "Revealing…" : "Reveal password", systemImage: "eye")
                        }.disabled(revealing)
                    }
                }

                if let totpCode {
                    Section("One-time code") {
                        HStack {
                            Text(spaced(totpCode)).font(.system(.title2, design: .monospaced))
                            Spacer()
                            Text("\(totpRemaining)s").foregroundStyle(.secondary)
                            Button { copy(totpCode) } label: { Image(systemName: "doc.on.doc") }
                        }
                        ProgressView(value: Double(totpRemaining), total: 30)
                    }
                }

                if !item.customFields.isEmpty {
                    Section("Custom fields") {
                        ForEach(item.customFields.indices, id: \.self) { i in
                            CustomFieldRow(field: item.customFields[i]) { copy($0) }
                        }
                    }
                }

                if let n = item.notes, !n.isEmpty { field("Notes", n) }
            } else if let error {
                Text(error).foregroundStyle(.red)
            } else {
                ProgressView()
            }
        }
        .navigationTitle(item?.title ?? "Item")
        .navigationBarTitleDisplayMode(.inline)
        .toolbar {
            ToolbarItemGroup(placement: .navigationBarTrailing) {
                Button { path.append(.edit(itemId)) } label: { Image(systemName: "pencil") }
                Button(role: .destructive) { Task { await deleteItem() } } label: { Image(systemName: "trash") }
            }
        }
        .task {
            item = await env.repository.get(id: itemId)
            await loadTotp()
        }
        .onReceive(timer) { _ in
            if let secret = totpSecret, let code = Totp.generate(base32Secret: secret) {
                totpCode = code.value; totpRemaining = code.secondsRemaining
            }
            // Auto-hide a revealed password once its countdown elapses.
            if revealed != nil && revealSecondsLeft > 0 {
                revealSecondsLeft -= 1
                if revealSecondsLeft == 0 { hidePassword() }
            }
        }
    }

    private func hidePassword() { revealed = nil; revealSecondsLeft = 0 }

    private func field(_ label: String, _ value: String) -> some View {
        Section(label) { Text(value).textSelection(.enabled) }
    }

    private func copyRow(_ label: String, _ value: String) -> some View {
        Section(label) {
            HStack {
                Text(value).textSelection(.enabled)
                Spacer()
                Button { copy(value) } label: { Image(systemName: "doc.on.doc") }
            }
        }
    }

    private func reveal() async {
        revealing = true; error = nil
        do {
            revealed = try await env.repository.secret(id: itemId).password
            // Start the user-configurable auto-hide countdown (0 = stay visible).
            revealSecondsLeft = revealed == nil ? 0 : settingsStore.settings.passwordAutoHideSeconds
        } catch { self.error = error.localizedDescription }
        revealing = false
    }

    /// Loads the authenticator secret up front so the one-time code is always visible — the password
    /// stays hidden behind "Reveal password", but the TOTP code shows immediately like an authenticator app.
    private func loadTotp() async {
        guard let s = try? await env.repository.secret(id: itemId),
              let secret = s.totpSecret, !secret.isEmpty else { return }
        totpSecret = secret
        if let code = Totp.generate(base32Secret: secret) { totpCode = code.value; totpRemaining = code.secondsRemaining }
    }

    private func deleteItem() async {
        do { try await env.repository.delete(id: itemId); dismiss() }
        catch { self.error = error.localizedDescription }
    }

    private func copy(_ value: String) { UIPasteboard.general.string = value }
    private func spaced(_ code: String) -> String {
        stride(from: 0, to: code.count, by: 3).map {
            let start = code.index(code.startIndex, offsetBy: $0)
            let end = code.index(start, offsetBy: min(3, code.count - $0))
            return String(code[start..<end])
        }.joined(separator: " ")
    }
}

/// A single custom-field row in the detail view. Password/OTP fields stay masked until revealed;
/// Toggle fields read as Yes/No. The type matches the desktop custom-field types.
private struct CustomFieldRow: View {
    let field: CustomFieldData
    let onCopy: (String) -> Void
    @State private var revealed = false

    private var isToggle: Bool { field.fieldType == .toggle }
    private var masked: Bool { field.isMasked && !revealed }

    private var displayValue: String {
        if isToggle { return (field.value.lowercased() == "true" || field.value == "1") ? "Yes" : "No" }
        return masked ? String(repeating: "•", count: max(6, min(field.value.count, 12))) : field.value
    }

    var body: some View {
        HStack {
            VStack(alignment: .leading, spacing: 2) {
                Text(field.name.isEmpty ? "Field" : field.name)
                    .font(.caption).foregroundStyle(.secondary)
                Text(displayValue)
                    .font(field.isMasked ? .system(.body, design: .monospaced) : .body)
                    .textSelection(.enabled)
                    .lineLimit(field.fieldType.isMultiline ? 6 : 2)
            }
            Spacer()
            if masked {
                Button { revealed = true } label: { Image(systemName: "eye") }.buttonStyle(.borderless)
            } else if !isToggle {
                Button { onCopy(field.value) } label: { Image(systemName: "doc.on.doc") }.buttonStyle(.borderless)
            }
        }
    }
}
