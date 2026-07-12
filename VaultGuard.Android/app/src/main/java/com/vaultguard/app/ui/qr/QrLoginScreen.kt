package com.vaultguard.app.ui.qr

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Button
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.ViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewModelScope
import com.vaultguard.app.data.repo.VaultRepository
import com.vaultguard.app.ui.common.Toaster
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import javax.inject.Inject

sealed interface QrLoginUi {
    data object Scanning : QrLoginUi
    data object Working : QrLoginUi
    data class Done(val message: String) : QrLoginUi
    data class Error(val message: String) : QrLoginUi
}

@HiltViewModel
class QrLoginViewModel @Inject constructor(
    private val repository: VaultRepository,
    private val toaster: Toaster,
) : ViewModel() {
    private val _state = MutableStateFlow<QrLoginUi>(QrLoginUi.Scanning)
    val state = _state.asStateFlow()

    fun authenticate(scanned: String) {
        if (_state.value != QrLoginUi.Scanning) return
        _state.value = QrLoginUi.Working
        viewModelScope.launch {
            repository.signInComputer(scanned)
                .onSuccess { toaster.show(it); _state.value = QrLoginUi.Done(it) }
                .onFailure { _state.value = QrLoginUi.Error(it.message ?: "QR sign-in failed.") }
        }
    }

    fun retry() { _state.value = QrLoginUi.Scanning }
}

@Composable
fun QrLoginScreen(onDone: () -> Unit, viewModel: QrLoginViewModel = hiltViewModel()) {
    val state by viewModel.state.collectAsStateWithLifecycle()

    LaunchedEffect(state) { if (state is QrLoginUi.Done) onDone() }

    Box(Modifier.fillMaxSize()) {
        when (val s = state) {
            is QrLoginUi.Scanning -> QrScannerScreen(
                onResult = { viewModel.authenticate(it) },
                onCancel = onDone,
            )
            is QrLoginUi.Working -> Center { CircularProgressIndicator(); Text("Signing in the computer…") }
            is QrLoginUi.Done -> Center { Text(s.message) }
            is QrLoginUi.Error -> Center {
                Text(s.message, color = MaterialTheme.colorScheme.error)
                Button(onClick = viewModel::retry) { Text("Scan again") }
                Button(onClick = onDone) { Text("Close") }
            }
        }
    }
}

@Composable
private fun Center(content: @Composable () -> Unit) {
    Box(Modifier.fillMaxSize(), Alignment.Center) {
        Column(
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.spacedBy(12.dp),
            modifier = Modifier.padding(24.dp),
        ) { content() }
    }
}
