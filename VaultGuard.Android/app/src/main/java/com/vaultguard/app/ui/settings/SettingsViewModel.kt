package com.vaultguard.app.ui.settings

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.vaultguard.app.config.AppSettings
import com.vaultguard.app.config.ConfigStore
import com.vaultguard.app.config.ConnectionConfig
import com.vaultguard.app.config.ConnectionMode
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
