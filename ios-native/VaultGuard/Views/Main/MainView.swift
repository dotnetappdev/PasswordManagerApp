import SwiftUI

// MARK: - Sidebar Selection

enum SidebarSelection: Hashable {
    case allItems
    case favorites
    case vault(Int64)
    case category(Int64)
    case tag(Int64)
    case type(ItemType)
    case trash
}

struct MainView: View {
    @EnvironmentObject var appState: AppState
    @State private var columnVisibility: NavigationSplitViewVisibility = .automatic
    @State private var sidebarSelection: SidebarSelection? = .allItems
    @State private var selectedItem: PasswordItem?
    @State private var showAddItem: Bool = false
    @State private var showSettings: Bool = false

    var body: some View {
        NavigationSplitView(columnVisibility: $columnVisibility) {
            SidebarView(selection: $sidebarSelection)
                .navigationSplitViewColumnWidth(min: 220, ideal: 260, max: 300)
        } content: {
            ItemListView(
                filter: sidebarSelection ?? .allItems,
                selectedItem: $selectedItem,
                showAddItem: $showAddItem
            )
            .navigationSplitViewColumnWidth(min: 300, ideal: 360, max: 420)
        } detail: {
            if let item = selectedItem {
                ItemDetailView(item: item, selectedItem: $selectedItem)
            } else {
                EmptyDetailView()
            }
        }
        .sheet(isPresented: $showAddItem) {
            AddItemView(
                defaultFilter: sidebarSelection,
                onSave: { newItem in
                    selectedItem = newItem
                }
            )
        }
        .sheet(isPresented: $showSettings) {
            SettingsView()
        }
        .toolbar {
            ToolbarItemGroup(placement: .navigationBarTrailing) {
                Button {
                    showSettings = true
                } label: {
                    Image(systemName: "gearshape.fill")
                        .foregroundColor(Color(hex: "7C3AED"))
                }
            }
        }
    }
}

// MARK: - Empty Detail View

struct EmptyDetailView: View {
    var body: some View {
        VStack(spacing: 20) {
            ZStack {
                RoundedRectangle(cornerRadius: 24)
                    .fill(Color(hex: "7C3AED").opacity(0.1))
                    .frame(width: 100, height: 100)
                Image(systemName: "lock.shield.fill")
                    .font(.system(size: 48))
                    .foregroundColor(Color(hex: "7C3AED").opacity(0.6))
            }

            VStack(spacing: 8) {
                Text("No Item Selected")
                    .font(.system(size: 20, weight: .semibold))
                    .foregroundColor(.primary)

                Text("Select an item from the list\nor create a new one.")
                    .font(.system(size: 14))
                    .foregroundColor(.secondary)
                    .multilineTextAlignment(.center)
            }
        }
        .frame(maxWidth: .infinity, maxHeight: .infinity)
        .background(Color(UIColor.systemBackground))
    }
}

#Preview {
    MainView()
        .environmentObject(AppState())
}
