// OnePasswordConnectImporter.swift — import items from a 1Password Connect server (or a Service Account,
// which speaks the same REST API). This is the only supported *programmatic* path — 1Password has no
// consumer read API — so the user supplies their Connect server URL and an access token. The desktop/web
// apps keep using the existing file (1PUX/CSV) importers; this is a mobile-only convenience.
//
// Endpoints (Connect v1):
//   GET {host}/v1/vaults
//   GET {host}/v1/vaults/{vaultId}/items
//   GET {host}/v1/vaults/{vaultId}/items/{itemId}   (full item incl. fields)
import Foundation

struct OnePasswordConnectImporter {

    struct ImportResult { let items: [LoginItemInput]; let error: String? }

    func fetch(host: String, token: String) async -> ImportResult {
        let base = host.trimmingCharacters(in: .whitespacesAndNewlines)
            .trimmingCharacters(in: CharacterSet(charactersIn: "/"))
        guard !base.isEmpty else { return .init(items: [], error: "Enter your 1Password Connect server URL.") }
        guard base.lowercased().hasPrefix("http") else {
            return .init(items: [], error: "The server URL must start with http:// or https://.")
        }
        guard !token.isEmpty else { return .init(items: [], error: "Enter your Connect access token.") }

        do {
            let vaults: [OpVault] = try await get("\(base)/v1/vaults", token: token)
            var out: [LoginItemInput] = []
            for v in vaults {
                let summaries: [OpItemSummary] = try await get("\(base)/v1/vaults/\(v.id)/items", token: token)
                for s in summaries {
                    if let item: OpItem = try? await get("\(base)/v1/vaults/\(v.id)/items/\(s.id)", token: token) {
                        out.append(map(item))
                    }
                }
            }
            return .init(items: out, error: nil)
        } catch let e as ImportError {
            return .init(items: [], error: e.message)
        } catch {
            return .init(items: [], error: error.localizedDescription)
        }
    }

    private func get<T: Decodable>(_ urlString: String, token: String) async throws -> T {
        guard let url = URL(string: urlString) else { throw ImportError("That server URL isn't valid.") }
        var req = URLRequest(url: url)
        req.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")
        req.setValue("application/json", forHTTPHeaderField: "Accept")
        let (data, resp) = try await URLSession.shared.data(for: req)
        if let http = resp as? HTTPURLResponse, !(200..<300).contains(http.statusCode) {
            switch http.statusCode {
            case 401, 403: throw ImportError("Access token was rejected (HTTP \(http.statusCode)).")
            case 404: throw ImportError("Server or vault not found (HTTP 404). Check the URL.")
            default: throw ImportError("1Password Connect returned HTTP \(http.statusCode).")
            }
        }
        return try JSONDecoder().decode(T.self, from: data)
    }

    private func map(_ item: OpItem) -> LoginItemInput {
        let username = item.fields.first { $0.purpose == "USERNAME" }?.value
        let password = item.fields.first { $0.purpose == "PASSWORD" }?.value
        let otp = item.fields.first { $0.type == "OTP" }?.value
        let website = item.urls.first { $0.primary }?.href ?? item.urls.first?.href
        let note = item.fields.first { $0.purpose == "NOTES" || $0.id == "notesPlain" }?.value

        let custom: [CustomFieldData] = item.fields
            .filter {
                !$0.value.isEmpty && !$0.label.isEmpty &&
                $0.purpose != "USERNAME" && $0.purpose != "PASSWORD" && $0.purpose != "NOTES" &&
                $0.type != "OTP" && $0.id != "notesPlain"
            }
            .map { CustomFieldData(name: $0.label, value: $0.value, secret: $0.type == "CONCEALED") }

        return LoginItemInput(
            title: item.title.isEmpty ? "Untitled" : item.title,
            description: nil,
            type: mapType(item.category),
            isFavorite: item.favorite,
            username: username,
            email: nil,
            website: website,
            loginUrl: nil,
            password: password,
            totpSecret: otp.map { Totp.secretFromUri($0) },
            notes: note,
            categoryName: friendlyCategory(item.category),
            customFields: custom
        )
    }

