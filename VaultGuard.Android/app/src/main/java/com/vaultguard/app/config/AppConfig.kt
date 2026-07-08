package com.vaultguard.app.config

/** How the app reaches vault data. */
enum class ConnectionMode {
    /** Talk to the VaultGuard REST API using an API URL + API key. */
    API,

    /** Store the vault in a local SQLite (Room) database on the device, no server. */
    LOCAL,
}

/**
 * User-editable connection settings. The API key itself is a secret and is NOT held here — it lives in
 * [SecureStore] (EncryptedSharedPreferences); this object only reports whether one is present.
 */
data class ConnectionConfig(
    val mode: ConnectionMode = ConnectionMode.API,
    val apiBaseUrl: String = "",
    val hasApiKey: Boolean = false,
) {
    val isApiConfigured: Boolean
        get() = mode == ConnectionMode.API && apiBaseUrl.isNotBlank() && hasApiKey

    /** Base URL guaranteed to end with a single trailing slash (Retrofit requirement). */
    val normalizedBaseUrl: String
        get() = if (apiBaseUrl.isBlank()) apiBaseUrl
        else if (apiBaseUrl.endsWith("/")) apiBaseUrl else "$apiBaseUrl/"
}
