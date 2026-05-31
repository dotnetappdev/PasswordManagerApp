import Foundation
import GRDB

class VaultService: ObservableObject {
    static let shared = VaultService()
    private init() {}

    private var db: DatabaseService { DatabaseService.shared }

    // MARK: - Fetch

    func fetchAll() throws -> [Vault] {
        try db.read { database in
            try Vault.order(Vault.Columns.isDefault.desc, Vault.Columns.name.asc).fetchAll(database)
        }
    }

    func fetchDefault() throws -> Vault? {
        try db.read { database in
            try Vault.filter(Vault.Columns.isDefault == true).fetchOne(database)
        }
    }

    func fetchById(_ id: Int64) throws -> Vault? {
        try db.read { database in
            try Vault.fetchOne(database, key: id)
        }
    }

    // MARK: - Create

    func create(_ vault: inout Vault) throws {
        vault.createdAt = .now
        vault.updatedAt = .now
        vault.syncId = UUID().uuidString
        try db.write { database in
            try vault.insert(database)
        }
    }

    // MARK: - Update

    func update(_ vault: inout Vault) throws {
        vault.updatedAt = .now
        try db.write { database in
            try vault.update(database)
        }
    }

    // MARK: - Delete

    func delete(_ vault: Vault) throws {
        guard !vault.isDefault else {
            throw VaultError.cannotDeleteDefault
        }
        try db.write { database in
            try vault.delete(database)
        }
    }

    // MARK: - Item Count

    func itemCount(for vault: Vault) throws -> Int {
        guard let id = vault.id else { return 0 }
        return try db.read { database in
            try PasswordItem
                .filter(PasswordItem.Columns.vaultId == id)
                .filter(PasswordItem.Columns.isDeleted == false)
                .fetchCount(database)
        }
    }
}

// MARK: - Category Service

class CategoryService: ObservableObject {
    static let shared = CategoryService()
    private init() {}

    private var db: DatabaseService { DatabaseService.shared }

    func fetchAll() throws -> [Category] {
        try db.read { database in
            try Category.order(Category.Columns.name.asc).fetchAll(database)
        }
    }

    func fetchById(_ id: Int64) throws -> Category? {
        try db.read { database in
            try Category.fetchOne(database, key: id)
        }
    }

    func create(_ category: inout Category) throws {
        category.createdAt = .now
        category.updatedAt = .now
        category.syncId = UUID().uuidString
        try db.write { database in
            try category.insert(database)
        }
    }

    func update(_ category: inout Category) throws {
        category.updatedAt = .now
        try db.write { database in
            try category.update(database)
        }
    }

    func delete(_ category: Category) throws {
        try db.write { database in
            try category.delete(database)
        }
    }

    func itemCount(for category: Category) throws -> Int {
        guard let id = category.id else { return 0 }
        return try db.read { database in
            try PasswordItem
                .filter(PasswordItem.Columns.categoryId == id)
                .filter(PasswordItem.Columns.isDeleted == false)
                .fetchCount(database)
        }
    }
}

// MARK: - Tag Service

class TagService: ObservableObject {
    static let shared = TagService()
    private init() {}

    private var db: DatabaseService { DatabaseService.shared }

    func fetchAll() throws -> [Tag] {
        try db.read { database in
            try Tag.order(Tag.Columns.name.asc).fetchAll(database)
        }
    }

    func create(_ tag: inout Tag) throws {
        tag.createdAt = .now
        tag.syncId = UUID().uuidString
        try db.write { database in
            try tag.insert(database)
        }
    }

    func delete(_ tag: Tag) throws {
        try db.write { database in
            try tag.delete(database)
        }
    }
}

// MARK: - Errors

enum VaultError: LocalizedError {
    case cannotDeleteDefault

    var errorDescription: String? {
        switch self {
        case .cannotDeleteDefault:
            return "Cannot delete the default vault."
        }
    }
}
