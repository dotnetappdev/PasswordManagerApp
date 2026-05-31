import SwiftUI

struct ItemListView: View {
    let filter: SidebarSelection
    @Binding var selectedItem: PasswordItem?
    @Binding var showAddItem: Bool

    @State private var items: [PasswordItem] = []
    @State private var searchText: String = ""
    @State private var isLoading: Bool = false
    @State private var sortOrder: SortOrder = .updatedDesc
    @State private var showSortMenu: Bool = false

    enum SortOrder: String, CaseIterable {
        case titleAsc = "Title (A–Z)"
        case titleDesc = "Title (Z–A)"
        case updatedDesc = "Recently Updated"
        case updatedAsc = "Oldest First"
        case createdDesc = "Recently Created"
    }

    private var filteredItems: [PasswordItem] {
        var result = items
        if !searchText.isEmpty {
            result = result.filter {
                $0.title.localizedCaseInsensitiveContains(searchText) ||
                ($0.username?.localizedCaseInsensitiveContains(searchText) ?? false) ||
                ($0.website?.localizedCaseInsensitiveContains(searchText) ?? false) ||
                ($0.notes?.localizedCaseInsensitiveContains(searchText) ?? false)
            }
        }
        return sorted(result)
    }

    private var groupedItems: [String: [PasswordItem]] {
        Dictionary(grouping: filteredItems) { item in
            String(item.title.prefix(1).uppercased())
        }
    }

    private var sectionKeys: [String] {
        groupedItems.keys.sorted()
    }

    var navigationTitle: String {
        switch filter {
        case .allItems: return "All Items"
        case .favorites: return "Favorites"
        case .trash: return "Trash"
        case .vault: return "Vault"
        case .category: return "Category"
        case .tag: return "Tag"
        case .type(let t): return t.displayName
        }
    }

    var body: some View {
        Group {
            if isLoading {
                ProgressView()
                    .frame(maxWidth: .infinity, maxHeight: .infinity)
            } else if filteredItems.isEmpty {
                emptyState
            } else {
                itemList
            }
        }
        .navigationTitle(navigationTitle)
        .navigationBarTitleDisplayMode(.large)
        .searchable(text: $searchText, placement: .navigationBarDrawer(displayMode: .always), prompt: "Search items...")
        .toolbar {
            ToolbarItemGroup(placement: .navigationBarTrailing) {
                // Sort menu
                Menu {
                    ForEach(SortOrder.allCases, id: \.self) { order in
                        Button {
                            sortOrder = order
                        } label: {
                            HStack {
                                Text(order.rawValue)
                                if sortOrder == order {
                                    Image(systemName: "checkmark")
                                }
                            }
                        }
                    }
                } label: {
                    Image(systemName: "arrow.up.arrow.down")
                        .foregroundColor(Color(hex: "7C3AED"))
                }

                // Add button
                if filter != .trash {
                    Button {
                        showAddItem = true
                    } label: {
                        Image(systemName: "plus")
                            .foregroundColor(Color(hex: "7C3AED"))
                            .fontWeight(.semibold)
                    }
                }
            }
        }
        .onAppear { loadItems() }
        .onChange(of: filter) { _, _ in loadItems() }
        .refreshable { loadItems() }
    }

    // MARK: - Item List

    private var itemList: some View {
        List(selection: $selectedItem) {
            ForEach(sectionKeys, id: \.self) { key in
                Section(header: Text(key).font(.system(size: 13, weight: .semibold))) {
                    ForEach(groupedItems[key] ?? []) { item in
                        ItemRowView(item: item)
                            .tag(item)
                            .swipeActions(edge: .trailing, allowsFullSwipe: false) {
                                if filter == .trash {
                                    Button(role: .destructive) {
                                        hardDelete(item)
                                    } label: {
                                        Label("Delete", systemImage: "trash.fill")
                                    }

                                    Button {
                                        restore(item)
                                    } label: {
                                        Label("Restore", systemImage: "arrow.uturn.backward")
                                    }
                                    .tint(.blue)
                                } else {
                                    Button(role: .destructive) {
                                        softDelete(item)
                                    } label: {
                                        Label("Delete", systemImage: "trash.fill")
                                    }
                                }
                            }
                            .swipeActions(edge: .leading, allowsFullSwipe: true) {
                                Button {
                                    toggleFavorite(item)
                                } label: {
                                    Label(
                                        item.isFavorite ? "Unfavorite" : "Favorite",
                                        systemImage: item.isFavorite ? "star.slash.fill" : "star.fill"
                                    )
                                }
                                .tint(.yellow)
                            }
                            .contextMenu {
                                itemContextMenu(item)
                            }
                    }
                }
            }
        }
        .listStyle(.insetGrouped)
    }

    // MARK: - Empty State

