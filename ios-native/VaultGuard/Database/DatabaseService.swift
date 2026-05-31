import Foundation
import GRDB

class DatabaseService {
    static let shared = DatabaseService()
    private(set) var dbQueue: DatabaseQueue!

    private init() {}

    private var migrator: DatabaseMigrator {
        var m = DatabaseMigrator()
        #if DEBUG
        // In debug, erase DB on schema change (for development)
        m.eraseDatabaseOnSchemaChange = false
        #endif
        Migrations.register(in: &m)
        return m
    }

    func setup() throws {
        let url = try FileManager.default
            .url(for: .applicationSupportDirectory,
                 in: .userDomainMask,
                 appropriateFor: nil,
                 create: true)
            .appendingPathComponent("VaultGuard")

        try FileManager.default.createDirectory(at: url, withIntermediateDirectories: true)

        let dbURL = url.appendingPathComponent("vaultguard.sqlite")

        var config = Configuration()
        config.label = "VaultGuard"
        config.maximumReaderCount = 5

        dbQueue = try DatabaseQueue(path: dbURL.path, configuration: config)

        // Enable WAL mode for better performance
        try dbQueue.write { db in
            try db.execute(sql: "PRAGMA journal_mode=WAL")
            try db.execute(sql: "PRAGMA foreign_keys=ON")
            try db.execute(sql: "PRAGMA synchronous=NORMAL")
        }

        try migrator.migrate(dbQueue)
        print("Database setup complete at: \(dbURL.path)")
    }

    // MARK: - Convenience

    func read<T>(_ block: (Database) throws -> T) throws -> T {
        try dbQueue.read(block)
    }

    func write<T>(_ block: (Database) throws -> T) throws -> T {
        try dbQueue.write(block)
    }
}
