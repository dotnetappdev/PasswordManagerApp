// HomeView.swift — main screen: WPF-style sidebar (sheet) + grouped, searchable item list.
import SwiftUI

enum HomeRoute: Hashable {
    case detail(Int), edit(Int?), settings, vaults, categories, security, importItems, profile, about
}

@MainActor
struct HomeView: View {
    @EnvironmentObject var env: AppEnvironment
    @EnvironmentObject var session: Session

    @State private var section: VaultSection = .allItems
    @State private var items: [VaultItem] = []
    @State private var vaults: [VaultDto] = []
    @State private var categories: [CategoryDto] = []
    @State private var categoryFilter: String?
    @State private var query = ""
    @State private var loading = true
    @State private var error: String?
    @State private var showSidebar = false
    @State private var showQuickAccess = false
    @State private var path: [HomeRoute] = []

    /// The vault the user is viewing (default/personal), shown as context under the title.
    private var vaultLabel: String { (vaults.first { $0.isDefault } ?? vaults.first)?.name ?? "Personal" }

    var body: some View {
        NavigationStack(path: $path) {
            VStack(spacing: 0) {
                if !categories.isEmpty {
                    CategoryFilterBar(categories: categories, selected: $categoryFilter)
                }
                List {
                    ForEach(groupedKeys, id: \.self) { month in
                        Section(header: Text(month)) {
                            ForEach(grouped[month] ?? []) { item in
                                Button { path.append(.detail(item.id)) } label: {
                                    VaultItemRow(item: item) { toggleFavorite(item.id) }
                                }
                                .buttonStyle(.plain)
                            }
                        }
                    }
                }
                .listStyle(.insetGrouped)
                .overlay { if visible.isEmpty && !loading { ContentUnavailableCompat(title: "Nothing here yet", systemImage: "tray") } }
            }
            .searchable(text: $query, prompt: "Search")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .navigationBarLeading) {
                    Button { showSidebar = true } label: { Image(systemName: "line.3.horizontal") }
                }
                ToolbarItem(placement: .principal) {
                    VStack(spacing: 1) {
                        Text(section.title).font(.headline)
                        Label(vaultLabel, systemImage: "folder")
                            .font(.caption2).foregroundStyle(.secondary).labelStyle(.titleAndIcon)
                    }
                }
                ToolbarItem(placement: .navigationBarTrailing) {
                    Button { showQuickAccess = true } label: { Image(systemName: "text.magnifyingglass") }
                        .accessibilityLabel("Quick Access")
                }
                ToolbarItem(placement: .navigationBarTrailing) {
                    Button { path.append(.edit(nil)) } label: { Image(systemName: "plus") }
                }
                // Bottom bar — quick Favourites toggle + a prominent, high-emphasis New Item action.
                ToolbarItemGroup(placement: .bottomBar) {
                    Button {
                        section = section == .favorites ? .allItems : .favorites
                    } label: {
                        Label("Favourites",
                              systemImage: section == .favorites ? "star.fill" : "star")
                    }
                    .tint(section == .favorites ? .yellow : Theme.accent)
                    Spacer()
                    Button { path.append(.edit(nil)) } label: {
                        Label("New Item", systemImage: "square.and.pencil")
                            .font(.headline)
                            .padding(.horizontal, 14).padding(.vertical, 8)
                            .background(Theme.accent, in: Capsule())
                            .foregroundStyle(.white)
                    }
                    .buttonStyle(.plain)
                }
            }
            .navigationDestination(for: HomeRoute.self) { route in
                switch route {
                case .detail(let id): ItemDetailView(itemId: id, path: $path)
                case .edit(let id): ItemEditView(itemId: id) { Task { await load() } }
                case .settings: SettingsView()
                case .vaults: VaultsView()
                case .categories: CategoriesView()
                case .security: SecurityDashboardView()
                case .importItems: ImportView()
                case .profile: ProfileView()
                case .about: AboutView()
                }
            }
            .task { await load() }
            .refreshable { await load() }
            .sheet(isPresented: $showSidebar) {
                SidebarView(current: section,
                            onSelectSection: { section = $0; showSidebar = false },
                            onSelectRoute: { route in showSidebar = false; path.append(route) })
                    .presentationDetents([.large])
            }
            .sheet(isPresented: $showQuickAccess) {
                QuickAccessView(
                    items: items,
                    onOpen: { id in path.append(.detail(id)) },
                    onToggleFavorite: toggleFavorite
                )
            }
        }
    }

    // MARK: - Data

    private var visible: [VaultItem] {
        items.filter { section.matches($0) }
            .filter { categoryFilter == nil || $0.categoryName == categoryFilter }
            .filter {
                query.isEmpty ||
                $0.title.localizedCaseInsensitiveContains(query) ||
                ($0.username?.localizedCaseInsensitiveContains(query) ?? false) ||
                ($0.website?.localizedCaseInsensitiveContains(query) ?? false)
            }
    }

    private static let monthFormat: DateFormatter = {
        let f = DateFormatter()
        f.dateFormat = "MMMM yyyy"
        return f
    }()

    private func monthKey(_ item: VaultItem) -> String {
        item.createdAt > 0
            ? Self.monthFormat.string(from: Date(timeIntervalSince1970: item.createdAt)).uppercased()
            : "UNDATED"
    }

    /// Grouped by month added (newest first) — matches the 1Password desktop item list.
    private var grouped: [String: [VaultItem]] {
        Dictionary(grouping: visible.sorted { $0.createdAt > $1.createdAt }, by: monthKey)
    }

    private var groupedKeys: [String] {
        var seen: [String] = []
        for item in visible.sorted(by: { $0.createdAt > $1.createdAt }) {
            let key = monthKey(item)
            if !seen.contains(key) { seen.append(key) }
        }
        return seen
    }

    func count(_ s: VaultSection) -> Int { items.filter { s.matches($0) }.count }

    private func load() async {
        loading = true; error = nil
        env.repository.seedLocalDemoIfEmpty()
        do { items = try await env.repository.list() }
        catch { self.error = error.localizedDescription }
        vaults = await env.repository.vaults()
        categories = await env.repository.categories()
        loading = false
        // Keep the AutoFill QuickType credentials in sync with the vault.
        Task { await env.repository.syncAutoFill() }
    }

    private func toggleFavorite(_ id: Int) {
        Task { try? await env.repository.toggleFavorite(id: id); await load() }
    }
}

