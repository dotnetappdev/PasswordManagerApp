package com.vaultguard.app.data.local

import androidx.room.Database
import androidx.room.RoomDatabase

@Database(entities = [VaultItemEntity::class, VaultEntity::class], version = 4, exportSchema = false)
abstract class VaultDatabase : RoomDatabase() {
    abstract fun vaultDao(): VaultDao
    abstract fun vaultsDao(): VaultsDao
}
