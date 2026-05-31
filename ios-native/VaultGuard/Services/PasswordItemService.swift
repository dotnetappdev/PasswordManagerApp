import Foundation
import GRDB

class PasswordItemService: ObservableObject {
    static let shared = PasswordItemService()
    private init() {}

    private var db: DatabaseService { DatabaseService.shared }

    // MARK: - Fetch

    func fetchAll(includeDeleted: Bool = false) throws -> [PasswordItem] {
        try db.read { database in
            var request = PasswordItem.all()
            if !includeDeleted {
                request = request.filter(PasswordItem.Columns.isDeleted == false)
            }
            return try request.order(PasswordItem.Columns.updatedAt.desc).fetchAll(database)
        }
    }

    func fetchFavorites() throws -> [PasswordItem] {
        try db.read { database in
            try PasswordItem
                .filter(PasswordItem.Columns.isFavorite == true)
                .filter(PasswordItem.Columns.isDeleted == false)
                .order(PasswordItem.Columns.updatedAt.desc)
                .fetchAll(database)
        }
    }

    func fetchByVault(_ vaultId: Int64) throws -> [PasswordItem] {
        try db.read { database in
            try PasswordItem
                .filter(PasswordItem.Columns.vaultId == vaultId)
                .filter(PasswordItem.Columns.isDeleted == false)
                .order(PasswordItem.Columns.updatedAt.desc)
                .fetchAll(database)
        }
    }

    func fetchByCategory(_ categoryId: Int64) throws -> [PasswordItem] {
        try db.read { database in
            try PasswordItem
                .filter(PasswordItem.Columns.categoryId == categoryId)
                .filter(PasswordItem.Columns.isDeleted == false)
                .order(PasswordItem.Columns.updatedAt.desc)
                .fetchAll(database)
        }
    }

    func fetchByType(_ type: ItemType) throws -> [PasswordItem] {
        try db.read { database in
            try PasswordItem
                .filter(PasswordItem.Columns.type == type.rawValue)
                .filter(PasswordItem.Columns.isDeleted == false)
                .order(PasswordItem.Columns.updatedAt.desc)
                .fetchAll(database)
        }
    }

    func search(_ query: String) throws -> [PasswordItem] {
        guard !query.isEmpty else { return try fetchAll() }

        let pattern = "%\(query)%"
        return try db.read { database in
            try PasswordItem
                .filter(PasswordItem.Columns.isDeleted == false)
                .filter(
                    PasswordItem.Columns.title.like(pattern) ||
                    PasswordItem.Columns.username.like(pattern) ||
                    PasswordItem.Columns.website.like(pattern) ||
                    PasswordItem.Columns.notes.like(pattern)
                )
                .order(PasswordItem.Columns.updatedAt.desc)
                .fetchAll(database)
        }
    }

    func fetchById(_ id: Int64) throws -> PasswordItem? {
        try db.read { database in
            try PasswordItem.fetchOne(database, key: id)
        }
    }

    // MARK: - Create

    func create(_ item: inout PasswordItem) throws {
        // Encrypt password if present
        if let password = item.encryptedPassword, !password.hasPrefix("AES:") {
            item.encryptedPassword = try EncryptionService.shared.encrypt(password)
        }

        // Encrypt card number and CVV if present
        if let cardNum = item.cardNumber, !isEncrypted(cardNum) {
            item.cardNumber = try EncryptionService.shared.encrypt(cardNum)
        }
        if let cvv = item.cardCVV, !isEncrypted(cvv) {
            item.cardCVV = try EncryptionService.shared.encrypt(cvv)
        }
        if let accountNum = item.accountNumber, !isEncrypted(accountNum) {
            item.accountNumber = try EncryptionService.shared.encrypt(accountNum)
        }

        item.createdAt = .now
        item.updatedAt = .now
        item.syncId = UUID().uuidString

        try db.write { database in
            try item.insert(database)
        }
    }

    // MARK: - Update

    func update(_ item: inout PasswordItem) throws {
        item.updatedAt = .now
        try db.write { database in
            try item.update(database)
        }
    }

    func updatePassword(itemId: Int64, newPassword: String) throws {
        guard var item = try fetchById(itemId) else { return }
        item.encryptedPassword = try EncryptionService.shared.encrypt(newPassword)
        item.updatedAt = .now
        try db.write { database in
            try item.update(database)
        }
    }

    func toggleFavorite(_ item: PasswordItem) throws {
        var updated = item
        updated.isFavorite.toggle()
        updated.updatedAt = .now
        try db.write { database in
            try updated.update(database)
        }
    }

    // MARK: - Delete

    func softDelete(_ item: PasswordItem) throws {
        var updated = item
        updated.isDeleted = true
        updated.updatedAt = .now
        try db.write { database in
            try updated.update(database)
        }
    }

    func hardDelete(_ item: PasswordItem) throws {
        try db.write { database in
            try item.delete(database)
        }
    }

    func restore(_ item: PasswordItem) throws {
        var updated = item
        updated.isDeleted = false
        updated.updatedAt = .now
        try db.write { database in
            try updated.update(database)
        }
    }

    // MARK: - Statistics

    func count() throws -> Int {
        try db.read { database in
            try PasswordItem
                .filter(PasswordItem.Columns.isDeleted == false)
                .fetchCount(database)
        }
    }

    func countByType() throws -> [ItemType: Int] {
        var result: [ItemType: Int] = [:]
        for type in ItemType.allCases {
            result[type] = try db.read { database in
                try PasswordItem
                    .filter(PasswordItem.Columns.type == type.rawValue)
                    .filter(PasswordItem.Columns.isDeleted == false)
                    .fetchCount(database)
            }
        }
        return result
    }

    // MARK: - Export

    func exportAll() throws -> [[String: String]] {
        let items = try fetchAll()
        return items.map { item in
            var dict: [String: String] = [
                "title": item.title,
                "type": item.type.rawValue,
                "createdAt": ISO8601DateFormatter().string(from: item.createdAt),
            ]
            if let username = item.username { dict["username"] = username }
            if let website = item.website { dict["website"] = website }
            if let notes = item.notes { dict["notes"] = notes }
            return dict
        }
    }

    // MARK: - Helpers

    private func isEncrypted(_ value: String) -> Bool {
        // AES.GCM combined format is always > 28 bytes when base64 encoded
        // Simple heuristic: check if it's valid base64 and has expected minimum length
        Data(base64Encoded: value) != nil && value.count > 40
    }
}
