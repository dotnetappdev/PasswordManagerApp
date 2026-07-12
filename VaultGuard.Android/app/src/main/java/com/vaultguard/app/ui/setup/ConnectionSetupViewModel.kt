package com.vaultguard.app.ui.setup

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.vaultguard.app.config.ConfigStore
import com.vaultguard.app.config.ConnectionMode
import com.vaultguard.app.config.SecureStore
import com.vaultguard.app.data.remote.ApiProvider
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import javax.inject.Inject

data class ConnectionSetupState(
    val mode: ConnectionMode = ConnectionMode.API,
    val apiUrl: String = "",
    val apiKey: String = "",
    val testing: Boolean = false,
    val testMessage: String? = null,
    val testSuccess: Boolean? = null,
    val saved: Boolean = false,
)

@HiltViewModel
class ConnectionSetupViewModel @Inject constructor(
    private val configStore: ConfigStore,
    private val secureStore: SecureStore,
    private val apiProvider: ApiProvider,
) : ViewModel() {

    private val _state = MutableStateFlow(ConnectionSetupState())
    val state = _state.asStateFlow()

    init {
        viewModelScope.launch {
            val cfg = configStore.config.first()
            _state.update {
                it.copy(
                    mode = cfg.mode,
                    apiUrl = cfg.apiBaseUrl,
                    apiKey = secureStore.apiKey ?: "",
                )
            }
        }
    }

    /** Apply a scanned "Set Up Another Device" QR: fills API URL + key and saves. Returns true if valid. */
    fun applyScannedSetup(scanned: String): Boolean {
        val p = DeviceSetup.parse(scanned) ?: return false
        _state.update { it.copy(mode = ConnectionMode.API, apiUrl = p.url, apiKey = p.key) }
        save()
        return true
    }

    fun setMode(mode: ConnectionMode) = _state.update { it.copy(mode = mode, testMessage = null, testSuccess = null) }
    fun setUrl(url: String) = _state.update { it.copy(apiUrl = url, testMessage = null, testSuccess = null) }
    fun setKey(key: String) = _state.update { it.copy(apiKey = key, testMessage = null, testSuccess = null) }

    fun test() {
        val s = _state.value
        if (s.mode != ConnectionMode.API) return
        if (s.apiUrl.isBlank() || s.apiKey.isBlank()) {
            _state.update { it.copy(testSuccess = false, testMessage = "Enter both an API URL and API key.") }
            return
        }
        viewModelScope.launch {
            _state.update { it.copy(testing = true, testMessage = null, testSuccess = null) }
            val result = apiProvider.testConnection(s.apiUrl.trim(), s.apiKey.trim())
            _state.update {
                it.copy(
                    testing = false,
                    testSuccess = result.isSuccess,
                    testMessage = if (result.isSuccess) "Connection successful."
                    else result.exceptionOrNull()?.message ?: "Connection failed.",
                )
            }
        }
    }

    fun save() {
        val s = _state.value
        if (s.mode == ConnectionMode.API && (s.apiUrl.isBlank() || s.apiKey.isBlank())) {
            _state.update { it.copy(testSuccess = false, testMessage = "Enter both an API URL and API key.") }
            return
        }
        viewModelScope.launch {
            configStore.save(
                mode = s.mode,
                apiBaseUrl = s.apiUrl,
                apiKey = if (s.mode == ConnectionMode.API) s.apiKey else null,
            )
            _state.update { it.copy(saved = true) }
        }
    }
}
