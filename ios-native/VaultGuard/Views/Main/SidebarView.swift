import SwiftUI

struct SidebarView: View {
    @Binding var selection: SidebarSelection?
    @State private var vaults: [Vault] = []
    @State private var categories: [Category] = []
    @State private var tags: [Tag] = []
    @State private var itemCounts: [SidebarSelection: Int] = [:]
    @State private var showAddVault: Bool = false
    @State private var showAddCategory: Bool = false

    var body: some View {
        List(selection: $selection) {
            // MARK: - Core Sections

            Section {
                sidebarRow(
                    icon: "square.grid.2x2.fill",
                    label: "All Items",
                    color: Color(hex: "7C3AED"),
                    selection: .allItems,
                    count: itemCounts[.allItems]
                )

                sidebarRow(
                    icon: "star.fill",
                    label: "Favorites",
                    color: .yellow,
                    selection: .favorites,
                    count: itemCounts[.favorites]
                )

                sidebarRow(
                    icon: "trash.fill",
                    label: "Trash",
                    color: .gray,
                    selection: .trash,
                    count: itemCounts[.trash]
                )
            }

            // MARK: - Item Types

            Section("Types") {
                ForEach(ItemType.allCases, id: \.self) { type in
                    sidebarRow(
                        icon: type.systemImage,
                        label: type.displayName,
                        color: type.color,
                        selection: .type(type),
                        count: itemCounts[.type(type)]
                    )
                }
            }

            // MARK: - Vaults

            Section {
                ForEach(vaults) { vault in
                    if let id = vault.id {
                        sidebarRow(
                            icon: vault.iconName,
                            label: vault.name,
                            color: Color(hex: vault.colorHex),
                            selection: .vault(id),
                            count: itemCounts[.vault(id)]
                        )
                    }
                }

                Button {
                    showAddVault = true
                } label: {
                    Label("Add Vault", systemImage: "plus.circle")
                        .font(.system(size: 14))
                        .foregroundColor(Color(hex: "7C3AED"))
                }
                .listRowBackground(Color.clear)
            } header: {
                Text("Vaults")
            }

            // MARK: - Categories

            Section {
                ForEach(categories) { category in
                    if let id = category.id {
                        sidebarRow(
                            icon: category.iconName,
                            label: category.name,
                            color: category.color,
                            selection: .category(id),
                            count: itemCounts[.category(id)]
                        )
                    }
                }

                Button {
                    showAddCategory = true
                } label: {
                    Label("Add Category", systemImage: "plus.circle")
                        .font(.system(size: 14))
                        .foregroundColor(Color(hex: "7C3AED"))
                }
                .listRowBackground(Color.clear)
            } header: {
                Text("Categories")
            }

            // MARK: - Tags

            if !tags.isEmpty {
                Section("Tags") {
                    ForEach(tags) { tag in
                        if let id = tag.id {
                            sidebarRow(
                                icon: "tag.fill",
                                label: tag.name,
                                color: tag.color,
                                selection: .tag(id),
                                count: nil
                            )
                        }
                    }
                }
            }
        }
        .listStyle(.sidebar)
        .navigationTitle("VaultGuard")
        .navigationBarTitleDisplayMode(.large)
        .onAppear {
            loadData()
        }
        .refreshable {
            loadData()
        }
        .sheet(isPresented: $showAddVault) {
            AddVaultView(onSave: { loadData() })
        }
        .sheet(isPresented: $showAddCategory) {
            AddCategoryView(onSave: { loadData() })
        }
    }

    // MARK: - Row Builder

    @ViewBuilder
    private func sidebarRow(
        icon: String,
        label: String,
        color: Color,
        selection: SidebarSelection,
        count: Int?
    ) -> some View {
        Label {
            HStack {
                Text(label)
                    .font(.system(size: 15))
                Spacer()
                if let c = count, c > 0 {
                    Text("\(c)")
                        .font(.system(size: 12, weight: .medium))
                        .foregroundColor(.secondary)
                }
            }
        } icon: {
            ZStack {
                RoundedRectangle(cornerRadius: 7)
                    .fill(color.opacity(0.15))
                    .frame(width: 28, height: 28)
                Image(systemName: icon)
                    .font(.system(size: 14))
                    .foregroundColor(color)
            }
        }
        .tag(selection)
    }

    // MARK: - Data Loading

    private func loadData() {
        do {
            vaults = try VaultService.shared.fetchAll()
            categories = try CategoryService.shared.fetchAll()
            tags = try TagService.shared.fetchAll()
            loadCounts()
        } catch {
            print("SidebarView load error: \(error)")
        }
    }

    private func loadCounts() {
        do {
            let allItems = try PasswordItemService.shared.fetchAll()
            let favorites = try PasswordItemService.shared.fetchFavorites()
            let deleted = try PasswordItemService.shared.fetchAll(includeDeleted: true).filter { $0.isDeleted }

            itemCounts[.allItems] = allItems.count
            itemCounts[.favorites] = favorites.count
            itemCounts[.trash] = deleted.count

            for type in ItemType.allCases {
                itemCounts[.type(type)] = allItems.filter { $0.type == type }.count
            }

            for vault in vaults {
                if let id = vault.id {
                    itemCounts[.vault(id)] = allItems.filter { $0.vaultId == id }.count
                }
            }

            for category in categories {
                if let id = category.id {
                    itemCounts[.category(id)] = allItems.filter { $0.categoryId == id }.count
                }
            }
        } catch {
            print("Count load error: \(error)")
        }
    }
}

