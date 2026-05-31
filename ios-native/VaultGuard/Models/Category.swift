import Foundation
import GRDB
import SwiftUI

struct Category: Identifiable, Codable, FetchableRecord, PersistableRecord {
    var id: Int64?
    var name: String
    var iconName: String = "folder.fill"
    var colorHex: String = "7C3AED"
    var createdAt: Date = .now
    var updatedAt: Date = .now
    var syncId: String?

    static let databaseTableName = "categories"

    enum Columns: String, ColumnExpression {
        case id, name, iconName, colorHex, createdAt, updatedAt, syncId
    }

    static let items = hasMany(PasswordItem.self)

    var color: Color {
        Color(hex: colorHex)
    }
}

// MARK: - Default Categories

extension Category {
    static var defaultCategories: [Category] {
        [
            Category(name: "Social", iconName: "person.2.fill", colorHex: "3B82F6"),
            Category(name: "Finance", iconName: "dollarsign.circle.fill", colorHex: "10B981"),
            Category(name: "Shopping", iconName: "bag.fill", colorHex: "F59E0B"),
            Category(name: "Entertainment", iconName: "play.circle.fill", colorHex: "EC4899"),
            Category(name: "Work", iconName: "briefcase.fill", colorHex: "8B5CF6"),
            Category(name: "Health", iconName: "heart.fill", colorHex: "EF4444"),
            Category(name: "Travel", iconName: "airplane", colorHex: "06B6D4"),
        ]
    }
}
