package com.vaultguard.app.data.local

import androidx.room.Dao
import androidx.room.Delete
import androidx.room.Insert
import androidx.room.OnConflictStrategy
import androidx.room.Query
import androidx.room.Update

@Dao
interface VaultDao {
    @Query("SELECT * FROM vault_items WHERE profileId = :profileId ORDER BY isFavorite DESC, title COLLATE NOCASE ASC")
    suspend fun getAll(profileId: String): List<VaultItemEntity>

    @Query("SELECT * FROM vault_items WHERE id = :id")
    suspend fun getById(id: Int): VaultItemEntity?

    @Query(
        "SELECT * FROM vault_items WHERE profileId = :profileId AND (title LIKE '%' || :term || '%' " +
            "OR username LIKE '%' || :term || '%' OR website LIKE '%' || :term || '%') " +
            "ORDER BY title COLLATE NOCASE ASC"
    )
    suspend fun search(profileId: String, term: String): List<VaultItemEntity>

    @Query("DELETE FROM vault_items WHERE profileId = :profileId")
    suspend fun deleteAllForProfile(profileId: String)

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insert(item: VaultItemEntity): Long

    @Update
    suspend fun update(item: VaultItemEntity)

    @Delete
    suspend fun delete(item: VaultItemEntity)

    @Query("DELETE FROM vault_items WHERE id = :id")
    suspend fun deleteById(id: Int)

    @Query("UPDATE vault_items SET vaultId = :toVaultId WHERE vaultId = :fromVaultId")
    suspend fun reassignVault(fromVaultId: Int, toVaultId: Int)
}
