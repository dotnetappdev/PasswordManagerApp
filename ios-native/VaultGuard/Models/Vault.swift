import Foundation
import GRDB

struct Vault: Identifiable, Codable, FetchableRecord, PersistableRecord {
    var id: Int64?
    var name: String
    var iconName: String = "lock.shield.fill"
    var colorHex: String = "7C3AED"
    var description: String?
    var createdAt: Date = .now
    var updatedAt: Date = .now
    var isDefault: Bool = false
    var syncId: String? // UUID for server sync

    static let databaseTableName = "vaults"

    enum Columns: String, ColumnExpression {
        case id, name, iconName, colorHex, description
        case createdAt, updatedAt, isDefault, syncId
    }

    static let items = hasMany(PasswordItem.self)
}

// MARK: - Default Vault

extension Vault {
    static var defaultVault: Vault {
        Vault(
            name: "Personal",
            iconName: "person.fill",
            colorHex: "7C3AED",
            isDefault: true
        )
    }
}
