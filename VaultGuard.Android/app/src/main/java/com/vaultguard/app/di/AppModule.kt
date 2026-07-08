package com.vaultguard.app.di

import android.content.Context
import androidx.room.Room
import com.vaultguard.app.data.local.VaultDao
import com.vaultguard.app.data.local.VaultDatabase
import dagger.Module
import dagger.Provides
import dagger.hilt.InstallIn
import dagger.hilt.android.qualifiers.ApplicationContext
import dagger.hilt.components.SingletonComponent
import javax.inject.Singleton

@Module
@InstallIn(SingletonComponent::class)
object AppModule {

    @Provides
    @Singleton
    fun provideVaultDatabase(@ApplicationContext context: Context): VaultDatabase =
        Room.databaseBuilder(context, VaultDatabase::class.java, "vaultguard-local.db")
            .fallbackToDestructiveMigration()
            .build()

    @Provides
    fun provideVaultDao(db: VaultDatabase): VaultDao = db.vaultDao()
}
