package com.vaultguard.app.ui.browse

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.vaultguard.app.config.SecureStore
import com.vaultguard.app.data.remote.OnePasswordConnectImporter
import com.vaultguard.app.data.repo.VaultRepository
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltViewModel
class OnePasswordImportViewModel @Inject constructor(
    private val importer: OnePasswordConnectImporter,
    private val repository: VaultRepository,
    private val secureStore: SecureStore,
) : ViewModel() {

    data class UiState(
        val host: String = "",
        val token: String = "",
        val remember: Boolean = true,
        val running: Boolean = false,
        val imported: Int = 0,
        val total: Int = 0,
        val done: Boolean = false,
        val error: String? = null,
    )

    // Prefill from the saved connection so the user doesn't retype it each time.
    private val _state = MutableStateFlow(
        UiState(
            host = secureStore.onePasswordHost.orEmpty(),
            token = secureStore.onePasswordToken.orEmpty(),
        )
    )
    val state = _state.asStateFlow()

    fun setHost(v: String) = _state.update { it.copy(host = v, error = null, done = false) }
    fun setToken(v: String) = _state.update { it.copy(token = v, error = null, done = false) }
    fun setRemember(v: Boolean) = _state.update { it.copy(remember = v) }

    fun run() {
        val s = _state.value
        if (s.running) return
        viewModelScope.launch {
            _state.update { it.copy(running = true, error = null, imported = 0, total = 0, done = false) }
            val result = importer.fetch(s.host, s.token)
            if (result.error != null) {
                _state.update { it.copy(running = false, error = result.error) }
                return@launch
            }
            // Persist (or clear) the connection for next time.
            if (s.remember) {
                secureStore.onePasswordHost = s.host.trim()
                secureStore.onePasswordToken = s.token.trim()
            } else {
                secureStore.onePasswordHost = null
                secureStore.onePasswordToken = null
            }
            var count = 0
            for (input in result.items) {
                runCatching { repository.create(input) }.onSuccess { count++ }
                _state.update { it.copy(imported = count, total = result.items.size) }
            }
            _state.update { it.copy(running = false, done = true, imported = count, total = result.items.size) }
        }
    }
}
