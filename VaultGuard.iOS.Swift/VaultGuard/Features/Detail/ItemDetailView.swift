// ItemDetailView.swift — read a vault item; reveal password + live TOTP on demand.
import SwiftUI

@MainActor
struct ItemDetailView: View {
    @EnvironmentObject var env: AppEnvironment
    @Environment(\.dismiss) private var dismiss
    let itemId: Int
    @Binding var path: [HomeRoute]

    @State private var item: VaultItem?
    @State private var revealed: String?
    @State private var revealing = false
    @State private var totpCode: String?
    @State private var totpRemaining = 0
    @State private var totpSecret: String?
    @State private var error: String?

    private let timer = Timer.publish(every: 1, on: .main, in: .common).autoconnect()

    var body: some View {
        Form {
            if let item {
                if let d = item.description, !d.isEmpty { field("Description", d) }
                if let v = item.username { copyRow("Username", v) }
                if let v = item.email { copyRow("Email", v) }
                if let v = item.website { copyRow("Website", v) }
                if let v = item.loginUrl { copyRow("Login URL", v) }

                Section("Password") {
                    if let revealed {
                        HStack {
                            Text(revealed).font(.system(.body, design: .monospaced)).textSelection(.enabled)
                            Spacer()
                            Button { copy(revealed) } label: { Image(systemName: "doc.on.doc") }
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
        .task { item = await env.repository.get(id: itemId) }
        .onReceive(timer) { _ in
            guard let secret = totpSecret, let code = Totp.generate(base32Secret: secret) else { return }
            totpCode = code.value; totpRemaining = code.secondsRemaining
        }
    }

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
            let secret = try await env.repository.secret(id: itemId)
            revealed = secret.password
            if let t = secret.totpSecret, !t.isEmpty {
                totpSecret = t
                if let code = Totp.generate(base32Secret: t) { totpCode = code.value; totpRemaining = code.secondsRemaining }
            }
        } catch { self.error = error.localizedDescription }
        revealing = false
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
