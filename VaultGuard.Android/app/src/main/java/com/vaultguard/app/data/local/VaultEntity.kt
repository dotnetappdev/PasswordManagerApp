package com.vaultguard.app.data.local

import androidx.room.Entity
import androidx.room.PrimaryKey

/** A vault stored in the on-device SQLite database (LOCAL mode). */
@Entity(tableName = "vaults")
data class VaultEntity(
    @PrimaryKey(autoGenerate = true) val id: Int = 0,
    val name: String,
    val description: String?,
    val color: String?,
    val icon: String?,
    val isDefault: Boolean = false,
    val createdAt: Long = System.currentTimeMillis(),
)
