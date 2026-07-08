// LocalStore.swift — on-device encrypted vault (LOCAL mode) backed by SQLite.
import Foundation
import SQLite3

struct VaultRow {
    var id: Int = 0
    var title: String
    var descriptionText: String?
    var type: Int
    var isFavorite: Bool
    var isArchived: Bool = false
    var isDeleted: Bool = false
    var username: String?
    var email: String?
    var website: String?
    var loginUrl: String?
    var notes: String?
    var encPassword: String?
    var encTotp: String?
    var categoryName: String?
    var createdAt: Double
    var lastModified: Double
}

final class LocalStore {
    private var db: OpaquePointer?
    private let SQLITE_TRANSIENT = unsafeBitCast(-1, to: sqlite3_destructor_type.self)

    init() {
        let url = FileManager.default
            .urls(for: .applicationSupportDirectory, in: .userDomainMask)[0]
            .appendingPathComponent("vaultguard-local.sqlite")
        try? FileManager.default.createDirectory(at: url.deletingLastPathComponent(), withIntermediateDirectories: true)
        sqlite3_open(url.path, &db)
        exec("""
            CREATE TABLE IF NOT EXISTS vault_items (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                title TEXT NOT NULL, description TEXT, type INTEGER NOT NULL,
                isFavorite INTEGER NOT NULL DEFAULT 0, isArchived INTEGER NOT NULL DEFAULT 0,
                isDeleted INTEGER NOT NULL DEFAULT 0,
                username TEXT, email TEXT, website TEXT, loginUrl TEXT, notes TEXT,
                encPassword TEXT, encTotp TEXT, categoryName TEXT,
                createdAt REAL NOT NULL, lastModified REAL NOT NULL
            );
        """)
    }

    deinit { sqlite3_close(db) }

    private func exec(_ sql: String) { sqlite3_exec(db, sql, nil, nil, nil) }

    private func text(_ stmt: OpaquePointer?, _ col: Int32) -> String? {
        guard let c = sqlite3_column_text(stmt, col) else { return nil }
        return String(cString: c)
    }

    private func bind(_ stmt: OpaquePointer?, _ i: Int32, _ value: String?) {
        if let value { sqlite3_bind_text(stmt, i, value, -1, SQLITE_TRANSIENT) }
        else { sqlite3_bind_null(stmt, i) }
    }

    private func rowFrom(_ stmt: OpaquePointer?) -> VaultRow {
        VaultRow(
            id: Int(sqlite3_column_int64(stmt, 0)),
            title: text(stmt, 1) ?? "",
            descriptionText: text(stmt, 2),
            type: Int(sqlite3_column_int(stmt, 3)),
            isFavorite: sqlite3_column_int(stmt, 4) == 1,
            isArchived: sqlite3_column_int(stmt, 5) == 1,
            isDeleted: sqlite3_column_int(stmt, 6) == 1,
            username: text(stmt, 7), email: text(stmt, 8), website: text(stmt, 9),
            loginUrl: text(stmt, 10), notes: text(stmt, 11),
            encPassword: text(stmt, 12), encTotp: text(stmt, 13), categoryName: text(stmt, 14),
            createdAt: sqlite3_column_double(stmt, 15),
            lastModified: sqlite3_column_double(stmt, 16)
        )
    }

    private let columns = "id,title,description,type,isFavorite,isArchived,isDeleted,username,email,website,loginUrl,notes,encPassword,encTotp,categoryName,createdAt,lastModified"

    func all() -> [VaultRow] {
        var stmt: OpaquePointer?
        var out: [VaultRow] = []
        if sqlite3_prepare_v2(db, "SELECT \(columns) FROM vault_items ORDER BY isFavorite DESC, title COLLATE NOCASE ASC", -1, &stmt, nil) == SQLITE_OK {
            while sqlite3_step(stmt) == SQLITE_ROW { out.append(rowFrom(stmt)) }
        }
        sqlite3_finalize(stmt)
        return out
    }

