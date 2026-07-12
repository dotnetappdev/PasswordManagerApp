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

    // Per-profile LOCAL salt + verifier so each local account (profile) has its own encrypted vault.
    fun localSalt(profileId: String): String? = prefs.getString("${KEY_LOCAL_SALT}_$profileId", null)
    fun setLocalSalt(profileId: String, value: String?) = prefs.edit().apply {
        if (value.isNullOrBlank()) remove("${KEY_LOCAL_SALT}_$profileId") else putString("${KEY_LOCAL_SALT}_$profileId", value)
    }.apply()

    fun localVerifier(profileId: String): String? = prefs.getString("${KEY_LOCAL_VERIFIER}_$profileId", null)
    fun setLocalVerifier(profileId: String, value: String?) = prefs.edit().apply {
        if (value.isNullOrBlank()) remove("${KEY_LOCAL_VERIFIER}_$profileId") else putString("${KEY_LOCAL_VERIFIER}_$profileId", value)
    }.apply()

    fun clearLocalProfile(profileId: String) {
        prefs.edit()
            .remove("${KEY_LOCAL_SALT}_$profileId")
            .remove("${KEY_LOCAL_VERIFIER}_$profileId")
            .apply()
    }

    /** App-level authenticator (TOTP) 2FA secret (base32). Non-null = app 2FA is enabled. */
    var appTotpSecret: String?
        get() = prefs.getString(KEY_APP_TOTP, null)
        set(value) = prefs.edit().apply {
            if (value.isNullOrBlank()) remove(KEY_APP_TOTP) else putString(KEY_APP_TOTP, value)
        }.apply()

    /**
     * The master password cached for quick unlock (fingerprint / passcode). Stored in the Keystore-backed
     * encrypted prefs and only ever read after a successful biometric prompt or a verified passcode, so the
     * user doesn't have to retype the master key every time.
     */
    var quickUnlockMaster: String?
        get() = prefs.getString(KEY_QUICK_MASTER, null)
        set(value) = prefs.edit().apply {
            if (value.isNullOrBlank()) remove(KEY_QUICK_MASTER) else putString(KEY_QUICK_MASTER, value)
        }.apply()

    /** Numeric passcode stored as "saltB64:hashB64" (PBKDF2). Non-null = a passcode is set. */
    var passcode: String?
        get() = prefs.getString(KEY_PASSCODE, null)
        set(value) = prefs.edit().apply {
            if (value.isNullOrBlank()) remove(KEY_PASSCODE) else putString(KEY_PASSCODE, value)
        }.apply()

    /** Saved 1Password Connect server URL for the API importer (encrypted at rest). */
    var onePasswordHost: String?
        get() = prefs.getString(KEY_1P_HOST, null)
        set(value) = prefs.edit().apply {
            if (value.isNullOrBlank()) remove(KEY_1P_HOST) else putString(KEY_1P_HOST, value)
        }.apply()

    /** Saved 1Password Connect access token / Service-Account token (encrypted at rest). */
    var onePasswordToken: String?
        get() = prefs.getString(KEY_1P_TOKEN, null)
        set(value) = prefs.edit().apply {
            if (value.isNullOrBlank()) remove(KEY_1P_TOKEN) else putString(KEY_1P_TOKEN, value)
        }.apply()

    val hasQuickUnlock: Boolean get() = !quickUnlockMaster.isNullOrBlank()
    val hasPasscode: Boolean get() = !passcode.isNullOrBlank()

    /** Forget the cached master password + passcode (e.g. on sign-out or "disable quick unlock"). */
    fun clearQuickUnlock() {
        prefs.edit().remove(KEY_QUICK_MASTER).remove(KEY_PASSCODE).apply()
    }

    // Per-account API keys for the account switcher (keyed by account id).
    fun setAccountKey(accountId: String, key: String?) = prefs.edit().apply {
        if (key.isNullOrBlank()) remove("acct_key_$accountId") else putString("acct_key_$accountId", key)
    }.apply()

    fun getAccountKey(accountId: String): String? = prefs.getString("acct_key_$accountId", null)

    fun clearSession() {
        prefs.edit().remove(KEY_SESSION).apply()
    }

    private companion object {
        const val KEY_API_KEY = "api_key"
        const val KEY_SESSION = "session_token"
        const val KEY_LOCAL_SALT = "local_vault_salt"
        const val KEY_LOCAL_VERIFIER = "local_verifier"
        const val KEY_APP_TOTP = "app_totp_secret"
        const val KEY_QUICK_MASTER = "quick_unlock_master"
        const val KEY_PASSCODE = "quick_unlock_passcode"
        const val KEY_1P_HOST = "onepassword_connect_host"
        const val KEY_1P_TOKEN = "onepassword_connect_token"
    }
}
