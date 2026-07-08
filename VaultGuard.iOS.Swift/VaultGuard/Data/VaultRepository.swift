// VaultRepository.swift — single entry point the UI uses; delegates to API or local SQLite by mode.
import Foundation
import CryptoKit

enum LoginResult { case success, needsTwoFactor, error(String) }

@MainActor
final class VaultRepository {
    private let api: ApiClient
    private let local: LocalStore
    private let crypto: VaultCrypto
    private let session: Session
    private let keychain: Keychain
    private let configStore: ConfigStore

    private let verifierPlaintext = "vaultguard-local-verifier-v1"

    init(api: ApiClient, local: LocalStore, crypto: VaultCrypto, session: Session,
         keychain: Keychain, configStore: ConfigStore) {
        self.api = api; self.local = local; self.crypto = crypto
        self.session = session; self.keychain = keychain; self.configStore = configStore
    }

    private var mode: ConnectionMode { configStore.config.mode }

    private func localKey() throws -> SymmetricKey {
        guard let master = session.masterPassword else { throw RepoError.locked }
        guard let saltB64 = keychain.get(Keychain.Keys.localSalt), let salt = Data(base64Encoded: saltB64) else {
            throw RepoError.notInitialized
        }
        return crypto.deriveKey(masterPassword: master, salt: salt)
    }

    enum RepoError: LocalizedError {
        case locked, notInitialized
        var errorDescription: String? {
            switch self { case .locked: return "Vault is locked."; case .notInitialized: return "Local vault is not initialised." }
        }
    }

    // MARK: - Auth / unlock

    func login(email: String, password: String, twoFactorCode: String?) async -> LoginResult {
        do {
            let resp = try await api.login(email: email, password: password, twoFactorCode: twoFactorCode?.isEmpty == true ? nil : twoFactorCode)
            if resp.requiresTwoFactor && (twoFactorCode?.isEmpty ?? true) { return .needsTwoFactor }
            if let token = resp.authResponse?.token, !token.isEmpty {
                session.onLoggedIn(token: token, user: resp.authResponse?.user, masterPassword: password)
                return .success
            }
            return .error("Login failed. Check your credentials.")
        } catch {
            return .error(error.localizedDescription)
        }
    }

    func unlockLocal(masterPassword: String) -> LoginResult {
        let saltB64 = keychain.get(Keychain.Keys.localSalt)
        let verifier = keychain.get(Keychain.Keys.localVerifier)
        do {
            if saltB64 == nil || verifier == nil {
                let salt = crypto.newSalt()
                let key = crypto.deriveKey(masterPassword: masterPassword, salt: salt)
                keychain.set(salt.base64EncodedString(), for: Keychain.Keys.localSalt)
                keychain.set(try crypto.encrypt(verifierPlaintext, key: key), for: Keychain.Keys.localVerifier)
                session.onLocalUnlocked(masterPassword: masterPassword)
                return .success
            }
            let salt = Data(base64Encoded: saltB64!)!
            let key = crypto.deriveKey(masterPassword: masterPassword, salt: salt)
            if (try? crypto.decrypt(verifier!, key: key)) == verifierPlaintext {
                session.onLocalUnlocked(masterPassword: masterPassword)
                return .success
            }
            return .error("Incorrect master password.")
        } catch {
            return .error(error.localizedDescription)
        }
    }

    // MARK: - Listing

    func list() async throws -> [VaultItem] {
        switch mode {
        case .api: return try await api.items().map(VaultItem.from)
        case .local: return local.all().map(toVaultItem)
        }
    }

    func get(id: Int) async -> VaultItem? {
        switch mode {
        case .api: return (try? await api.items())?.first { $0.id == id }.map(VaultItem.from)
        case .local: return local.byId(id).map(toVaultItem)
        }
    }

    func secret(id: Int) async throws -> ItemSecret {
        switch mode {
        case .api:
            let d = try await api.decrypt(id: id).loginItem
            return ItemSecret(password: d?.password, totpSecret: d?.totpSecret)
        case .local:
            guard let row = local.byId(id) else { return ItemSecret() }
            let key = try localKey()
            return ItemSecret(
                password: try row.encPassword.map { try crypto.decrypt($0, key: key) },
                totpSecret: try row.encTotp.map { try crypto.decrypt($0, key: key) }
            )
        }
    }

    func categories() async -> [CategoryDto] {
        switch mode {
        case .api:
            let remote = (try? await api.categories()) ?? []
            return remote.isEmpty ? DefaultData.categories : remote
        case .local:
            let custom = Set(local.all().compactMap { $0.categoryName })
                .subtracting(DefaultData.categories.map { $0.name })
            return DefaultData.categories + custom.enumerated().map { CategoryDto(id: 100 + $0.offset, name: $0.element) }
        }
    }

    func vaults() async -> [VaultDto] {
        switch mode {
        case .api:
            let remote = (try? await api.vaults()) ?? []
            return remote.isEmpty ? [DefaultData.personalVault] : remote
        case .local:
            var v = DefaultData.personalVault
            v.itemCount = local.all().filter { !$0.isDeleted }.count
            return [v]
        }
    }

    // MARK: - Mutations

