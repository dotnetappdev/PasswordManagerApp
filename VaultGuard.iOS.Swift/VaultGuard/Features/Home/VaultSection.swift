// VaultSection.swift — the sidebar sections, mirroring the WPF navigation.
import Foundation

enum VaultSection: Hashable, Identifiable {
    case allItems, favorites, archive, recentlyDeleted
    case category(ItemType)

    var id: String { title }

    var title: String {
        switch self {
        case .allItems: return "All Items"
        case .favorites: return "Favourites"
        case .archive: return "Archive"
        case .recentlyDeleted: return "Recently Deleted"
        case .category(let t):
            switch t {
            case .login: return "Logins"
            case .creditCard: return "Credit Cards"
            case .secureNote: return "Secure Notes"
            case .wifi: return "Wi-Fi"
            case .identity: return "Identities"
            case .passkey: return "Passkeys"
            default: return t.label
            }
        }
    }

    var systemImage: String {
        switch self {
        case .allItems: return "square.stack.3d.up"
        case .favorites: return "star"
        case .archive: return "archivebox"
        case .recentlyDeleted: return "trash"
        case .category(let t): return t.systemImage
        }
    }

    func matches(_ item: VaultItem) -> Bool {
        switch self {
        case .allItems: return !item.isArchived && !item.isDeleted
        case .favorites: return item.isFavorite && !item.isArchived && !item.isDeleted
        case .archive: return item.isArchived && !item.isDeleted
        case .recentlyDeleted: return item.isDeleted
        case .category(let t): return item.type == t && !item.isArchived && !item.isDeleted
        }
    }

    static let categories: [VaultSection] = [
        .category(.login), .category(.creditCard), .category(.secureNote),
        .category(.wifi), .category(.identity), .category(.passkey),
    ]
}
