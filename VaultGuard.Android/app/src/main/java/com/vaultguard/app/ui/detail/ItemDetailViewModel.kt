package com.vaultguard.app.ui.detail

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.vaultguard.app.config.SettingsStore
import com.vaultguard.app.data.model.VaultItem
import com.vaultguard.app.data.repo.VaultRepository
import com.vaultguard.app.domain.Totp
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import javax.inject.Inject

data class ItemDetailState(
    val loading: Boolean = true,
    val item: VaultItem? = null,
    val error: String? = null,
    val revealing: Boolean = false,
    val revealedPassword: String? = null,
    /** Seconds left before a revealed password auto-hides (0 = not counting down). */
    val passwordHideRemaining: Int = 0,
    val totpCode: String? = null,
    val totpRemaining: Int = 0,
    val deleted: Boolean = false,
)

@HiltViewModel
class ItemDetailViewModel @Inject constructor(
    private val repository: VaultRepository,
    private val settingsStore: SettingsStore,
) : ViewModel() {

    private val _state = MutableStateFlow(ItemDetailState())
    val state = _state.asStateFlow()

    private var totpSecret: String? = null
    private var totpJob: Job? = null
    private var hideJob: Job? = null

    fun load(id: Int) {
        viewModelScope.launch {
            _state.update { it.copy(loading = true, error = null) }
            try {
                val item = repository.get(id)
                _state.update { it.copy(loading = false, item = item) }
                // Always show the authenticator code (independent of revealing the password), like an
                // authenticator app — the password itself stays hidden until the user taps Reveal.
                runCatching { repository.secret(id).totpSecret }
                    .getOrNull()?.takeIf { it.isNotBlank() }?.let { startTotp(it) }
            } catch (e: Exception) {
                _state.update { it.copy(loading = false, error = e.message ?: "Could not load this item.") }
            }
        }
    }

    /** Fetch the decrypted password for the current item (the TOTP code is already shown on load). */
    fun reveal() {
        val id = _state.value.item?.id ?: return
        viewModelScope.launch {
            _state.update { it.copy(revealing = true, error = null) }
            try {
                val secret = repository.secret(id)
                _state.update { it.copy(revealing = false, revealedPassword = secret.password) }
                if (secret.password != null) startHideCountdown()
            } catch (e: Exception) {
                _state.update { it.copy(revealing = false, error = e.message ?: "Could not reveal.") }
            }
        }
    }

    /** Re-mask the password and stop the auto-hide countdown. */
    fun hidePassword() {
        hideJob?.cancel()
        _state.update { it.copy(revealedPassword = null, passwordHideRemaining = 0) }
    }

    /** Counts down the user-configured window, then re-hides the password (0 = stay visible). */
    private fun startHideCountdown() {
        hideJob?.cancel()
        hideJob = viewModelScope.launch {
            var remaining = settingsStore.settings.first().passwordAutoHideSeconds
            _state.update { it.copy(passwordHideRemaining = remaining) }
            if (remaining <= 0) return@launch
            while (remaining > 0) {
                delay(1000)
                remaining--
                _state.update { it.copy(passwordHideRemaining = remaining) }
            }
            hidePassword()
        }
    }

    private fun startTotp(secret: String) {
        totpSecret = secret
        totpJob?.cancel()
        totpJob = viewModelScope.launch {
            while (true) {
                val code = Totp.generate(secret)
                if (code != null) {
                    _state.update { it.copy(totpCode = code.value, totpRemaining = code.secondsRemaining) }
                }
                delay(1000)
            }
        }
    }

    fun delete() {
        val id = _state.value.item?.id ?: return
        viewModelScope.launch {
            try {
                repository.delete(id)
                _state.update { it.copy(deleted = true) }
            } catch (e: Exception) {
                _state.update { it.copy(error = e.message ?: "Could not delete this item.") }
            }
        }
    }

    override fun onCleared() {
        totpJob?.cancel()
        hideJob?.cancel()
        super.onCleared()
    }
}
