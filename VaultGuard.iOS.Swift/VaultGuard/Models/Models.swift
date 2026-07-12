// Models.swift — Codable DTOs mirroring the VaultGuard API wire contract + presentation models.
import Foundation

enum ItemType: Int, Codable, CaseIterable {
    case login = 1, creditCard = 2, secureNote = 3, wifi = 4, password = 5, passkey = 6, identity = 7

    var label: String {
        switch self {
        case .login: return "Login"
        case .creditCard: return "Credit Card"
        case .secureNote: return "Secure Note"
        case .wifi: return "Wi-Fi"
        case .password: return "Password"
        case .passkey: return "Passkey"
        case .identity: return "Identity"
        }
    }

    var systemImage: String {
        switch self {
        case .login: return "person.crop.circle"
        case .creditCard: return "creditcard"
        case .secureNote: return "note.text"
        case .wifi: return "wifi"
        case .password: return "key"
        case .passkey: return "person.badge.key"
        case .identity: return "person.text.rectangle"
        }
    }

    static func from(_ code: Int) -> ItemType { ItemType(rawValue: code) ?? .login }
}

// MARK: - Auth

struct EnhancedLoginRequest: Encodable {
    let email: String
    let password: String
    var twoFactorCode: String?
    var isTwoFactorBackupCode: Bool = false
}

struct LoginResponse: Decodable {
    var requiresTwoFactor: Bool = false
    var supportsPasskey: Bool = false
    var twoFactorToken: String?
    var authResponse: AuthResponse?
}

struct AuthResponse: Decodable {
    var token: String = ""
    var user: UserDto?
}

struct UserDto: Codable {
    var id: String = ""
    var email: String = ""
    var firstName: String?
    var lastName: String?
}

// MARK: - Items

struct PasswordItemDto: Decodable {
    var id: Int = 0
    var title: String = ""
    var description: String?
    var type: Int = 1
    var isFavorite: Bool = false
    var isArchived: Bool = false
    var isDeleted: Bool = false
    var categoryId: Int?
    var category: CategoryDto?
    var loginItem: LoginItemDto?
    var tags: [TagDto] = []
    var createdAt: String?
}

struct LoginItemDto: Decodable {
    var username: String?
    var email: String?
    var website: String?
    var loginUrl: String?
    var totpSecret: String?
    var notes: String?
    var password: String?
}

struct CategoryDto: Codable, Identifiable {
    var id: Int = 0
    var name: String = ""
    var color: String?
}

struct TagDto: Codable { var id: Int = 0; var name: String = ""; var color: String? }

struct VaultDto: Codable, Identifiable {
    var id: Int = 0
    var name: String = ""
    var description: String?
    var color: String?
    var icon: String?
    var itemCount: Int = 0
    var isDefault: Bool = false
}

// Create (server-side encryption)
struct CreateEncryptedPasswordItem: Encodable {
    let title: String
    var description: String?
    var type: Int = 1
    var isFavorite: Bool = false
    let masterPassword: String
    var loginItem: CreateLoginItem?
    var tagIds: [Int] = []
}

struct CreateLoginItem: Encodable {
    var website: String?
    var username: String?
    var email: String?
    var password: String?
    var totpSecret: String?
    var loginUrl: String?
    var notes: String?
}

struct UpdatePasswordItem: Encodable {
    var title: String?
    var description: String?
    var isFavorite: Bool?
    var isArchived: Bool = false
    var loginItem: CreateLoginItem?
    var tagIds: [Int] = []
}

struct RevealPasswordRequest: Encodable { let masterPassword: String }
struct RevealPasswordResponse: Decodable { var password: String = "" }

struct DecryptedPasswordItemDto: Decodable { var loginItem: DecryptedLoginItemDto? }
struct DecryptedLoginItemDto: Decodable {
    var username: String?
    var password: String?
    var email: String?
    var website: String?
    var totpSecret: String?
    var loginUrl: String?
    var notes: String?
}

// MARK: - Presentation