    private var emptyState: some View {
        VStack(spacing: 20) {
            ZStack {
                Circle()
                    .fill(Color(hex: "7C3AED").opacity(0.08))
                    .frame(width: 100, height: 100)
                Image(systemName: searchText.isEmpty ? emptyIcon : "magnifyingglass")
                    .font(.system(size: 44))
                    .foregroundColor(Color(hex: "7C3AED").opacity(0.5))
            }

            VStack(spacing: 8) {
                Text(searchText.isEmpty ? "No Items" : "No Results")
                    .font(.system(size: 20, weight: .semibold))

                Text(searchText.isEmpty ? emptyMessage : "Try a different search term.")
                    .font(.system(size: 14))
                    .foregroundColor(.secondary)
                    .multilineTextAlignment(.center)
            }

            if searchText.isEmpty && filter != .trash {
                Button {
                    showAddItem = true
                } label: {
                    Label("Add Item", systemImage: "plus.circle.fill")
                        .font(.system(size: 15, weight: .semibold))
                        .foregroundColor(.white)
                        .padding(.horizontal, 24)
                        .padding(.vertical, 12)
                        .background(Color(hex: "7C3AED"))
                        .cornerRadius(12)
                }
            }
        }
        .frame(maxWidth: .infinity, maxHeight: .infinity)
        .padding()
    }

    private var emptyIcon: String {
        switch filter {
        case .favorites: return "star.fill"
        case .trash: return "trash.fill"
        case .type(let t): return t.systemImage
        default: return "lock.fill"
        }
    }

    private var emptyMessage: String {
        switch filter {
        case .favorites: return "Tap the star on any item to add it to Favorites."
        case .trash: return "Deleted items will appear here."
        default: return "Add your first item to get started."
        }
    }

    // MARK: - Context Menu

    @ViewBuilder
    private func itemContextMenu(_ item: PasswordItem) -> some View {
        Button {
            if let password = item.decryptedPassword {
                UIPasteboard.general.string = password
            }
        } label: {
            Label("Copy Password", systemImage: "doc.on.doc")
        }

        Button {
            if let username = item.username {
                UIPasteboard.general.string = username
            }
        } label: {
            Label("Copy Username", systemImage: "person.fill")
        }

        if let website = item.website, let url = URL(string: website) {
            Button {
                UIApplication.shared.open(url)
            } label: {
                Label("Open Website", systemImage: "safari.fill")
            }
        }

        Divider()

        Button {
            toggleFavorite(item)
        } label: {
            Label(
                item.isFavorite ? "Remove from Favorites" : "Add to Favorites",
                systemImage: item.isFavorite ? "star.slash.fill" : "star.fill"
            )
        }

        Divider()

        Button(role: .destructive) {
            softDelete(item)
        } label: {
            Label("Delete", systemImage: "trash.fill")
        }
    }

    // MARK: - Data Loading

    private func loadItems() {
        isLoading = true
        do {
            switch filter {
            case .allItems:
                items = try PasswordItemService.shared.fetchAll()
            case .favorites:
                items = try PasswordItemService.shared.fetchFavorites()
            case .trash:
                items = try PasswordItemService.shared.fetchAll(includeDeleted: true).filter { $0.isDeleted }
            case .vault(let id):
                items = try PasswordItemService.shared.fetchByVault(id)
            case .category(let id):
                items = try PasswordItemService.shared.fetchByCategory(id)
            case .tag(let id):
                let all = try PasswordItemService.shared.fetchAll()
                items = all.filter { item in
                    guard let tagJson = item.tagIds,
                          let data = tagJson.data(using: .utf8),
                          let tagIds = try? JSONDecoder().decode([Int64].self, from: data) else {
                        return false
                    }
                    return tagIds.contains(id)
                }
            case .type(let type):
                items = try PasswordItemService.shared.fetchByType(type)
            }
        } catch {
            print("ItemListView load error: \(error)")
            items = []
        }
        isLoading = false
    }

    // MARK: - Sorting

    private func sorted(_ items: [PasswordItem]) -> [PasswordItem] {
        switch sortOrder {
        case .titleAsc:
            return items.sorted { $0.title < $1.title }
        case .titleDesc:
            return items.sorted { $0.title > $1.title }
        case .updatedDesc:
            return items.sorted { $0.updatedAt > $1.updatedAt }
        case .updatedAsc:
            return items.sorted { $0.updatedAt < $1.updatedAt }
        case .createdDesc:
            return items.sorted { $0.createdAt > $1.createdAt }
        }
    }

    // MARK: - Actions

    private func toggleFavorite(_ item: PasswordItem) {
        try? PasswordItemService.shared.toggleFavorite(item)
        loadItems()
    }

    private func softDelete(_ item: PasswordItem) {
        try? PasswordItemService.shared.softDelete(item)
        if selectedItem?.id == item.id { selectedItem = nil }
        loadItems()
    }

    private func hardDelete(_ item: PasswordItem) {
        try? PasswordItemService.shared.hardDelete(item)
        if selectedItem?.id == item.id { selectedItem = nil }
        loadItems()
    }

    private func restore(_ item: PasswordItem) {
        try? PasswordItemService.shared.restore(item)
        loadItems()
    }
}
