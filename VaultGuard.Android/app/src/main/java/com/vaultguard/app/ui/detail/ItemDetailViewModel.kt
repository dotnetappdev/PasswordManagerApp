package com.vaultguard.app.ui.detail

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.vaultguard.app.data.model.VaultItem
import com.vaultguard.app.data.repo.VaultRepository
import com.vaultguard.app.domain.Totp
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import javax.inject.Inject

data class ItemDetailState(
    val loading: Boolean = true,
    val item: VaultItem? = null,
    val error: String? = null,
    val revealing: Boolean = false,
    val revealedPassword: String? = null,
    val totpCode: String? = null,
    val totpRemaining: Int = 0,
    val deleted: Boolean = false,
)

@HiltViewModel
class ItemDetailViewModel @Inject constructor(
    private val repository: VaultRepository,
) : ViewModel() {

    private val _state = MutableStateFlow(ItemDetailState())
    val state = _state.asStateFlow()

    private var totpSecret: String? = null
    private var totpJob: Job? = null

    fun load(id: Int) {
        viewModelScope.launch {
            _state.update { it.copy(loading = true, error = null) }
            try {
                val item = repository.get(id)
                _state.update { it.copy(loading = false, item = item) }
            } catch (e: Exception) {
                _state.update { it.copy(loading = false, error = e.message ?: "Could not load this item.") }
            }
        }
    }

    /** Fetch the decrypted secrets (password + TOTP) for the current item. */
    fun reveal() {
        val id = _state.value.item?.id ?: return
        viewModelScope.launch {
            _state.update { it.copy(revealing = true, error = null) }
            try {
                val secret = repository.secret(id)
                _state.update { it.copy(revealing = false, revealedPassword = secret.password) }
                secret.totpSecret?.takeIf { it.isNotBlank() }?.let { startTotp(it) }
            } catch (e: Exception) {
                _state.update { it.copy(revealing = false, error = e.message ?: "Could not reveal.") }
            }
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
        super.onCleared()
    }
}