    func byId(_ id: Int) -> VaultRow? {
        var stmt: OpaquePointer?
        var row: VaultRow?
        if sqlite3_prepare_v2(db, "SELECT \(columns) FROM vault_items WHERE id = ?", -1, &stmt, nil) == SQLITE_OK {
            sqlite3_bind_int64(stmt, 1, Int64(id))
            if sqlite3_step(stmt) == SQLITE_ROW { row = rowFrom(stmt) }
        }
        sqlite3_finalize(stmt)
        return row
    }

    @discardableResult
    func insert(_ r: VaultRow) -> Int {
        var stmt: OpaquePointer?
        let sql = "INSERT INTO vault_items (title,description,type,isFavorite,isArchived,isDeleted,username,email,website,loginUrl,notes,encPassword,encTotp,categoryName,createdAt,lastModified) VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)"
        guard sqlite3_prepare_v2(db, sql, -1, &stmt, nil) == SQLITE_OK else { return 0 }
        bind(stmt, 1, r.title); bind(stmt, 2, r.descriptionText)
        sqlite3_bind_int(stmt, 3, Int32(r.type))
        sqlite3_bind_int(stmt, 4, r.isFavorite ? 1 : 0)
        sqlite3_bind_int(stmt, 5, r.isArchived ? 1 : 0)
        sqlite3_bind_int(stmt, 6, r.isDeleted ? 1 : 0)
        bind(stmt, 7, r.username); bind(stmt, 8, r.email); bind(stmt, 9, r.website)
        bind(stmt, 10, r.loginUrl); bind(stmt, 11, r.notes)
        bind(stmt, 12, r.encPassword); bind(stmt, 13, r.encTotp); bind(stmt, 14, r.categoryName)
        sqlite3_bind_double(stmt, 15, r.createdAt)
        sqlite3_bind_double(stmt, 16, r.lastModified)
        sqlite3_step(stmt)
        sqlite3_finalize(stmt)
        return Int(sqlite3_last_insert_rowid(db))
    }

    func update(_ r: VaultRow) {
        var stmt: OpaquePointer?
        let sql = "UPDATE vault_items SET title=?,description=?,isFavorite=?,isArchived=?,isDeleted=?,username=?,email=?,website=?,loginUrl=?,notes=?,encPassword=?,encTotp=?,categoryName=?,lastModified=? WHERE id=?"
        guard sqlite3_prepare_v2(db, sql, -1, &stmt, nil) == SQLITE_OK else { return }
        bind(stmt, 1, r.title); bind(stmt, 2, r.descriptionText)
        sqlite3_bind_int(stmt, 3, r.isFavorite ? 1 : 0)
        sqlite3_bind_int(stmt, 4, r.isArchived ? 1 : 0)
        sqlite3_bind_int(stmt, 5, r.isDeleted ? 1 : 0)
        bind(stmt, 6, r.username); bind(stmt, 7, r.email); bind(stmt, 8, r.website)
        bind(stmt, 9, r.loginUrl); bind(stmt, 10, r.notes)
        bind(stmt, 11, r.encPassword); bind(stmt, 12, r.encTotp); bind(stmt, 13, r.categoryName)
        sqlite3_bind_double(stmt, 14, r.lastModified)
        sqlite3_bind_int64(stmt, 15, Int64(r.id))
        sqlite3_step(stmt)
        sqlite3_finalize(stmt)
    }

    func delete(_ id: Int) {
        var stmt: OpaquePointer?
        if sqlite3_prepare_v2(db, "DELETE FROM vault_items WHERE id = ?", -1, &stmt, nil) == SQLITE_OK {
            sqlite3_bind_int64(stmt, 1, Int64(id))
            sqlite3_step(stmt)
        }
        sqlite3_finalize(stmt)
    }

    func deleteAll() { exec("DELETE FROM vault_items") }
    func count() -> Int { all().count }
}
