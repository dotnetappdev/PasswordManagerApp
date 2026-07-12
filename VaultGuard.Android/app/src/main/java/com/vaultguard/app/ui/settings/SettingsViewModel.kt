package com.vaultguard.app.ui.settings

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.vaultguard.app.config.AppSettings
import com.vaultguard.app.config.ConfigStore
import com.vaultguard.app.config.ConnectionConfig
import com.vaultguard.app.config.ConnectionMode
import com.vaultguard.app.config.LocalProfilesStore
import com.vaultguard.app.config.SecureStore
import com.vaultguard.app.config.SettingsStore
import com.vaultguard.app.data.remote.ApiProvider
import com.vaultguard.app.data.repo.SessionManager
import com.vaultguard.app.data.repo.VaultRepository
import com.vaultguard.app.domain.PasswordGenerator
import com.vaultguard.app.domain.PasswordOptions
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.stateIn
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltViewModel
class SettingsViewModel @Inject constructor(
    private val settingsStore: SettingsStore,
    private val configStore: ConfigStore,
    private val secureStore: SecureStore,
    private val apiProvider: ApiProvider,
    private val repository: VaultRepository,
    private val session: SessionManager,
    private val generator: PasswordGenerator,
    private val localProfiles: LocalProfilesStore,
) : ViewModel() {

    val settings = settingsStore.settings
        .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5_000), AppSettings())

    val config = configStore.config
        .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5_000), ConnectionConfig())

    private val _preview = MutableStateFlow("")
    val preview = _preview.asStateFlow()

    // Editable API configuration surfaced directly in the Storage tab.
    private val _apiMessage = MutableStateFlow<String?>(null)
    val apiMessage = _apiMessage.asStateFlow()

    fun currentApiKey(): String = secureStore.apiKey ?: ""

    fun saveApiConfig(mode: ConnectionMode, url: String, key: String) {
        viewModelScope.launch {
            configStore.save(mode, url, if (mode == ConnectionMode.API) key else null)
            _apiMessage.value = "Saved."
        }
    }

    fun testApiConfig(url: String, key: String) {
        viewModelScope.launch {
            _apiMessage.value = "Testing…"
            val result = apiProvider.testConnection(url.trim(), key.trim())
            _apiMessage.value = if (result.isSuccess) "Connection successful."
            else result.exceptionOrNull()?.message ?: "Connection failed."
        }
    }

    fun update(transform: (AppSettings) -> AppSettings) {
        viewModelScope.launch { settingsStore.update(transform) }
    }

    // Maintenance: seed/reset the local demo vault.
    private val _maintMessage = MutableStateFlow<String?>(null)
    val maintMessage = _maintMessage.asStateFlow()

    fun seedDemoData() {
        viewModelScope.launch {
            _maintMessage.value = "Seeding…"
            runCatching { repository.seedLocalDemo() }
                .onSuccess { _maintMessage.value = "Demo data seeded into the Personal vault." }
                .onFailure { _maintMessage.value = it.message ?: "Could not seed demo data." }
        }
    }

    fun resetLocalVault() {
        viewModelScope.launch {
            _maintMessage.value = "Clearing…"
            runCatching { repository.resetLocal() }
                .onSuccess { _maintMessage.value = "Local vault cleared." }
                .onFailure { _maintMessage.value = it.message ?: "Could not clear the vault." }
        }
    }

    /** Remove only the seeded demo items, keeping the user's own items, categories and account. */
    fun deleteSeedData() {
        viewModelScope.launch {
            _maintMessage.value = "Deleting seed data…"
            runCatching { repository.deleteSeedData() }
                .onSuccess { removed ->
                    _maintMessage.value = if (removed > 0)
                        "Removed $removed demo item(s). Your own items, categories and account were kept."
                    else "No demo items to remove."
                }
                .onFailure { _maintMessage.value = it.message ?: "Could not delete seed data." }
        }
    }

    /**
     * Run a Maintenance delete for the independently-chosen options — they can be combined (seed data +
     * accounts, or just seed data while keeping accounts). Where selections overlap the most destructive
     * one wins. If [accounts] is chosen the session is locked and [onReset] is invoked so the UI can
     * return to the unlock / account-picker screen.
     */
    fun runDelete(
        seedData: Boolean,
        allItems: Boolean,
        accounts: Boolean,
        onReset: () -> Unit = {},
    ) {
        if (!seedData && !allItems && !accounts) return
        viewModelScope.launch {
            _maintMessage.value = "Working…"
            runCatching {
                when {
                    accounts -> {
                        clearAllAccounts(); clearAppSecrets()
                        "All local accounts, their vault items and saved keys were removed."
                    }
                    allItems -> {
                        repository.resetLocal()
                        "All items on this device were removed. Your accounts were kept."
                    }
                    else -> {
                        val n = repository.deleteSeedData()
                        if (n > 0) "Removed $n demo item(s). Your own items and accounts were kept."
                        else "No demo items to remove."
                    }
                }
            }.onSuccess { msg ->
                _maintMessage.value = msg
                if (accounts) { session.lock(); onReset() }
            }.onFailure { _maintMessage.value = it.message ?: "Delete failed." }
        }
    }

    /** Wipe every local account (profile) plus its keys and items. */
    private suspend fun clearAllAccounts() {
        localProfiles.list().forEach { secureStore.clearLocalProfile(it.id) }
        repository.wipeAllLocalItems()
        localProfiles.clearAll()
    }

    /** Clear app-level secrets for a full factory reset. */
    private fun clearAppSecrets() {
        secureStore.appTotpSecret = null
        secureStore.clearQuickUnlock()
        secureStore.apiKey = null
        _appTotpEnabled.value = false
    }

    fun generatePreview() {
        val s = settings.value
        _preview.value = generator.generate(
            PasswordOptions(
                length = s.pwLength,
                upper = s.pwUpper,
                lower = s.pwLower,
                digits = s.pwDigits,
                symbols = s.pwSymbols,
                avoidAmbiguous = s.pwAvoidAmbiguous,
            )
        )
    }

    fun lock() = session.lock()

    // ---- App authenticator (TOTP) 2FA setup --------------------------------

    data class TotpSetup(val secret: String, val otpauthUri: String)

    private val _appTotpEnabled = MutableStateFlow(secureStore.appTotpSecret != null)
    val appTotpEnabled = _appTotpEnabled.asStateFlow()

    private val _totpSetup = MutableStateFlow<TotpSetup?>(null)
    val totpSetup = _totpSetup.asStateFlow()

    fun startTotpSetup() {
        val secret = com.vaultguard.app.domain.Totp.randomSecret()
        val account = session.user.value?.email ?: "vault"
        _totpSetup.value = TotpSetup(secret, com.vaultguard.app.domain.Totp.otpauthUri(secret, account))
    }

    fun cancelTotpSetup() { _totpSetup.value = null }

    /** Verify the setup code; on success app 2FA is enabled. Returns success for the UI. */
    fun confirmTotpSetup(code: String): Boolean {
        val secret = _totpSetup.value?.secret ?: return false
        return if (com.vaultguard.app.domain.Totp.verify(secret, code)) {
            secureStore.appTotpSecret = secret
            _appTotpEnabled.value = true
            _totpSetup.value = null
            true
        } else false
    }

    fun disableAppTotp() {
        secureStore.appTotpSecret = null
        _appTotpEnabled.value = false
    }
}
