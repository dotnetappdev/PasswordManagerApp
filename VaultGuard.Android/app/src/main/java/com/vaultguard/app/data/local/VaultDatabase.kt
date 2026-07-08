package com.vaultguard.app.data.local

import androidx.room.Database
import androidx.room.RoomDatabase

@Database(entities = [VaultItemEntity::class], version = 1, exportSchema = false)
abstract class VaultDatabase : RoomDatabase() {
    abstract fun vaultDao(): VaultDao
}
