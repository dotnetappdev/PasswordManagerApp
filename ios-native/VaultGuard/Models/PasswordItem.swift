import Foundation
import GRDB
import SwiftUI

// MARK: - Item Type

enum ItemType: String, Codable, CaseIterable {
    case login
    case creditCard
    case secureNote
    case wifi
    case passkey
    case identity
    case bankAccount

    var displayName: String {
        switch self {
        case .login: return "Login"
        case .creditCard: return "Credit Card"
        case .secureNote: return "Secure Note"
        case .wifi: return "Wi-Fi Password"
        case .passkey: return "Passkey"
        case .identity: return "Identity"
        case .bankAccount: return "Bank Account"
        }
    }

    var systemImage: String {
        switch self {
        case .login: return "person.fill.viewfinder"
        case .creditCard: return "creditcard.fill"
        case .secureNote: return "lock.doc.fill"
        case .wifi: return "wifi"
        case .passkey: return "key.fill"
        case .identity: return "person.text.rectangle.fill"
        case .bankAccount: return "building.columns.fill"
        }
    }

    var color: Color {
        switch self {
        case .login: return Color(hex: "7C3AED")
        case .creditCard: return Color(hex: "EC4899")
        case .secureNote: return Color(hex: "F59E0B")
        case .wifi: return Color(hex: "10B981")
        case .passkey: return Color(hex: "3B82F6")
        case .identity: return Color(hex: "8B5CF6")
        case .bankAccount: return Color(hex: "06B6D4")
        }
    }
}

// MARK: - Custom Field

struct CustomField: Codable, Identifiable {
    var id: UUID = UUID()
    var label: String
    var value: String
    var isSecure: Bool = false
}

// MARK: - Password Item

struct PasswordItem: Identifiable, Codable, FetchableRecord, PersistableRecord {
    var id: Int64?
    var title: String
    var type: ItemType
    var username: String?
    var encryptedPassword: String?      // AES-256-GCM encrypted, base64
    var website: String?
    var notes: String?
    var isFavorite: Bool = false
    var vaultId: Int64?
    var categoryId: Int64?
    var tagIds: String?                 // JSON array of tag IDs
    var createdAt: Date = .now
    var updatedAt: Date = .now
    var isDeleted: Bool = false
    var customFields: String?           // JSON encoded array of CustomField
    var syncId: String?                 // UUID for server sync
    var lastSyncedAt: Date?

    // Credit card specific
    var cardNumber: String?             // encrypted
    var cardExpiry: String?
    var cardCVV: String?                // encrypted
    var cardholderName: String?

    // Wi-Fi specific
    var networkName: String?
    var securityType: String?

    // Identity specific
    var firstName: String?
    var lastName: String?
    var email: String?
    var phone: String?
    var address: String?

    // Bank account specific
    var accountNumber: String?          // encrypted
    var routingNumber: String?
    var bankName: String?

    static let databaseTableName = "password_items"

    enum Columns: String, ColumnExpression {
        case id, title, type, username, encryptedPassword, website, notes
        case isFavorite, vaultId, categoryId, tagIds, createdAt, updatedAt
        case isDeleted, customFields, syncId, lastSyncedAt
        case cardNumber, cardExpiry, cardCVV, cardholderName
        case networkName, securityType
        case firstName, lastName, email, phone, address
        case accountNumber, routingNumber, bankName
    }

    // Transient property — decrypted at display time
    var decryptedPassword: String? {
        get {
            guard let enc = encryptedPassword else { return nil }
            return try? EncryptionService.shared.decrypt(enc)
        }
    }

    // Parsed custom fields
    var parsedCustomFields: [CustomField] {
        get {
            guard let json = customFields,
                  let data = json.data(using: .utf8),
                  let fields = try? JSONDecoder().decode([CustomField].self, from: data) else {
                return []
            }
            return fields
        }
        set {
            if let data = try? JSONEncoder().encode(newValue),
               let json = String(data: data, encoding: .utf8) {
                customFields = json
            }
        }
    }

    // Subtitle for list display
    var subtitle: String {
        switch type {
        case .login:
            return username ?? website ?? type.displayName
        case .creditCard:
            if let num = cardNumber, num.count >= 4 {
                return "•••• " + String(num.suffix(4))
            }
            return cardholderName ?? type.displayName
        case .secureNote:
            return notes.flatMap { String($0.prefix(40)) } ?? type.displayName
        case .wifi:
            return networkName ?? type.displayName
        case .passkey:
            return username ?? website ?? type.displayName
        case .identity:
            let name = [firstName, lastName].compactMap { $0 }.joined(separator: " ")
            return name.isEmpty ? type.displayName : name
        case .bankAccount:
            return bankName ?? type.displayName
        }
    }
}

// MARK: - Item Associations

extension PasswordItem {
    static let vault = belongsTo(Vault.self)
    static let category = belongsTo(Category.self)
}