/// "All Categories" filter dropdown shown above the item list — mirrors the desktop app's category picker.
struct CategoryFilterBar: View {
    let categories: [CategoryDto]
    @Binding var selected: String?

    var body: some View {
        Menu {
            Button {
                selected = nil
            } label: {
                if selected == nil { Label("All Categories", systemImage: "checkmark") }
                else { Text("All Categories") }
            }
            ForEach(categories) { cat in
                Button {
                    selected = cat.name
                } label: {
                    if selected == cat.name { Label(cat.name, systemImage: "checkmark") }
                    else { Text(cat.name) }
                }
            }
        } label: {
            HStack {
                Image(systemName: "square.grid.2x2")
                Text(selected ?? "All Categories")
                Spacer()
                Image(systemName: "chevron.up.chevron.down").font(.caption)
            }
            .foregroundStyle(.primary)
            .padding(.horizontal, 12)
            .padding(.vertical, 8)
            .background(Color(.secondarySystemBackground))
            .clipShape(RoundedRectangle(cornerRadius: 10))
            .padding(.horizontal)
            .padding(.top, 8)
        }
        .buttonStyle(.plain)
    }
}

/// Backwards-compatible empty state (ContentUnavailableView is iOS 17+).
struct ContentUnavailableCompat: View {
    let title: String
    let systemImage: String
    var body: some View {
        VStack(spacing: 10) {
            Image(systemName: systemImage).font(.system(size: 44)).foregroundStyle(.secondary)
            Text(title).foregroundStyle(.secondary)
        }
    }
}
