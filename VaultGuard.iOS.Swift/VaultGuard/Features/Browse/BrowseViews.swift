// BrowseViews.swift — Vaults, Categories, Security Dashboard, Import, Profile, About.
import SwiftUI

struct VaultsView: View {
    @EnvironmentObject var env: AppEnvironment
    @State private var vaults: [VaultDto] = []
    @State private var loading = true

    var body: some View {
        List(vaults) { v in
            HStack {
                Image(systemName: "lock.rectangle.stack.fill").foregroundStyle(Theme.accent)
                VStack(alignment: .leading) {
                    Text(v.name).font(.body.weight(.semibold))
                    Text("\(v.itemCount) item\(v.itemCount == 1 ? "" : "s")\(v.isDefault ? " · Default" : "")")
                        .font(.caption).foregroundStyle(.secondary)
                }
            }
        }
        .overlay { if loading { ProgressView() } }
        .navigationTitle("Vaults")
        .task { vaults = await env.repository.vaults(); loading = false }
    }
}

struct CategoriesView: View {
    @EnvironmentObject var env: AppEnvironment
    @State private var categories: [CategoryDto] = []
    @State private var loading = true

    var body: some View {
        List(categories) { c in
            Label {
                Text(c.name)
            } icon: {
                Image(systemName: "folder.fill").foregroundStyle(Theme.categoryTint(c.color))
            }
        }
        .overlay { if loading { ProgressView() } }
        .navigationTitle("Manage Categories")
        .task { categories = await env.repository.categories(); loading = false }
    }
}

struct SecurityDashboardView: View {
    @EnvironmentObject var env: AppEnvironment
    @State private var items: [VaultItem] = []

    // Breach monitoring (Have I Been Pwned, k-anonymity) — on-demand since it's a network call.
    @State private var breachScanning = false
    @State private var breachScanned = false
    @State private var breachFailed = false
    @State private var breached: [BreachHit] = []

    private struct BreachHit: Identifiable {
        let id: Int
        let title: String
        let timesSeen: Int
    }

    private var active: [VaultItem] { items.filter { !$0.isDeleted && !$0.isArchived } }

    var body: some View {
        List {
            Section {
                HStack {
                    stat("Items", active.count)
                    stat("Favourites", active.filter { $0.isFavorite }.count)
                }
                HStack {
                    stat("Archived", items.filter { $0.isArchived && !$0.isDeleted }.count)
                    stat("Deleted", items.filter { $0.isDeleted }.count)
                }
            }
            Section("Breach check") {
                Text("Checks every stored password against known data breaches. Privacy-preserving — only the first five characters of each password's hash are ever sent (Have I Been Pwned).")
                    .font(.footnote).foregroundStyle(.secondary)

                if breachScanning {
                    HStack { ProgressView(); Text("Checking…").foregroundStyle(.secondary) }
                } else {
                    Button {
                        Task { await scanBreaches() }
                    } label: {
                        Label(breachScanned ? "Re-check" : "Check for breaches", systemImage: "shield.lefthalf.filled")
                    }
                }

                if breachScanned {
                    if breached.isEmpty {
                        Label("No passwords found in known breaches.", systemImage: "checkmark.shield")
                            .foregroundStyle(.green)
                    } else {
                        ForEach(breached) { hit in
                            HStack {
                                Label(hit.title, systemImage: "exclamationmark.shield.fill")
                                    .foregroundStyle(.red).lineLimit(1)
                                Spacer()
                                Text("seen \(hit.timesSeen)×").font(.caption).foregroundStyle(.red)
                            }
                        }
                    }
                    if breachFailed {
                        Text("Some passwords couldn't be checked (offline?). Try again when connected.")
                            .font(.footnote).foregroundStyle(.secondary)
                    }
                }
            }

            Section("Items by type") {
                let counts = Dictionary(grouping: active, by: { $0.type }).mapValues { $0.count }
                    .sorted { $0.value > $1.value }
                let maxVal = max(counts.map { $0.value }.max() ?? 1, 1)
                if counts.isEmpty { Text("No items yet.").foregroundStyle(.secondary) }
                ForEach(counts, id: \.key) { type, count in
                    barRow(type: type, count: count, maxVal: maxVal)
                }
            }
        }
        .navigationTitle("Security Center")
        .task { items = (try? await env.repository.list()) ?? [] }
    }

    /// Check every stored password against Have I Been Pwned (k-anonymity), deduping by password.
    private func scanBreaches() async {
        breachScanning = true
        breachFailed = false
        var found: [BreachHit] = []
        var cache: [String: Int?] = [:]
        for item in active {
            guard let secret = try? await env.repository.secret(id: item.id),
                  let pwd = secret.password, !pwd.isEmpty else { continue }
            let count: Int?
            if let cached = cache[pwd] { count = cached }
            else { count = await BreachChecker.timesSeen(pwd); cache[pwd] = count }

            if count == nil { breachFailed = true }
            else if let c = count, c > 0 { found.append(BreachHit(id: item.id, title: item.title, timesSeen: c)) }
        }
        breached = found.sorted { $0.timesSeen > $1.timesSeen }
        breachScanned = true
        breachScanning = false
    }

