// DefaultData.swift — default vault + categories, matching VaultGuard.DAL.Seed.IdentityDataSeeder.
import Foundation

enum DefaultData {
    static let defaultVaultId = 1

    static let personalVault = VaultDto(
        id: defaultVaultId, name: "Personal",
        description: "Your personal password vault", icon: "🔐", isDefault: true
    )

    // The same six categories the desktop/web apps create for the Personal vault.
    static let categories: [CategoryDto] = [
        CategoryDto(id: 1, name: "Logins", color: "#4A90E2"),
        CategoryDto(id: 2, name: "Credit Cards", color: "#E94B3C"),
        CategoryDto(id: 3, name: "Secure Notes", color: "#F5A623"),
        CategoryDto(id: 4, name: "WiFi Networks", color: "#7ED321"),
        CategoryDto(id: 5, name: "Passkeys", color: "#9013FE"),
        CategoryDto(id: 6, name: "Identities", color: "#50E3C2"),
    ]
}
