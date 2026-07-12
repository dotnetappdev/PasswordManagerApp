// QuickAccessView.swift — 1Password-style "Quick Access" popup: category filter, search,
// and matching items grouped by the month they were added (newest month first).
import SwiftUI

struct QuickAccessView: View {
    @EnvironmentObject var env: AppEnvironment
    @Environment(\.dismiss) private var dismiss

    let items: [VaultItem]
    var onOpen: (Int) -> Void
    var onToggleFavorite: (Int) -> Void

    @State private var query = ""
    @State private var categories: [CategoryDto] = []
    @State private var categoryFilter: String?

    var body: some View {
        NavigationStack {
            List {
                ForEach(groupedKeys, id: \.self) { month in
                    Section(header: Text(month)) {
                        ForEach(grouped[month] ?? []) { item in
                            Button {
                                onOpen(item.id)
                                dismiss()
                            } label: {
                                VaultItemRow(item: item) { onToggleFavorite(item.id) }
                            }
                            .buttonStyle(.plain)
                        }
                    }
                }
            }
            .listStyle(.insetGrouped)
            .overlay {
                if filtered.isEmpty {
                    ContentUnavailableCompat(
                        title: query.isEmpty && categoryFilter == nil ? "Search your vault" : "No matches",
                        systemImage: "magnifyingglass"
                    )
                }
            }
            .searchable(text: $query, prompt: "Search")
            .navigationTitle("Quick Access")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .navigationBarLeading) {
                    Menu {
                        Button {
                            categoryFilter = nil
                        } label: {
                            if categoryFilter == nil { Label("All Categories", systemImage: "checkmark") }
                            else { Text("All Categories") }
                        }
                        ForEach(categories) { cat in
                            Button {
                                categoryFilter = cat.name
                            } label: {
                                if categoryFilter == cat.name { Label(cat.name, systemImage: "checkmark") }
                                else { Text(cat.name) }
                            }
                        }
                    } label: {
                        Label(categoryFilter ?? "All Categories", systemImage: "line.3.horizontal.decrease.circle")
                    }
                }
                ToolbarItem(placement: .navigationBarTrailing) {
                    Button("Close") { dismiss() }
                }
            }
            .task { categories = await env.repository.categories() }
        }
    }

    // MARK: - Data

    private var filtered: [VaultItem] {
        items.filter { !$0.isArchived && !$0.isDeleted }
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

    private var grouped: [String: [VaultItem]] {
        Dictionary(grouping: filtered.sorted { $0.createdAt > $1.createdAt }, by: monthKey)
    }

    /// Newest-month-first order (not alphabetical), matching the desktop quick-access popup.
    private var groupedKeys: [String] {
        var seen: [String] = []
        for item in filtered.sorted(by: { $0.createdAt > $1.createdAt }) {
            let key = monthKey(item)
            if !seen.contains(key) { seen.append(key) }
        }
        return seen
    }
}