    private func mapType(_ category: String) -> ItemType {
        switch category.uppercased() {
        case "LOGIN": return .login
        case "PASSWORD": return .password
        case "SECURE_NOTE": return .secureNote
        case "CREDIT_CARD": return .creditCard
        case "WIRELESS_ROUTER": return .wifi
        case "IDENTITY": return .identity
        default: return .login
        }
    }

    private func friendlyCategory(_ category: String) -> String {
        switch category.uppercased() {
        case "LOGIN": return "Logins"
        case "PASSWORD": return "Passwords"
        case "SECURE_NOTE": return "Secure Notes"
        case "CREDIT_CARD": return "Credit Cards"
        case "WIRELESS_ROUTER": return "WiFi Networks"
        case "IDENTITY": return "Identities"
        default: return "Logins"
        }
    }

    struct ImportError: Error { let message: String; init(_ m: String) { message = m } }

    // MARK: - Connect REST DTOs (subset)
    // Custom decoders so a missing key (Connect omits empty fields) never fails the whole import.

    private struct OpVault: Decodable {
        var id = ""; var name = ""
        init(from d: Decoder) throws {
            let c = try d.container(keyedBy: K.self)
            id = try c.decodeIfPresent(String.self, forKey: .id) ?? ""
            name = try c.decodeIfPresent(String.self, forKey: .name) ?? ""
        }
        enum K: String, CodingKey { case id, name }
    }

    private struct OpItemSummary: Decodable {
        var id = ""; var category = ""
        init(from d: Decoder) throws {
            let c = try d.container(keyedBy: K.self)
            id = try c.decodeIfPresent(String.self, forKey: .id) ?? ""
            category = try c.decodeIfPresent(String.self, forKey: .category) ?? ""
        }
        enum K: String, CodingKey { case id, category }
    }

    private struct OpUrl: Decodable {
        var href = ""; var primary = false
        init(from d: Decoder) throws {
            let c = try d.container(keyedBy: K.self)
            href = try c.decodeIfPresent(String.self, forKey: .href) ?? ""
            primary = try c.decodeIfPresent(Bool.self, forKey: .primary) ?? false
        }
        enum K: String, CodingKey { case href, primary }
    }

    private struct OpField: Decodable {
        var id = ""; var type = ""; var purpose = ""; var label = ""; var value = ""
        init(from d: Decoder) throws {
            let c = try d.container(keyedBy: K.self)
            id = try c.decodeIfPresent(String.self, forKey: .id) ?? ""
            type = try c.decodeIfPresent(String.self, forKey: .type) ?? ""
            purpose = try c.decodeIfPresent(String.self, forKey: .purpose) ?? ""
            label = try c.decodeIfPresent(String.self, forKey: .label) ?? ""
            value = try c.decodeIfPresent(String.self, forKey: .value) ?? ""
        }
        enum K: String, CodingKey { case id, type, purpose, label, value }
    }

    private struct OpItem: Decodable {
        var id = ""; var title = ""; var category = ""; var favorite = false
        var urls: [OpUrl] = []
        var fields: [OpField] = []
        init(from d: Decoder) throws {
            let c = try d.container(keyedBy: K.self)
            id = try c.decodeIfPresent(String.self, forKey: .id) ?? ""
            title = try c.decodeIfPresent(String.self, forKey: .title) ?? ""
            category = try c.decodeIfPresent(String.self, forKey: .category) ?? ""
            favorite = try c.decodeIfPresent(Bool.self, forKey: .favorite) ?? false
            urls = try c.decodeIfPresent([OpUrl].self, forKey: .urls) ?? []
            fields = try c.decodeIfPresent([OpField].self, forKey: .fields) ?? []
        }
        enum K: String, CodingKey { case id, title, category, favorite, urls, fields }
    }
}