    /// One horizontal bar in the distribution chart — a dependency-free Capsule bar.
    private func barRow(type: ItemType, count: Int, maxVal: Int) -> some View {
        VStack(alignment: .leading, spacing: 6) {
            HStack {
                Label(type.label, systemImage: type.systemImage)
                    .lineLimit(1)
                    .truncationMode(.tail)
                Spacer()
                Text("\(count)").fontWeight(.semibold)
            }
            GeometryReader { geo in
                ZStack(alignment: .leading) {
                    Capsule().fill(Color(.secondarySystemBackground)).frame(height: 8)
                    Capsule().fill(Theme.accent)
                        .frame(width: max(6, geo.size.width * CGFloat(count) / CGFloat(maxVal)), height: 8)
                }
            }
            .frame(height: 8)
        }
        .padding(.vertical, 2)
    }

    private func stat(_ label: String, _ value: Int) -> some View {
        VStack(alignment: .leading) {
            Text("\(value)").font(.largeTitle.bold()).foregroundStyle(Theme.accent)
            Text(label).font(.subheadline).foregroundStyle(.secondary).lineLimit(1)
        }
        .frame(maxWidth: .infinity, alignment: .leading)
    }
}

struct ImportView: View {
    private let providers = ["1Password", "Bitwarden", "LastPass", "Dashlane", "NordPass", "Keeper",
                             "Chrome", "Edge", "Firefox", "Safari", "KeePass", "Enpass", "RoboForm", "Apple Passwords"]
    var body: some View {
        List {
            Section("Connect directly") {
                NavigationLink {
                    OnePasswordImportView()
                } label: {
                    Label {
                        VStack(alignment: .leading) {
                            Text("1Password (Connect API)")
                            Text("Import over the air from a Connect server / Service Account")
                                .font(.caption).foregroundStyle(.secondary)
                        }
                    } icon: {
                        Image(systemName: "icloud.and.arrow.down").foregroundStyle(Theme.accent)
                    }
                }
            }
            Section {
                Text("Or export a CSV/1PUX from the app below, then import it on the VaultGuard desktop or web app to sync here.")
                    .font(.footnote).foregroundStyle(.secondary)
            }
            Section("From an export file") {
                ForEach(providers, id: \.self) { name in
                    Label(name, systemImage: "square.and.arrow.down")
                }
            }
        }
        .navigationTitle("Import")
    }
}

struct ProfileView: View {
    @EnvironmentObject var env: AppEnvironment
    @EnvironmentObject var session: Session

    var body: some View {
        List {
            Section {
                VStack(spacing: 8) {
                    Image(systemName: "person.crop.circle.fill").font(.system(size: 72)).foregroundStyle(Theme.accent)
                    Text(fullName).font(.title2.bold())
                    if let email = session.user?.email { Text(email).foregroundStyle(.secondary) }
                }.frame(maxWidth: .infinity).padding(.vertical)
            }
            Section("Account") {
                LabeledContent("Email", value: session.user?.email ?? "—")
                LabeledContent("Name", value: fullName)
            }
            Section {
                Button(role: .destructive) { session.lock() } label: { Label("Lock vault", systemImage: "lock") }
            }
        }
        .navigationTitle("My Account")
    }

    private var fullName: String {
        [session.user?.firstName, session.user?.lastName].compactMap { $0 }.joined(separator: " ")
            .ifEmptyReplace(session.user?.email ?? "Local user")
    }
}

struct AboutView: View {
    var body: some View {
        List {
            Section {
                VStack(spacing: 8) {
                    Image(systemName: "lock.shield.fill").font(.system(size: 64)).foregroundStyle(Theme.accent)
                    Text("VaultGuard").font(.title.bold())
                    Text("for iOS · Version 1.0.0").foregroundStyle(.secondary)
                }.frame(maxWidth: .infinity).padding(.vertical)
            }
            Section {
                Text("A native, encrypted companion to the VaultGuard desktop (WPF) and web apps.")
                Label("API or on-device SQLite modes", systemImage: "externaldrive.connected.to.line.below")
                Label("AES-256-GCM · PBKDF2 (600k)", systemImage: "lock.fill")
                Label("TOTP, QR scanning, password generator", systemImage: "qrcode")
                Label("Feature parity with the WPF app", systemImage: "checkmark.seal")
            }
        }
        .navigationTitle("About")
    }
}

private extension String {
    func ifEmptyReplace(_ fallback: String) -> String { isEmpty ? fallback : self }
}
