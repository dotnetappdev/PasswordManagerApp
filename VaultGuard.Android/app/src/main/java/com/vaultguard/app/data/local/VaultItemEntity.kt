package com.vaultguard.app.data.local

import androidx.room.Entity
import androidx.room.PrimaryKey

/**
 * A vault item stored in the on-device SQLite database (LOCAL connection mode). The password and TOTP
 * secret are stored encrypted (AES-GCM, base64 of iv+ciphertext) and only decrypted in memory on reveal.
 */
@Entity(tableName = "vault_items")
data class VaultItemEntity(
    @PrimaryKey(autoGenerate = true) val id: Int = 0,
    /** Which local profile (account) this item belongs to — isolates each profile's vault. */
    val profileId: String = "default",
    val title: String,
    val description: String?,
    val type: Int,
    val isFavorite: Boolean,
    val isArchived: Boolean = false,
    val isDeleted: Boolean = false,
    val vaultId: Int = 1,
    val username: String?,
    val email: String?,
    val website: String?,
    val loginUrl: String?,
    val notes: String?,
    val encPassword: String?,
    val encTotp: String?,
    val categoryName: String?,
    val tagsCsv: String?,
    /** User-defined custom fields, stored as a JSON array (see CustomFieldData). Null when none. */
    val customFieldsJson: String? = null,
    val createdAt: Long,
    val lastModified: Long,
)
