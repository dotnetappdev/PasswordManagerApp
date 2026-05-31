import Foundation
import GRDB
import SwiftUI

struct Tag: Identifiable, Codable, FetchableRecord, PersistableRecord {
    var id: Int64?
    var name: String
    var colorHex: String = "7C3AED"
    var createdAt: Date = .now
    var syncId: String?

    static let databaseTableName = "tags"

    enum Columns: String, ColumnExpression {
        case id, name, colorHex, createdAt, syncId
    }

    var color: Color {
        Color(hex: colorHex)
    }
}

// MARK: - Item Tag Join Table

struct ItemTag: Codable, FetchableRecord, PersistableRecord {
    var itemId: Int64
    var tagId: Int64

    static let databaseTableName = "item_tags"

    enum Columns: String, ColumnExpression {
        case itemId, tagId
    }
}
