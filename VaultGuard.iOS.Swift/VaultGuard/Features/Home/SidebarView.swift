// SidebarView.swift — the navigation list, mirroring the WPF MainWindow sidebar exactly.
import SwiftUI

struct SidebarView: View {
    @EnvironmentObject var session: Session
    let current: VaultSection
    let onSelectSection: (VaultSection) -> Void
    let onSelectRoute: (HomeRoute) -> Void

    var body: some View {
        NavigationStack {
            List {
                Section {
                    Button { onSelectRoute(.profile) } label: {
                        HStack {
                            Image(systemName: "person.crop.circle").font(.title2)
                            VStack(alignment: .leading) {
                                Text(session.user?.email ?? "My Account").font(.body)
                                Text("Personal Vault").font(.caption).foregroundStyle(.secondary)
                            }
                        }
                    }.buttonStyle(.plain)
                    Button { onSelectRoute(.edit(nil)) } label: {
                        Label("New Item", systemImage: "plus.circle.fill")
                    }
                }

                Section("Library") {
                    row(.allItems); row(.favorites)
                }
                Section("Categories") {
                    ForEach(VaultSection.categories) { row($0) }
                }
                Section("Security") {
                    Button { onSelectRoute(.security) } label: { Label("Security Dashboard", systemImage: "shield.lefthalf.filled") }
                }
                Section("Manage") {
                    Button { onSelectRoute(.categories) } label: { Label("Manage Categories", systemImage: "folder.badge.gearshape") }
                    Button { onSelectRoute(.vaults) } label: { Label("Vaults", systemImage: "lock.rectangle.stack") }
                    Button { onSelectRoute(.importItems) } label: { Label("Import", systemImage: "square.and.arrow.down") }
                }
                Section("More") {
                    row(.archive); row(.recentlyDeleted)
                    Button { onSelectRoute(.settings) } label: { Label("Settings", systemImage: "gearshape") }
                    Button { onSelectRoute(.about) } label: { Label("About", systemImage: "info.circle") }
                }
            }
            .navigationTitle("VaultGuard")
            .navigationBarTitleDisplayMode(.inline)
        }
    }

    @ViewBuilder
    private func row(_ section: VaultSection) -> some View {
        Button { onSelectSection(section) } label: {
            HStack {
                Label(section.title, systemImage: section.systemImage)
                Spacer()
                if current == section { Image(systemName: "checkmark").foregroundStyle(Theme.accent) }
            }
        }
        .buttonStyle(.plain)
    }
}
