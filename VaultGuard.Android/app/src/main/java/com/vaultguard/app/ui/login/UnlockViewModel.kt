package com.vaultguard.app.ui.login

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.vaultguard.app.config.ConfigStore
import com.vaultguard.app.config.ConnectionMode
import com.vaultguard.app.data.repo.AuthRepository
import com.vaultguard.app.data.repo.LoginResult
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import javax.inject.Inject

data class UnlockState(
    val mode: ConnectionMode = ConnectionMode.API,
    val email: String = "",
    val password: String = "",
    val twoFactorCode: String = "",
    val needsTwoFactor: Boolean = false,
    val loading: Boolean = false,
    val error: String? = null,
    val unlocked: Boolean = false,
)

@HiltViewModel
class UnlockViewModel @Inject constructor(
    private val authRepository: AuthRepository,
    private val configStore: ConfigStore,
) : ViewModel() {

    private val _state = MutableStateFlow(UnlockState())
    val state = _state.asStateFlow()

    init {
        viewModelScope.launch {
            _state.update { it.copy(mode = configStore.config.first().mode) }
        }
    }

    fun setEmail(v: String) = _state.update { it.copy(email = v, error = null) }
    fun setPassword(v: String) = _state.update { it.copy(password = v, error = null) }
    fun setTwoFactor(v: String) = _state.update { it.copy(twoFactorCode = v, error = null) }

    fun submit() {
        val s = _state.value
        if (s.password.isBlank()) {
            _state.update { it.copy(error = "Enter your master password.") }
            return
        }
        viewModelScope.launch {
            _state.update { it.copy(loading = true, error = null) }
            val result = when (s.mode) {
                ConnectionMode.API -> authRepository.login(s.email, s.password, s.twoFactorCode)
                ConnectionMode.LOCAL -> authRepository.unlockLocal(s.password)
            }
            when (result) {
                is LoginResult.Success -> _state.update { it.copy(loading = false, unlocked = true) }
                is LoginResult.NeedsTwoFactor ->
                    _state.update { it.copy(loading = false, needsTwoFactor = true, error = "Enter your 2FA code.") }
                is LoginResult.Error -> _state.update { it.copy(loading = false, error = result.message) }
            }
        }
    }
}
