package com.vaultguard.app.data.local

import androidx.room.Dao
import androidx.room.Delete
import androidx.room.Insert
import androidx.room.Query
import androidx.room.Update

@Dao
interface VaultsDao {
    @Query("SELECT * FROM vaults ORDER BY isDefault DESC, name COLLATE NOCASE ASC")
    suspend fun getAll(): List<VaultEntity>

    @Query("SELECT * FROM vaults WHERE id = :id")
    suspend fun getById(id: Int): VaultEntity?

    @Query("SELECT COUNT(*) FROM vaults")
    suspend fun count(): Int

    @Query("SELECT id FROM vaults WHERE isDefault = 1 LIMIT 1")
    suspend fun defaultVaultId(): Int?

    @Insert
    suspend fun insert(vault: VaultEntity): Long

    @Update
    suspend fun update(vault: VaultEntity)

    @Delete
    suspend fun delete(vault: VaultEntity)
}