/// A user-defined custom field (1Password-style). `secret` fields are masked in the UI.
/// In LOCAL mode these are persisted as a JSON array on the item row.
struct CustomFieldData: Codable, Equatable {
    var name: String
    var value: String
    var secret: Bool = false
    /// WPF `CustomFieldType` code (1=Text … 13=SignInWith). Stored as an Int so the JSON stays
    /// forward/backward compatible with older items that predate typed custom fields.
    var type: Int = CustomFieldType.text.code

    private enum CodingKeys: String, CodingKey { case name, value, secret, type }

    init(name: String, value: String, secret: Bool = false, type: Int = CustomFieldType.text.code) {
        self.name = name; self.value = value; self.secret = secret; self.type = type
    }

    init(from decoder: Decoder) throws {
        let c = try decoder.container(keyedBy: CodingKeys.self)
        name = try c.decode(String.self, forKey: .name)
        value = try c.decode(String.self, forKey: .value)
        secret = (try? c.decode(Bool.self, forKey: .secret)) ?? false
        type = (try? c.decode(Int.self, forKey: .type)) ?? CustomFieldType.text.code
    }

    var fieldType: CustomFieldType { CustomFieldType(code: type) }
    /// Password/OTP values (or an explicit secret flag) are masked in the UI.
    var isMasked: Bool { secret || fieldType.isSecret }
}

/// Mirrors the desktop `VaultGuard.Models.CustomFieldType` so iOS custom fields offer the exact same
/// field types as the WPF, Blazor and Android forms (full CRUD parity).
enum CustomFieldType: Int, CaseIterable, Identifiable {
    case text = 1, password = 2, date = 3, number = 4, email = 5, url = 6,
         textArea = 7, phone = 8, file = 9, toggle = 10, address = 11,
         oneTimePassword = 12, signInWith = 13

    var id: Int { rawValue }
    var code: Int { rawValue }
    init(code: Int) { self = CustomFieldType(rawValue: code) ?? .text }

    var label: String {
        switch self {
        case .text: return "Text"
        case .password: return "Password"
        case .date: return "Date"
        case .number: return "Number"
        case .email: return "Email"
        case .url: return "URL"
        case .textArea: return "Text area"
        case .phone: return "Phone"
        case .file: return "File"
        case .toggle: return "Toggle (Yes/No)"
        case .address: return "Address"
        case .oneTimePassword: return "One-Time Password"
        case .signInWith: return "Sign in with"
        }
    }

    /// Password and OTP values are always masked regardless of the per-field secret flag.
    var isSecret: Bool { self == .password || self == .oneTimePassword }
    /// Multi-line fields (address / free-text notes).
    var isMultiline: Bool { self == .textArea || self == .address }
}

struct VaultItem: Identifiable {
    let id: Int
    var title: String
    var description: String?
    var type: ItemType
    var isFavorite: Bool
    var isArchived: Bool
    var isDeleted: Bool
    var username: String?
    var email: String?
    var website: String?
    var loginUrl: String?
    var notes: String?
    var categoryName: String?
    var tags: [String] = []
    var customFields: [CustomFieldData] = []
    /// Epoch seconds the item was created; used to group items by month in quick-access/search UI.
    var createdAt: Double = 0

    static func from(_ dto: PasswordItemDto) -> VaultItem {
        VaultItem(
            id: dto.id, title: dto.title, description: dto.description,
            type: ItemType.from(dto.type), isFavorite: dto.isFavorite,
            isArchived: dto.isArchived, isDeleted: dto.isDeleted,
            username: dto.loginItem?.username, email: dto.loginItem?.email,
            website: dto.loginItem?.website, loginUrl: dto.loginItem?.loginUrl,
            notes: dto.loginItem?.notes, categoryName: dto.category?.name,
            tags: dto.tags.map { $0.name },
            createdAt: dto.createdAt.flatMap { ISO8601DateFormatter().date(from: $0) }?.timeIntervalSince1970 ?? 0
        )
    }
}

struct ItemSecret { var password: String?; var totpSecret: String? }

struct LoginItemInput {
    var title: String
    var description: String?
    var type: ItemType = .login
    var isFavorite = false
    var username: String?
    var email: String?
    var website: String?
    var loginUrl: String?
    var password: String?
    var totpSecret: String?
    var notes: String?
    var categoryName: String?
    var customFields: [CustomFieldData] = []
}
