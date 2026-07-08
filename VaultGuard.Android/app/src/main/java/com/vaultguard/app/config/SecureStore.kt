package com.vaultguard.app.config

import android.content.Context
import androidx.security.crypto.EncryptedSharedPreferences
import androidx.security.crypto.MasterKey
import dagger.hilt.android.qualifiers.ApplicationContext
import javax.inject.Inject
import javax.inject.Singleton

/**
 * AES-256 encrypted key/value store (Android Keystore-backed) for secrets: the API key and the
 * cached session token. Never store the master password here — it is kept in memory only.
 */
@Singleton
class SecureStore @Inject constructor(
    @ApplicationContext context: Context,
) {
    private val prefs by lazy {
        val masterKey = MasterKey.Builder(context)
            .setKeyScheme(MasterKey.KeyScheme.AES256_GCM)
            .build()
        EncryptedSharedPreferences.create(
            context,
            "vaultguard_secure",
            masterKey,
            EncryptedSharedPreferences.PrefKeyEncryptionScheme.AES256_SIV,
            EncryptedSharedPreferences.PrefValueEncryptionScheme.AES256_GCM,
        )
    }

    var apiKey: String?
        get() = prefs.getString(KEY_API_KEY, null)
        set(value) = prefs.edit().apply {
            if (value.isNullOrBlank()) remove(KEY_API_KEY) else putString(KEY_API_KEY, value)
        }.apply()

    var sessionToken: String?
        get() = prefs.getString(KEY_SESSION, null)
        set(value) = prefs.edit().apply {
            if (value.isNullOrBlank()) remove(KEY_SESSION) else putString(KEY_SESSION, value)
        }.apply()

    /** Per-vault key-derivation salt for LOCAL mode (base64). Created on first local vault use. */
    var localVaultSalt: String?
        get() = prefs.getString(KEY_LOCAL_SALT, null)
        set(value) = prefs.edit().apply {
            if (value.isNullOrBlank()) remove(KEY_LOCAL_SALT) else putString(KEY_LOCAL_SALT, value)
        }.apply()

    /** Encrypted verifier blob used to check the master password on LOCAL unlock. */
    var localVerifier: String?
        get() = prefs.getString(KEY_LOCAL_VERIFIER, null)
        set(value) = prefs.edit().apply {
            if (value.isNullOrBlank()) remove(KEY_LOCAL_VERIFIER) else putString(KEY_LOCAL_VERIFIER, value)
        }.apply()

    fun clearSession() {
        prefs.edit().remove(KEY_SESSION).apply()
    }

    private companion object {
        const val KEY_API_KEY = "api_key"
        const val KEY_SESSION = "session_token"
        const val KEY_LOCAL_SALT = "local_vault_salt"
        const val KEY_LOCAL_VERIFIER = "local_verifier"
    }
}
