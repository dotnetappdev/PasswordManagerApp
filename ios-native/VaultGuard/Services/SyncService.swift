import Foundation

// MARK: - Sync Models

struct SyncItemDTO: Codable {
    var id: String
    var title: String
    var type: String
    var username: String?
    var encryptedPassword: String?
    var website: String?
    var notes: String?
    var isFavorite: Bool
    var createdAt: Date
    var updatedAt: Date
    var isDeleted: Bool
}

struct SyncResponse: Codable {
    var items: [SyncItemDTO]
    var lastSyncedAt: Date
}

// MARK: - Sync Service

class SyncService: ObservableObject {
    static let shared = SyncService()
    private init() {}

    @Published var isSyncing: Bool = false
    @Published var lastSyncDate: Date? = UserDefaults.standard.object(forKey: "lastSyncDate") as? Date
    @Published var syncError: String?

    var apiBaseURL: String? {
        get { UserDefaults.standard.string(forKey: "apiBaseURL") }
        set { UserDefaults.standard.set(newValue, forKey: "apiBaseURL") }
    }

    var authToken: String? {
        get { UserDefaults.standard.string(forKey: "syncAuthToken") }
        set { UserDefaults.standard.set(newValue, forKey: "syncAuthToken") }
    }

    var isSyncEnabled: Bool {
        apiBaseURL != nil && !apiBaseURL!.isEmpty
    }

    // MARK: - Sync

    @MainActor
    func syncAll() async {
        guard isSyncEnabled, let baseURL = apiBaseURL, let token = authToken else {
            syncError = "Sync not configured. Set API URL in Settings."
            return
        }

        isSyncing = true
        syncError = nil

        do {
            try await performSync(baseURL: baseURL, token: token)
            lastSyncDate = Date()
            UserDefaults.standard.set(lastSyncDate, forKey: "lastSyncDate")
        } catch {
            syncError = error.localizedDescription
        }

        isSyncing = false
    }

    private func performSync(baseURL: String, token: String) async throws {
        // Push local changes first
        try await pushChanges(baseURL: baseURL, token: token)

        // Then pull remote changes
        try await pullChanges(baseURL: baseURL, token: token)
    }

    // MARK: - Push

    private func pushChanges(baseURL: String, token: String) async throws {
        let since = lastSyncDate ?? Date(timeIntervalSince1970: 0)
        let localItems = try PasswordItemService.shared.fetchAll()

        // Filter items modified since last sync
        let toSync = localItems.filter { item in
            item.updatedAt > since || item.lastSyncedAt == nil
        }

        guard !toSync.isEmpty else { return }

        let dtos = toSync.map { item -> SyncItemDTO in
            SyncItemDTO(
                id: item.syncId ?? UUID().uuidString,
                title: item.title,
                type: item.type.rawValue,
                username: item.username,
                encryptedPassword: item.encryptedPassword,
                website: item.website,
                notes: item.notes,
                isFavorite: item.isFavorite,
                createdAt: item.createdAt,
                updatedAt: item.updatedAt,
                isDeleted: item.isDeleted
            )
        }

        guard let url = URL(string: "\(baseURL)/api/sync/push") else {
            throw SyncError.invalidURL
        }

        var request = URLRequest(url: url)
        request.httpMethod = "POST"
        request.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")

        let encoder = JSONEncoder()
        encoder.dateEncodingStrategy = .iso8601
        request.httpBody = try encoder.encode(dtos)

        let (_, response) = try await URLSession.shared.data(for: request)

        guard let httpResponse = response as? HTTPURLResponse,
              (200...299).contains(httpResponse.statusCode) else {
            throw SyncError.pushFailed
        }
    }

    // MARK: - Pull

    private func pullChanges(baseURL: String, token: String) async throws {
        let since = lastSyncDate.map { ISO8601DateFormatter().string(from: $0) } ?? ""
        var urlString = "\(baseURL)/api/sync/pull"
        if !since.isEmpty {
            urlString += "?since=\(since)"
        }

        guard let url = URL(string: urlString) else {
            throw SyncError.invalidURL
        }

        var request = URLRequest(url: url)
        request.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")

        let (data, response) = try await URLSession.shared.data(for: request)

        guard let httpResponse = response as? HTTPURLResponse,
              (200...299).contains(httpResponse.statusCode) else {
            throw SyncError.pullFailed
        }

        let decoder = JSONDecoder()
        decoder.dateDecodingStrategy = .iso8601
        let syncResponse = try decoder.decode(SyncResponse.self, from: data)

        // Merge pulled items
        try await mergePulledItems(syncResponse.items)
    }

    private func mergePulledItems(_ items: [SyncItemDTO]) async throws {
        for dto in items {
            // Check if we have this item locally by syncId
            let existing = try? PasswordItemService.shared.fetchAll().first { $0.syncId == dto.id }

            if let existingItem = existing {
                // If remote is newer, update local
                if dto.updatedAt > existingItem.updatedAt {
                    var updated = existingItem
                    updated.title = dto.title
                    updated.username = dto.username
                    updated.encryptedPassword = dto.encryptedPassword
                    updated.website = dto.website
                    updated.notes = dto.notes
                    updated.isFavorite = dto.isFavorite
                    updated.isDeleted = dto.isDeleted
                    updated.updatedAt = dto.updatedAt
                    updated.lastSyncedAt = Date()
                    try PasswordItemService.shared.update(&updated)
                }
            } else if !dto.isDeleted {
                // New item from server
                guard let type = ItemType(rawValue: dto.type) else { continue }
                var newItem = PasswordItem(
                    title: dto.title,
                    type: type,
                    username: dto.username,
                    encryptedPassword: dto.encryptedPassword,
                    website: dto.website,
                    notes: dto.notes,
                    isFavorite: dto.isFavorite,
                    createdAt: dto.createdAt,
                    updatedAt: dto.updatedAt,
                    isDeleted: dto.isDeleted,
                    syncId: dto.id,
                    lastSyncedAt: Date()
                )
                try PasswordItemService.shared.create(&newItem)
            }
        }
    }

    // MARK: - Login

    func login(baseURL: String, username: String, password: String) async throws -> String {
        guard let url = URL(string: "\(baseURL)/api/auth/login") else {
            throw SyncError.invalidURL
        }

        var request = URLRequest(url: url)
        request.httpMethod = "POST"
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")

        let body = ["username": username, "password": password]
        request.httpBody = try JSONEncoder().encode(body)

        let (data, response) = try await URLSession.shared.data(for: request)

        guard let httpResponse = response as? HTTPURLResponse,
              (200...299).contains(httpResponse.statusCode) else {
            throw SyncError.authenticationFailed
        }

        let result = try JSONDecoder().decode([String: String].self, from: data)
        guard let token = result["token"] else {
            throw SyncError.authenticationFailed
        }

        return token
    }
}

// MARK: - Errors

enum SyncError: LocalizedError {
    case invalidURL
    case pushFailed
    case pullFailed
    case authenticationFailed
    case notConfigured

    var errorDescription: String? {
        switch self {
        case .invalidURL: return "Invalid API URL."
        case .pushFailed: return "Failed to push changes to server."
        case .pullFailed: return "Failed to pull changes from server."
        case .authenticationFailed: return "Authentication with server failed."
        case .notConfigured: return "Sync is not configured. Set an API URL in Settings."
        }
    }
}
