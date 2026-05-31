import Foundation
import GRDB

struct Migrations {
    static func register(in migrator: inout DatabaseMigrator) {
        // MARK: - v1: Initial Schema

        migrator.registerMigration("v1_initial") { db in
            // Vaults table
            try db.create(table: "vaults") { t in
                t.autoIncrementedPrimaryKey("id")
                t.column("name", .text).notNull()
                t.column("iconName", .text).notNull().defaults(to: "lock.shield.fill")
                t.column("colorHex", .text).notNull().defaults(to: "7C3AED")
                t.column("description", .text)
                t.column("createdAt", .datetime).notNull()
                t.column("updatedAt", .datetime).notNull()
                t.column("isDefault", .boolean).notNull().defaults(to: false)
                t.column("syncId", .text)
            }

            // Categories table
            try db.create(table: "categories") { t in
                t.autoIncrementedPrimaryKey("id")
                t.column("name", .text).notNull()
                t.column("iconName", .text).notNull().defaults(to: "folder.fill")
                t.column("colorHex", .text).notNull().defaults(to: "7C3AED")
                t.column("createdAt", .datetime).notNull()
                t.column("updatedAt", .datetime).notNull()
                t.column("syncId", .text)
            }

            // Tags table
            try db.create(table: "tags") { t in
                t.autoIncrementedPrimaryKey("id")
                t.column("name", .text).notNull()
                t.column("colorHex", .text).notNull().defaults(to: "7C3AED")
                t.column("createdAt", .datetime).notNull()
                t.column("syncId", .text)
            }

            // Password items table
            try db.create(table: "password_items") { t in
                t.autoIncrementedPrimaryKey("id")
                t.column("title", .text).notNull()
                t.column("type", .text).notNull()
                t.column("username", .text)
                t.column("encryptedPassword", .text)
                t.column("website", .text)
                t.column("notes", .text)
                t.column("isFavorite", .boolean).notNull().defaults(to: false)
                t.column("vaultId", .integer).references("vaults", onDelete: .setNull)
                t.column("categoryId", .integer).references("categories", onDelete: .setNull)
                t.column("tagIds", .text)
                t.column("createdAt", .datetime).notNull()
                t.column("updatedAt", .datetime).notNull()
                t.column("isDeleted", .boolean).notNull().defaults(to: false)
                t.column("customFields", .text)
                t.column("syncId", .text)
                t.column("lastSyncedAt", .datetime)

                // Credit card
                t.column("cardNumber", .text)
                t.column("cardExpiry", .text)
                t.column("cardCVV", .text)
                t.column("cardholderName", .text)

                // Wi-Fi
                t.column("networkName", .text)
                t.column("securityType", .text)

                // Identity
                t.column("firstName", .text)
                t.column("lastName", .text)
                t.column("email", .text)
                t.column("phone", .text)
                t.column("address", .text)

                // Bank account
                t.column("accountNumber", .text)
                t.column("routingNumber", .text)
                t.column("bankName", .text)
            }

            // Item-Tag join table
            try db.create(table: "item_tags") { t in
                t.column("itemId", .integer).notNull().references("password_items", onDelete: .cascade)
                t.column("tagId", .integer).notNull().references("tags", onDelete: .cascade)
                t.primaryKey(["itemId", "tagId"])
            }

            // Indexes
            try db.create(index: "idx_items_vault", on: "password_items", columns: ["vaultId"])
            try db.create(index: "idx_items_category", on: "password_items", columns: ["categoryId"])
            try db.create(index: "idx_items_type", on: "password_items", columns: ["type"])
            try db.create(index: "idx_items_favorite", on: "password_items", columns: ["isFavorite"])
            try db.create(index: "idx_items_deleted", on: "password_items", columns: ["isDeleted"])
            try db.create(index: "idx_items_syncId", on: "password_items", columns: ["syncId"])
        }

        // MARK: - v2: Seed Default Data

        migrator.registerMigration("v2_seed_defaults") { db in
            // Insert default vault
            let now = Date()
            try db.execute(
                sql: """
                    INSERT INTO vaults (name, iconName, colorHex, createdAt, updatedAt, isDefault)
                    VALUES (?, ?, ?, ?, ?, ?)
                """,
                arguments: ["Personal", "person.fill", "7C3AED", now, now, true]
            )

            // Insert default categories
            let categories: [(String, String, String)] = [
                ("Social", "person.2.fill", "3B82F6"),
                ("Finance", "dollarsign.circle.fill", "10B981"),
                ("Shopping", "bag.fill", "F59E0B"),
                ("Entertainment", "play.circle.fill", "EC4899"),
                ("Work", "briefcase.fill", "8B5CF6"),
                ("Health", "heart.fill", "EF4444"),
                ("Travel", "airplane", "06B6D4"),
            ]

            for (name, icon, color) in categories {
                try db.execute(
                    sql: """
                        INSERT INTO categories (name, iconName, colorHex, createdAt, updatedAt)
                        VALUES (?, ?, ?, ?, ?)
                    """,
                    arguments: [name, icon, color, now, now]
                )
            }
        }
    }
}
