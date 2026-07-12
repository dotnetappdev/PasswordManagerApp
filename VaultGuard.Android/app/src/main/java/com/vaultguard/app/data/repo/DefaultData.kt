package com.vaultguard.app.data.repo

import com.vaultguard.app.data.model.CategoryDto
import com.vaultguard.app.data.model.VaultDto

/**
 * The default vault + categories, matching VaultGuard.DAL.Seed.IdentityDataSeeder exactly so the mobile
 * apps present the same "Personal" vault and category set as the WPF/Web apps.
 */
object DefaultData {

    const val DEFAULT_VAULT_ID = 1
    const val DEFAULT_VAULT_NAME = "Personal"

    val personalVault = VaultDto(
        id = DEFAULT_VAULT_ID,
        name = DEFAULT_VAULT_NAME,
        description = "Your personal password vault",
        icon = "🔐",
        isDefault = true,
    )

    // The full category set the desktop/web apps seed (VaultGuard.DAL.Seed.TestDataSeeder.SeedCategories),
    // with matching names + colors, so the mobile category list mirrors the WPF version exactly.
    val categories: List<CategoryDto> = listOf(
        CategoryDto(id = 1, name = "Logins", color = "#3b82f6"),
        CategoryDto(id = 2, name = "Secure Notes", color = "#f59e0b"),
        CategoryDto(id = 3, name = "Credit Cards", color = "#10b981"),
        CategoryDto(id = 4, name = "Identities", color = "#10b981"),
        CategoryDto(id = 5, name = "Passwords", color = "#06b6d4"),
        CategoryDto(id = 6, name = "Documents", color = "#3b82f6"),
        CategoryDto(id = 7, name = "SSH Keys", color = "#f59e0b"),
        CategoryDto(id = 8, name = "API Credentials", color = "#06b6d4"),
        CategoryDto(id = 9, name = "Bank Accounts", color = "#f59e0b"),
        CategoryDto(id = 10, name = "Crypto Wallets", color = "#8b5cf6"),
        CategoryDto(id = 11, name = "Databases", color = "#6b7280"),
        CategoryDto(id = 12, name = "Driver Licenses", color = "#ec4899"),
        CategoryDto(id = 13, name = "Emails", color = "#ec4899"),
        CategoryDto(id = 14, name = "Medical Records", color = "#ef4444"),
        CategoryDto(id = 15, name = "Memberships", color = "#8b5cf6"),
        CategoryDto(id = 16, name = "Outdoor Licenses", color = "#10b981"),
        CategoryDto(id = 17, name = "Passports", color = "#3b82f6"),
        CategoryDto(id = 18, name = "Rewards", color = "#ec4899"),
        CategoryDto(id = 19, name = "Servers", color = "#6b7280"),
        CategoryDto(id = 20, name = "Social Security Numbers", color = "#3b82f6"),
        CategoryDto(id = 21, name = "Software Licenses", color = "#3b82f6"),
        CategoryDto(id = 22, name = "Wireless Routers", color = "#06b6d4"),
        CategoryDto(id = 23, name = "WiFi Networks", color = "#06b6d4"),
        CategoryDto(id = 24, name = "Passkeys", color = "#ec4899"),
    )
}