    func create(_ input: LoginItemInput) async throws {
        switch mode {
        case .api:
            guard let master = session.masterPassword else { throw RepoError.locked }
            _ = try await api.createEncrypted(CreateEncryptedPasswordItem(
                title: input.title, description: input.description, type: input.type.rawValue,
                isFavorite: input.isFavorite, masterPassword: master,
                loginItem: CreateLoginItem(website: input.website, username: input.username,
                    email: input.email, password: input.password, totpSecret: input.totpSecret,
                    loginUrl: input.loginUrl, notes: input.notes)
            ))
        case .local:
            let key = try localKey()
            let now = Date().timeIntervalSince1970
            local.insert(VaultRow(
                title: input.title, descriptionText: input.description, type: input.type.rawValue,
                isFavorite: input.isFavorite, username: input.username, email: input.email,
                website: input.website, loginUrl: input.loginUrl, notes: input.notes,
                encPassword: try input.password.flatMap { $0.isEmpty ? nil : try crypto.encrypt($0, key: key) },
                encTotp: try input.totpSecret.flatMap { $0.isEmpty ? nil : try crypto.encrypt($0, key: key) },
                categoryName: input.categoryName, createdAt: now, lastModified: now))
        }
    }

    func update(id: Int, _ input: LoginItemInput) async throws {
        switch mode {
        case .api:
            try await api.update(id: id, UpdatePasswordItem(
                title: input.title, description: input.description, isFavorite: input.isFavorite,
                loginItem: CreateLoginItem(website: input.website, username: input.username,
                    email: input.email, password: (input.password?.isEmpty ?? true) ? nil : input.password,
                    totpSecret: input.totpSecret, loginUrl: input.loginUrl, notes: input.notes)))
        case .local:
            guard var row = local.byId(id) else { return }
            let key = try localKey()
            row.title = input.title; row.descriptionText = input.description
            row.isFavorite = input.isFavorite; row.username = input.username
            row.email = input.email; row.website = input.website
            row.loginUrl = input.loginUrl; row.notes = input.notes
            if let p = input.password, !p.isEmpty { row.encPassword = try crypto.encrypt(p, key: key) }
            if let t = input.totpSecret, !t.isEmpty { row.encTotp = try crypto.encrypt(t, key: key) }
            row.lastModified = Date().timeIntervalSince1970
            local.update(row)
        }
    }

    func delete(id: Int) async throws {
        switch mode {
        case .api: try await api.delete(id: id)
        case .local: local.delete(id)
        }
    }

    func toggleFavorite(id: Int) async throws {
        switch mode {
        case .api: try await api.toggleFavorite(id: id)
        case .local:
            guard var row = local.byId(id) else { return }
            row.isFavorite.toggle(); local.update(row)
        }
    }

    // MARK: - Seed

    func seedLocalDemoIfEmpty() {
        guard mode == .local, local.all().isEmpty else { return }
        seedLocalDemo()
    }

    func seedLocalDemo() {
        guard mode == .local, session.masterPassword != nil, let key = try? localKey() else { return }
        let now = Date().timeIntervalSince1970
        for d in Self.demoItems {
            local.insert(VaultRow(
                title: d.title, descriptionText: d.notes, type: d.type.rawValue,
                isFavorite: d.title == "GitHub", username: d.username, email: d.email,
                website: d.website, loginUrl: d.loginUrl, notes: d.notes,
                encPassword: try? d.password.map { try crypto.encrypt($0, key: key) } ?? nil,
                encTotp: try? d.totp.map { try crypto.encrypt($0, key: key) } ?? nil,
                categoryName: d.category, createdAt: now, lastModified: now))
        }
    }

    func resetLocal() {
        guard mode == .local else { return }
        local.deleteAll()
    }

    private func toVaultItem(_ r: VaultRow) -> VaultItem {
        VaultItem(id: r.id, title: r.title, description: r.descriptionText, type: ItemType.from(r.type),
                  isFavorite: r.isFavorite, isArchived: r.isArchived, isDeleted: r.isDeleted,
                  username: r.username, email: r.email, website: r.website, loginUrl: r.loginUrl,
                  notes: r.notes, categoryName: r.categoryName, tags: [])
    }

    private struct Demo {
        let title: String; let type: ItemType; let username: String?; let email: String?
        let website: String?; let loginUrl: String?; let password: String?; let totp: String?
        let notes: String?; let category: String
    }

    private static let demoItems: [Demo] = [
        Demo(title: "GitHub", type: .login, username: "octocat", email: "octocat@example.com", website: "github.com", loginUrl: "https://github.com/login", password: "gh_Demo!2024pass", totp: "JBSWY3DPEHPK3PXP", notes: "Personal open-source account.", category: "Logins"),
        Demo(title: "Google", type: .login, username: "jane.doe", email: "jane.doe@gmail.com", website: "google.com", loginUrl: "https://accounts.google.com", password: "G00gle!Demo#77", totp: nil, notes: nil, category: "Logins"),
        Demo(title: "Work Email", type: .login, username: "j.doe", email: "j.doe@company.com", website: "outlook.office.com", loginUrl: nil, password: "W0rkMail$Demo9", totp: nil, notes: "Corporate mailbox.", category: "Logins"),
        Demo(title: "Visa •• 4242", type: .creditCard, username: "Jane Doe", email: nil, website: nil, loginUrl: nil, password: "4242 4242 4242 4242", totp: nil, notes: "Exp 04/28 · CVV 123", category: "Credit Cards"),
        Demo(title: "Home Wi-Fi", type: .wifi, username: nil, email: nil, website: nil, loginUrl: nil, password: "MyHomeNetwork#2024", totp: nil, notes: "SSID: HomeNet-5G", category: "WiFi Networks"),
        Demo(title: "Recovery Codes", type: .secureNote, username: nil, email: nil, website: nil, loginUrl: nil, password: nil, totp: nil, notes: "Store your backup/recovery codes here.", category: "Secure Notes"),
    ]
}
