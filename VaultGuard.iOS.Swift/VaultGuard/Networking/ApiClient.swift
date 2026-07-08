// ApiClient.swift — VaultGuard REST client used in API connection mode.
import Foundation

@MainActor
final class ApiClient {
    private let configStore: ConfigStore
    private let keychain: Keychain
    private let session: Session

    init(configStore: ConfigStore, keychain: Keychain, session: Session) {
        self.configStore = configStore
        self.keychain = keychain
        self.session = session
    }

    enum ApiError: LocalizedError {
        case notConfigured, http(Int), message(String)
        var errorDescription: String? {
            switch self {
            case .notConfigured: return "API URL is not configured."
            case .http(let code): return code == 401 ? "Unauthorized — check the API key." : "Server returned HTTP \(code)."
            case .message(let m): return m
            }
        }
    }

    // MARK: - Requests

    private func request(_ path: String, method: String = "GET", body: Data? = nil,
                         baseOverride: String? = nil, keyOverride: String? = nil) throws -> URLRequest {
        let base = baseOverride ?? configStore.config.normalizedBaseUrl
        guard !base.isEmpty, let url = URL(string: base + path) else { throw ApiError.notConfigured }
        var req = URLRequest(url: url)
        req.httpMethod = method
        req.setValue("application/json", forHTTPHeaderField: "Accept")
        if let key = keyOverride ?? keychain.get(Keychain.Keys.apiKey) {
            req.setValue(key, forHTTPHeaderField: "X-API-Key")
        }
        if let token = session.sessionToken {
            req.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")
        }
        if let body {
            req.setValue("application/json", forHTTPHeaderField: "Content-Type")
            req.httpBody = body
        }
        return req
    }

    private func send<T: Decodable>(_ req: URLRequest, as type: T.Type) async throws -> T {
        let (data, response) = try await URLSession.shared.data(for: req)
        guard let http = response as? HTTPURLResponse else { throw ApiError.message("No response.") }
        guard (200..<300).contains(http.statusCode) else { throw ApiError.http(http.statusCode) }
        return try JSONDecoder().decode(T.self, from: data)
    }

    private func sendNoContent(_ req: URLRequest) async throws {
        let (_, response) = try await URLSession.shared.data(for: req)
        guard let http = response as? HTTPURLResponse else { throw ApiError.message("No response.") }
        guard (200..<300).contains(http.statusCode) else { throw ApiError.http(http.statusCode) }
    }

    private func encode<T: Encodable>(_ value: T) throws -> Data { try JSONEncoder().encode(value) }

    // MARK: - Endpoints

    func login(email: String, password: String, twoFactorCode: String?) async throws -> LoginResponse {
        let body = try encode(EnhancedLoginRequest(email: email, password: password, twoFactorCode: twoFactorCode))
        return try await send(request("api/auth/login/enhanced", method: "POST", body: body), as: LoginResponse.self)
    }

    func items() async throws -> [PasswordItemDto] {
        try await send(request("api/passworditems"), as: [PasswordItemDto].self)
    }

    func decrypt(id: Int) async throws -> DecryptedPasswordItemDto {
        try await send(request("api/passworditems/\(id)/decrypt", method: "POST"), as: DecryptedPasswordItemDto.self)
    }

    func createEncrypted(_ dto: CreateEncryptedPasswordItem) async throws -> PasswordItemDto {
        try await send(request("api/passworditems/encrypted", method: "POST", body: encode(dto)), as: PasswordItemDto.self)
    }

    func update(id: Int, _ dto: UpdatePasswordItem) async throws {
        try await sendNoContent(request("api/passworditems/\(id)", method: "PUT", body: encode(dto)))
    }

    func delete(id: Int) async throws {
        try await sendNoContent(request("api/passworditems/\(id)", method: "DELETE"))
    }

    func toggleFavorite(id: Int) async throws {
        try await sendNoContent(request("api/passworditems/\(id)/toggle-favorite", method: "PATCH"))
    }

    func categories() async throws -> [CategoryDto] {
        try await send(request("api/categories"), as: [CategoryDto].self)
    }

    func vaults() async throws -> [VaultDto] {
        try await send(request("api/vaults"), as: [VaultDto].self)
    }

    /// Verify a URL + key before saving. Returns nil on success or an error message.
    func testConnection(baseUrl: String, apiKey: String) async -> String? {
        let normalized = baseUrl.hasSuffix("/") ? baseUrl : baseUrl + "/"
        do {
            let req = try request("api/passworditems", method: "GET", baseOverride: normalized, keyOverride: apiKey)
            let (_, response) = try await URLSession.shared.data(for: req)
            guard let http = response as? HTTPURLResponse else { return "No response." }
            if (200..<300).contains(http.statusCode) { return nil }
            return http.statusCode == 401 ? "Unauthorized — check the API key." : "Server returned HTTP \(http.statusCode)."
        } catch {
            return error.localizedDescription
        }
    }
}
