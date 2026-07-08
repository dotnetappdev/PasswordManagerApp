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
    @State private var query = ""
    @State private var loading = true
    @State private var error: String?
    @State private var showSidebar = false
    @State private var path: [HomeRoute] = []

    var body: some View {
        NavigationStack(path: $path) {
            List {
                ForEach(groupedKeys, id: \.self) { letter in
                    Section(header: Text(letter)) {
                        ForEach(grouped[letter] ?? []) { item in
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
            .searchable(text: $query, prompt: "Search")
            .navigationTitle(section.title)
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .navigationBarLeading) {
                    Button { showSidebar = true } label: { Image(systemName: "line.3.horizontal") }
                }
                ToolbarItem(placement: .navigationBarTrailing) {
                    Button { path.append(.edit(nil)) } label: { Image(systemName: "plus") }
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
        }
    }

    // MARK: - Data

    private var visible: [VaultItem] {
        items.filter { section.matches($0) }.filter {
            query.isEmpty ||
            $0.title.localizedCaseInsensitiveContains(query) ||
            ($0.username?.localizedCaseInsensitiveContains(query) ?? false) ||
            ($0.website?.localizedCaseInsensitiveContains(query) ?? false)
        }
    }

    private var grouped: [String: [VaultItem]] {
        Dictionary(grouping: visible.sorted { $0.title.lowercased() < $1.title.lowercased() }) {
            let c = $0.title.first.map { String($0).uppercased() } ?? "#"
            return c.first?.isLetter == true ? c : "#"
        }
    }
    private var groupedKeys: [String] { grouped.keys.sorted() }

    func count(_ s: VaultSection) -> Int { items.filter { s.matches($0) }.count }

    private func load() async {
        loading = true; error = nil
        env.repository.seedLocalDemoIfEmpty()
        do { items = try await env.repository.list() }
        catch { self.error = error.localizedDescription }
        loading = false
    }

    private func toggleFavorite(_ id: Int) {
        Task { try? await env.repository.toggleFavorite(id: id); await load() }
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
