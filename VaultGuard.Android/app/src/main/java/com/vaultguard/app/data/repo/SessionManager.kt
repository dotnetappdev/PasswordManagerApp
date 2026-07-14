package com.vaultguard.app.data.repo

import com.vaultguard.app.config.SecureStore
import com.vaultguard.app.data.model.UserDto
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import javax.inject.Inject
import javax.inject.Singleton

/**
 * Holds the unlocked-session state. The master password is kept in memory only (never persisted) and is
 * required for reveal/create operations. The API session token is persisted (encrypted) so the app can
 * resume without re-login until it expires.
 */
@Singleton
class SessionManager @Inject constructor(
    private val secureStore: SecureStore,
) {
    private val _user = MutableStateFlow<UserDto?>(null)
    val user: StateFlow<UserDto?> = _user.asStateFlow()

    private val _unlocked = MutableStateFlow(false)
    val unlocked: StateFlow<Boolean> = _unlocked.asStateFlow()

    /** In-memory only. Cleared on lock. */
    @Volatile
    var masterPassword: String? = null
        private set

    /** The active LOCAL profile (account) whose vault is unlocked. "default" for legacy/single-vault use. */
    @Volatile
    var activeProfileId: String = "default"
        private set

    var sessionToken: String?
        get() = secureStore.sessionToken
        private set(value) { secureStore.sessionToken = value }

    /**
     * [masterPassword] is null for a real passkey sign-in: WebAuthn proves identity to the server but
     * (correctly, for a zero-knowledge vault) never yields the master password, so features that need it
     * client-side (password reveal, QR hand-off) require the user to enter it separately in that flow.
     */
    fun onLoggedIn(token: String, user: UserDto?, masterPassword: String?) {
        this.sessionToken = token
        this.masterPassword = masterPassword
        _user.value = user
        _unlocked.value = true
    }

    /** LOCAL mode unlock: no server token, just the in-memory master password + active profile. */
    fun onLocalUnlocked(masterPassword: String, profileId: String = "default") {
        this.masterPassword = masterPassword
        this.activeProfileId = profileId
        _unlocked.value = true
    }

    fun lock() {
        masterPassword = null
        _unlocked.value = false
        secureStore.clearSession()
    }
}
