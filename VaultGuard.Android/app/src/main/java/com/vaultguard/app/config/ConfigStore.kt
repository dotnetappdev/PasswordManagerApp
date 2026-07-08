package com.vaultguard.app.config

import android.content.Context
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.stringPreferencesKey
import androidx.datastore.preferences.preferencesDataStore
import dagger.hilt.android.qualifiers.ApplicationContext
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.map
import javax.inject.Inject
import javax.inject.Singleton

private val Context.dataStore by preferencesDataStore(name = "vaultguard_config")

/**
 * Persists non-secret connection settings (mode + API URL) in DataStore, and reflects secret presence
 * from [SecureStore]. This is the source of truth for the in-app connection setup GUI.
 */
@Singleton
class ConfigStore @Inject constructor(
    @ApplicationContext private val context: Context,
    private val secureStore: SecureStore,
) {
    private object Keys {
        val MODE = stringPreferencesKey("mode")
        val API_URL = stringPreferencesKey("api_url")
    }

    val config: Flow<ConnectionConfig> = context.dataStore.data.map { prefs ->
        val mode = prefs[Keys.MODE]?.let { runCatching { ConnectionMode.valueOf(it) }.getOrNull() }
            ?: ConnectionMode.API
        ConnectionConfig(
            mode = mode,
            apiBaseUrl = prefs[Keys.API_URL] ?: "",
            hasApiKey = !secureStore.apiKey.isNullOrBlank(),
        )
    }

    suspend fun save(mode: ConnectionMode, apiBaseUrl: String, apiKey: String?) {
        context.dataStore.edit { prefs ->
            prefs[Keys.MODE] = mode.name
            prefs[Keys.API_URL] = apiBaseUrl.trim()
        }
        if (apiKey != null) secureStore.apiKey = apiKey.trim()
    }

    /** True once the user has finished the connection setup at least once. */
    val isConfigured: Flow<Boolean> = config.map { c ->
        when (c.mode) {
            ConnectionMode.API -> c.isApiConfigured
            ConnectionMode.LOCAL -> true
        }
    }
}
