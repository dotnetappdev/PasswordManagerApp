package com.vaultguard.app.config

import android.content.Context
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.stringPreferencesKey
import androidx.datastore.preferences.preferencesDataStore
import dagger.hilt.android.qualifiers.ApplicationContext
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.flow.map
import kotlinx.serialization.Serializable
import kotlinx.serialization.decodeFromString
import kotlinx.serialization.encodeToString
import kotlinx.serialization.json.Json
import javax.inject.Inject
import javax.inject.Singleton

private val Context.accountsDataStore by preferencesDataStore(name = "vaultguard_accounts")

/** A saved account shown in the login account-switcher. The API key lives in [SecureStore] by id. */
@Serializable
data class Account(
    val id: String,
    val label: String,
    val email: String? = null,
    val mode: String = ConnectionMode.API.name,
    val apiBaseUrl: String = "",
) {
    val connectionMode: ConnectionMode get() = runCatching { ConnectionMode.valueOf(mode) }.getOrDefault(ConnectionMode.API)
    val initials: String
        get() = (label.ifBlank { email ?: "?" }).trim().take(1).uppercase()
}

/**
 * Stores the list of accounts a user can switch between on the login screen. Selecting an account
 * copies its connection settings into [ConfigStore] (the active target) and its key into [SecureStore].
 */
@Singleton
class AccountsStore @Inject constructor(
    @ApplicationContext private val context: Context,
    private val secureStore: SecureStore,
    private val configStore: ConfigStore,
) {
    private object Keys {
        val LIST = stringPreferencesKey("accounts_json")
        val CURRENT = stringPreferencesKey("current_account_id")
    }
    private val json = Json { ignoreUnknownKeys = true }

    val accounts: Flow<List<Account>> = context.accountsDataStore.data.map { prefs ->
        prefs[Keys.LIST]?.let { runCatching { json.decodeFromString<List<Account>>(it) }.getOrNull() } ?: emptyList()
    }

    val currentId: Flow<String?> = context.accountsDataStore.data.map { it[Keys.CURRENT] }

    suspend fun list(): List<Account> = accounts.first()

    /** Add or update an account (matched by id) and remember its API key. Marks it current. */
    suspend fun upsert(account: Account, apiKey: String?) {
        val current = list().filterNot { it.id == account.id } + account
        context.accountsDataStore.edit { prefs ->
            prefs[Keys.LIST] = json.encodeToString(current)
            prefs[Keys.CURRENT] = account.id
        }
        if (apiKey != null) secureStore.setAccountKey(account.id, apiKey)
    }

    /** Make the given account active: apply its connection config + key. */
    suspend fun select(id: String) {
        val account = list().firstOrNull { it.id == id } ?: return
        context.accountsDataStore.edit { it[Keys.CURRENT] = id }
        configStore.save(
            mode = account.connectionMode,
            apiBaseUrl = account.apiBaseUrl,
            apiKey = if (account.connectionMode == ConnectionMode.API) secureStore.getAccountKey(id) else null,
        )
    }

    suspend fun remove(id: String) {
        val remaining = list().filterNot { it.id == id }
        context.accountsDataStore.edit { prefs ->
            prefs[Keys.LIST] = json.encodeToString(remaining)
            if (prefs[Keys.CURRENT] == id) prefs.remove(Keys.CURRENT)
        }
        secureStore.setAccountKey(id, null)
    }
}
