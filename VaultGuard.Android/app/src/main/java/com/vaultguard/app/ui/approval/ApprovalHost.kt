package com.vaultguard.app.ui.approval

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.LinearProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.ViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewModelScope
import com.vaultguard.app.config.SettingsStore
import com.vaultguard.app.data.model.PendingApprovalDto
import com.vaultguard.app.data.repo.SessionManager
import com.vaultguard.app.data.repo.VaultRepository
import com.vaultguard.app.ui.common.Toaster
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.isActive
import kotlinx.coroutines.launch
import java.time.Instant
import javax.inject.Inject

@HiltViewModel
class ApprovalViewModel @Inject constructor(
    private val repository: VaultRepository,
    private val settingsStore: SettingsStore,
    private val session: SessionManager,
    private val toaster: Toaster,
) : ViewModel() {

    private val _pending = MutableStateFlow<PendingApprovalDto?>(null)
    val pending = _pending.asStateFlow()

    init {
        viewModelScope.launch {
            while (isActive) {
                val enabled = settingsStore.settings.first().numberMatchApprovals
                if (session.unlocked.value && enabled && _pending.value == null) {
                    _pending.value = repository.pendingApprovals().firstOrNull()
                }
                delay(4000)
            }
        }
    }

    fun respond(number: Int, approve: Boolean) {
        val id = _pending.value?.id ?: return
        _pending.value = null
        viewModelScope.launch {
            val state = repository.respondApproval(id, number, approve).getOrNull()
            toaster.show(
                when {
                    !approve -> "Sign-in denied."
                    state == "Approved" -> "Approved ✓"
                    else -> "That number didn't match."
                }
            )
        }
    }
}

@Composable
fun ApprovalHost(viewModel: ApprovalViewModel = hiltViewModel()) {
    val pending by viewModel.pending.collectAsStateWithLifecycle()
    pending?.let { p -> ApprovalDialog(p, onPick = { viewModel.respond(it, true) }, onDeny = { viewModel.respond(-1, false) }) }
}

@Composable
private fun ApprovalDialog(p: PendingApprovalDto, onPick: (Int) -> Unit, onDeny: () -> Unit) {
    val total = 60
    var remaining by remember(p.id) { mutableIntStateOf(secondsLeft(p.expiresAt, total)) }
    LaunchedEffect(p.id) {
        while (remaining > 0) { delay(1000); remaining = secondsLeft(p.expiresAt, total) }
        onDeny() // auto-dismiss on expiry
    }

    AlertDialog(
        onDismissRequest = { },
        title = { Text("Approve sign-in?") },
        text = {
            Column(verticalArrangement = Arrangement.spacedBy(12.dp)) {
                Text(p.action, style = MaterialTheme.typography.bodyMedium)
                Text("Tap the number shown on the other device.",
                    style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
                Row(Modifier.fillMaxWidth(), Arrangement.spacedBy(12.dp, androidx.compose.ui.Alignment.CenterHorizontally)) {
                    p.choices.forEach { n ->
                        Button(
                            onClick = { onPick(n) },
                            shape = RoundedCornerShape(16.dp),
                            modifier = Modifier.size(72.dp),
                        ) { Text("%02d".format(n), fontSize = 24.sp, fontWeight = FontWeight.Bold) }
                    }
                }
                LinearProgressIndicator(progress = { remaining / total.toFloat() }, modifier = Modifier.fillMaxWidth())
                Text("Expires in ${remaining}s", style = MaterialTheme.typography.labelSmall)
            }
        },
        confirmButton = {},
        dismissButton = {
            TextButton(onClick = onDeny, colors = ButtonDefaults.textButtonColors(contentColor = MaterialTheme.colorScheme.error)) {
                Text("It's not me — deny")
            }
        },
    )
}

private fun secondsLeft(expiresAt: String?, fallback: Int): Int {
    if (expiresAt == null) return fallback
    return runCatching {
        val secs = java.time.Duration.between(Instant.now(), Instant.parse(expiresAt)).seconds
        secs.coerceIn(0, fallback.toLong()).toInt()
    }.getOrDefault(fallback)
}
