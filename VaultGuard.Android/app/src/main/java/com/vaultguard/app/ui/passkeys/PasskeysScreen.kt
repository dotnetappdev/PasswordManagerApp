package com.vaultguard.app.ui.passkeys

import android.content.Context
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material.icons.filled.Fingerprint
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Card
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExtendedFloatingActionButton
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.ListItem
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import androidx.credentials.CreatePublicKeyCredentialRequest
import androidx.credentials.CreatePublicKeyCredentialResponse
import androidx.credentials.CredentialManager
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.ViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewModelScope
import com.vaultguard.app.data.model.ItemType
import com.vaultguard.app.data.model.PasskeyDto
import com.vaultguard.app.data.model.PasskeyStatus
import com.vaultguard.app.data.model.VaultItem
import com.vaultguard.app.data.repo.VaultRepository
import com.vaultguard.app.ui.common.Toaster
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import javax.inject.Inject

data class PasskeysState(
    val loading: Boolean = true,
    val busy: Boolean = false,
    val status: PasskeyStatus? = null,
    val passkeys: List<PasskeyDto> = emptyList(),
    // Passkeys saved FOR websites (vault items of type Passkey), each tied to its site URL.
    val sitePasskeys: List<VaultItem> = emptyList(),
)

@HiltViewModel
class PasskeysViewModel @Inject constructor(
    private val repository: VaultRepository,
    private val toaster: Toaster,
) : ViewModel() {
    private val _state = MutableStateFlow(PasskeysState())
    val state = _state.asStateFlow()

    init { load() }

    fun load() {
        viewModelScope.launch {
            _state.update { it.copy(loading = true) }
            val status = repository.passkeyStatus()
            val list = repository.passkeys()
            // Website passkeys = local vault items of type Passkey. Best-effort; never fail the screen.
            val site = runCatching {
                repository.list().filter { it.type == ItemType.Passkey && !it.isDeleted && !it.isArchived }
                    .sortedBy { it.title }
            }.getOrDefault(emptyList())
            _state.update { it.copy(loading = false, status = status, passkeys = list, sitePasskeys = site) }
        }
    }

    fun delete(id: Int) {
        viewModelScope.launch {
            repository.deletePasskey(id)
                .onSuccess { toaster.show("Passkey removed."); load() }
                .onFailure { toaster.show(it.message ?: "Could not remove passkey.") }
        }
    }

    /** Full WebAuthn registration via Android Credential Manager. [activity] must be an Activity context. */
    fun register(activity: Context, name: String) {
        viewModelScope.launch {
            _state.update { it.copy(busy = true) }
            val start = repository.passkeyRegisterStart(name).getOrElse {
                toaster.show(it.message ?: "Could not start registration.")
                _state.update { s -> s.copy(busy = false) }
                return@launch
            }
            try {
                val manager = CredentialManager.create(activity)
                val request = CreatePublicKeyCredentialRequest(start.credentialCreationOptions)
                val response = manager.createCredential(activity, request) as CreatePublicKeyCredentialResponse
                repository.passkeyRegisterComplete(
                    challenge = start.challenge,
                    optionsJson = start.credentialCreationOptions,
                    responseJson = response.registrationResponseJson,
                    name = name,
                ).onSuccess { toaster.show("Passkey added."); load() }
                    .onFailure { toaster.show(it.message ?: "Could not save passkey.") }
            } catch (e: Exception) {
                toaster.show(e.message ?: "Passkey registration was cancelled.")
            }
            _state.update { it.copy(busy = false) }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun PasskeysScreen(onBack: () -> Unit, viewModel: PasskeysViewModel = hiltViewModel()) {
    val state by viewModel.state.collectAsStateWithLifecycle()
    val context = LocalContext.current
    var showAdd by remember { mutableStateOf(false) }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Passkeys") },
                navigationIcon = { IconButton(onClick = onBack) { Icon(Icons.AutoMirrored.Filled.ArrowBack, "Back") } },
            )
        },
        floatingActionButton = {
            ExtendedFloatingActionButton(
                onClick = { showAdd = true },
                icon = { Icon(Icons.Filled.Fingerprint, null) },
                text = { Text(if (state.busy) "Working…" else "Add passkey") },
            )
        },
    ) { padding ->
        Column(Modifier.fillMaxSize().padding(padding)) {
            Card(Modifier.fillMaxWidth().padding(16.dp)) {
                Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(6.dp)) {
                    Text("Passwordless sign-in", style = MaterialTheme.typography.titleMedium, color = MaterialTheme.colorScheme.primary)
                    Text(
                        "Register a passkey (fingerprint, face, screen lock or a hardware key) to sign in without your master password.",
                        style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant,
                    )
                    state.status?.let {
                        Text("${it.passkeyCount} passkey${if (it.passkeyCount == 1) "" else "s"} · ${if (it.isEnabled) "Enabled" else "Not enabled"}",
                            style = MaterialTheme.typography.bodySmall)
                    }
                }
            }

            // ── Passkeys saved for your websites (password-manager style) ──
            Card(Modifier.fillMaxWidth().padding(horizontal = 16.dp)) {
                Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(6.dp)) {
                    Text("Passkeys saved for your websites", style = MaterialTheme.typography.titleMedium)
                    Text(
                        "Passkeys VaultGuard stores for other sites — each tied to the website it belongs to. New ones are captured when you create a passkey on a site.",
                        style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant,
                    )
                    if (state.sitePasskeys.isEmpty()) {
                        Text("No website passkeys saved yet.",
                            style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
                    } else {
                        state.sitePasskeys.forEach { item ->
                            ListItem(
                                leadingContent = { Icon(Icons.Filled.Fingerprint, null, tint = MaterialTheme.colorScheme.primary) },
                                headlineContent = { Text(item.title.ifBlank { "Passkey" }) },
                                supportingContent = {
                                    Text(buildString {
                                        val site = item.website?.takeIf { it.isNotBlank() }
                                            ?: item.loginUrl?.takeIf { it.isNotBlank() } ?: "No website"
                                        append(site)
                                        item.username?.takeIf { it.isNotBlank() }?.let { append(" · "); append(it) }
                                    })
                                },
                            )
                            HorizontalDivider()
                        }
                    }
                }
            }

            if (state.loading) {
                CircularProgressIndicator(Modifier.padding(24.dp))
            } else if (state.passkeys.isEmpty()) {
                Text("No passkeys yet. Tap “Add passkey”.",
                    Modifier.padding(24.dp), color = MaterialTheme.colorScheme.onSurfaceVariant)
            } else {
                LazyColumn(Modifier.fillMaxSize()) {
                    items(state.passkeys, key = { it.id }) { pk ->
                        ListItem(
                            leadingContent = { Icon(Icons.Filled.Fingerprint, null, tint = MaterialTheme.colorScheme.primary) },
                            headlineContent = { Text(pk.name.ifBlank { "Passkey" }) },
                            supportingContent = {
                                Text(buildString {
                                    pk.deviceType?.let { append(it) }
                                    if (pk.isBackedUp) append(" · Synced")
                                })
                            },
                            trailingContent = {
                                IconButton(onClick = { viewModel.delete(pk.id) }) {
                                    Icon(Icons.Filled.Delete, "Remove", tint = MaterialTheme.colorScheme.error)
                                }
                            },
                        )
                        HorizontalDivider()
                    }
                }
            }
        }
    }

    if (showAdd) {
        var name by remember { mutableStateOf("My ${android.os.Build.MODEL}") }
        AlertDialog(
            onDismissRequest = { showAdd = false },
            title = { Text("Add a passkey") },
            text = {
                OutlinedTextField(value = name, onValueChange = { name = it }, label = { Text("Name") }, singleLine = true, modifier = Modifier.fillMaxWidth())
            },
            confirmButton = {
                TextButton(enabled = name.isNotBlank(), onClick = { showAdd = false; viewModel.register(context, name.trim()) }) { Text("Continue") }
            },
            dismissButton = { TextButton(onClick = { showAdd = false }) { Text("Cancel") } },
        )
    }
}