// MARK: - Add Vault Sheet

struct AddVaultView: View {
    @Environment(\.dismiss) var dismiss
    var onSave: () -> Void

    @State private var name: String = ""
    @State private var selectedColor: String = "7C3AED"
    @State private var selectedIcon: String = "lock.shield.fill"

    let colors = ["7C3AED", "EC4899", "3B82F6", "10B981", "F59E0B", "EF4444", "06B6D4"]
    let icons = ["lock.shield.fill", "person.fill", "briefcase.fill", "house.fill", "heart.fill", "star.fill", "globe"]

    var body: some View {
        NavigationView {
            Form {
                Section("Vault Name") {
                    TextField("e.g. Work, Family, Gaming", text: $name)
                }

                Section("Color") {
                    LazyVGrid(columns: Array(repeating: GridItem(.fixed(44)), count: 7), spacing: 12) {
                        ForEach(colors, id: \.self) { hex in
                            Circle()
                                .fill(Color(hex: hex))
                                .frame(width: 36, height: 36)
                                .overlay(
                                    Circle()
                                        .stroke(.white, lineWidth: selectedColor == hex ? 3 : 0)
                                )
                                .onTapGesture { selectedColor = hex }
                        }
                    }
                    .padding(.vertical, 4)
                }

                Section("Icon") {
                    LazyVGrid(columns: Array(repeating: GridItem(.fixed(48)), count: 7), spacing: 12) {
                        ForEach(icons, id: \.self) { icon in
                            ZStack {
                                RoundedRectangle(cornerRadius: 10)
                                    .fill(Color(hex: selectedColor).opacity(selectedIcon == icon ? 0.2 : 0.08))
                                    .frame(width: 40, height: 40)
                                Image(systemName: icon)
                                    .font(.system(size: 18))
                                    .foregroundColor(Color(hex: selectedColor))
                            }
                            .onTapGesture { selectedIcon = icon }
                        }
                    }
                    .padding(.vertical, 4)
                }
            }
            .navigationTitle("New Vault")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Cancel") { dismiss() }
                }
                ToolbarItem(placement: .confirmationAction) {
                    Button("Save") {
                        saveVault()
                    }
                    .disabled(name.isEmpty)
                    .fontWeight(.semibold)
                }
            }
        }
    }

    private func saveVault() {
        var vault = Vault(name: name, iconName: selectedIcon, colorHex: selectedColor)
        try? VaultService.shared.create(&vault)
        onSave()
        dismiss()
    }
}

// MARK: - Add Category Sheet

struct AddCategoryView: View {
    @Environment(\.dismiss) var dismiss
    var onSave: () -> Void

    @State private var name: String = ""
    @State private var selectedColor: String = "7C3AED"
    @State private var selectedIcon: String = "folder.fill"

    let colors = ["7C3AED", "EC4899", "3B82F6", "10B981", "F59E0B", "EF4444", "06B6D4"]
    let icons = ["folder.fill", "person.2.fill", "dollarsign.circle.fill", "bag.fill",
                 "play.circle.fill", "briefcase.fill", "heart.fill", "airplane", "gamecontroller.fill"]

    var body: some View {
        NavigationView {
            Form {
                Section("Category Name") {
                    TextField("e.g. Social, Finance, Shopping", text: $name)
                }

                Section("Color") {
                    LazyVGrid(columns: Array(repeating: GridItem(.fixed(44)), count: 7), spacing: 12) {
                        ForEach(colors, id: \.self) { hex in
                            Circle()
                                .fill(Color(hex: hex))
                                .frame(width: 36, height: 36)
                                .overlay(
                                    Circle()
                                        .stroke(.white, lineWidth: selectedColor == hex ? 3 : 0)
                                )
                                .onTapGesture { selectedColor = hex }
                        }
                    }
                    .padding(.vertical, 4)
                }

                Section("Icon") {
                    LazyVGrid(columns: Array(repeating: GridItem(.fixed(48)), count: 6), spacing: 12) {
                        ForEach(icons, id: \.self) { icon in
                            ZStack {
                                RoundedRectangle(cornerRadius: 10)
                                    .fill(Color(hex: selectedColor).opacity(selectedIcon == icon ? 0.2 : 0.08))
                                    .frame(width: 40, height: 40)
                                Image(systemName: icon)
                                    .font(.system(size: 18))
                                    .foregroundColor(Color(hex: selectedColor))
                            }
                            .onTapGesture { selectedIcon = icon }
                        }
                    }
                    .padding(.vertical, 4)
                }
            }
            .navigationTitle("New Category")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Cancel") { dismiss() }
                }
                ToolbarItem(placement: .confirmationAction) {
                    Button("Save") {
                        saveCategory()
                    }
                    .disabled(name.isEmpty)
                    .fontWeight(.semibold)
                }
            }
        }
    }

    private func saveCategory() {
        var category = Category(name: name, iconName: selectedIcon, colorHex: selectedColor)
        try? CategoryService.shared.create(&category)
        onSave()
        dismiss()
    }
}
