package com.vaultguard.app.ui.home

import com.vaultguard.app.data.model.ItemType
import com.vaultguard.app.data.model.VaultItem

/** The selectable sidebar sections, mirroring the WPF navigation exactly. */
sealed class VaultSection(val title: String) {
    data object AllItems : VaultSection("All Items")
    data object Favorites : VaultSection("Favourites")
    data object Archive : VaultSection("Archive")
    data object RecentlyDeleted : VaultSection("Recently Deleted")
    data class Category(val type: ItemType, val label: String) : VaultSection(label)

    fun matches(item: VaultItem): Boolean = when (this) {
        AllItems -> !item.isArchived && !item.isDeleted
        Favorites -> item.isFavorite && !item.isArchived && !item.isDeleted
        Archive -> item.isArchived && !item.isDeleted
        RecentlyDeleted -> item.isDeleted
        is Category -> item.type == type && !item.isArchived && !item.isDeleted
    }

    companion object {
        // The five category filters shown in the WPF "Categories" group.
        val categories = listOf(
            Category(ItemType.Login, "Logins"),
            Category(ItemType.CreditCard, "Credit Cards"),
            Category(ItemType.SecureNote, "Secure Notes"),
            Category(ItemType.WiFi, "Wi-Fi"),
            Category(ItemType.Identity, "Identities"),
        )
    }
}
