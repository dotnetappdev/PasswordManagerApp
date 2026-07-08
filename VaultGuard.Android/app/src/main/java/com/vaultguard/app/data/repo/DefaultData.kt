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

    // Same six categories the desktop/web apps create for the Personal vault.
    val categories: List<CategoryDto> = listOf(
        CategoryDto(id = 1, name = "Logins", color = "#4A90E2"),
        CategoryDto(id = 2, name = "Credit Cards", color = "#E94B3C"),
        CategoryDto(id = 3, name = "Secure Notes", color = "#F5A623"),
        CategoryDto(id = 4, name = "WiFi Networks", color = "#7ED321"),
        CategoryDto(id = 5, name = "Passkeys", color = "#9013FE"),
        CategoryDto(id = 6, name = "Identities", color = "#50E3C2"),
    )
}
