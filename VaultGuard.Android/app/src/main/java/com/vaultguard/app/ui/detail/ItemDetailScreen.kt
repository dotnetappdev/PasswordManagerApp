package com.vaultguard.app.ui.detail

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.ContentCopy
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material.icons.filled.Edit
import androidx.compose.material.icons.filled.Visibility
import androidx.compose.material3.Card
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.LinearProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalClipboardManager
import androidx.compose.ui.text.AnnotatedString
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ItemDetailScreen(
    itemId: Int,
    onBack: () -> Unit,
    onEdit: () -> Unit,
    viewModel: ItemDetailViewModel = hiltViewModel(),
) {
    val state by viewModel.state.collectAsStateWithLifecycle()
    val clipboard = LocalClipboardManager.current

    LaunchedEffect(itemId) { viewModel.load(itemId) }
    LaunchedEffect(state.deleted) { if (state.deleted) onBack() }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(state.item?.title ?: "Item") },
                navigationIcon = {
                    IconButton(onClick = onBack) { Icon(Icons.AutoMirrored.Filled.ArrowBack, "Back") }
                },
                actions = {
                    IconButton(onClick = onEdit) { Icon(Icons.Filled.Edit, "Edit") }
                    IconButton(onClick = viewModel::delete) { Icon(Icons.Filled.Delete, "Delete") }
                },
            )
        },
    ) { padding ->
        val item = state.item
        Column(
            Modifier
                .fillMaxSize()
                .padding(padding)
                .padding(16.dp)
                .verticalScroll(rememberScrollState()),
            verticalArrangement = Arrangement.spacedBy(12.dp),
        ) {
            if (item == null) {
                state.error?.let { Text(it, color = MaterialTheme.colorScheme.error) }
                return@Column
            }

            item.description?.takeIf { it.isNotBlank() }?.let { Field("Description", it) }
            item.username?.let { CopyField("Username", it) { clipboard.setText(AnnotatedString(it)) } }
            item.email?.let { CopyField("Email", it) { clipboard.setText(AnnotatedString(it)) } }
            item.website?.let { CopyField("Website", it) { clipboard.setText(AnnotatedString(it)) } }
            item.loginUrl?.let { CopyField("Login URL", it) { clipboard.setText(AnnotatedString(it)) } }

            // Password (revealed on demand)
            Card(Modifier.fillMaxWidth()) {
                Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
                    Text("Password", style = MaterialTheme.typography.labelMedium, color = MaterialTheme.colorScheme.primary)
                    val pwd = state.revealedPassword
                    if (pwd == null) {
                        TextButton(onClick = viewModel::reveal) {
                            Icon(Icons.Filled.Visibility, null)
                            Text(if (state.revealing) "  Revealing…" else "  Reveal password")
                        }
                    } else {
                        Row(Modifier.fillMaxWidth(), Arrangement.SpaceBetween, Alignment.CenterVertically) {
                            Text(pwd, fontFamily = FontFamily.Monospace)
                            IconButton(onClick = { clipboard.setText(AnnotatedString(pwd)) }) {
                                Icon(Icons.Filled.ContentCopy, "Copy password")
                            }
                        }
                    }
                }
            }

            // TOTP (populated after reveal, if the item has an authenticator secret)
            state.totpCode?.let { code ->
                Card(Modifier.fillMaxWidth()) {
                    Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
                        Text("One-time code", style = MaterialTheme.typography.labelMedium, color = MaterialTheme.colorScheme.primary)
                        Row(Modifier.fillMaxWidth(), Arrangement.SpaceBetween, Alignment.CenterVertically) {
                            Text(
                                code.chunked(3).joinToString(" "),
                                style = MaterialTheme.typography.headlineSmall,
                                fontFamily = FontFamily.Monospace,
                            )
                            IconButton(onClick = { clipboard.setText(AnnotatedString(code)) }) {
                                Icon(Icons.Filled.ContentCopy, "Copy code")
                            }
                        }
                        LinearProgressIndicator(
                            progress = { state.totpRemaining / 30f },
                            modifier = Modifier.fillMaxWidth(),
                        )
                        Text("Refreshes in ${state.totpRemaining}s", style = MaterialTheme.typography.bodySmall)
                    }
                }
            }

            item.notes?.takeIf { it.isNotBlank() }?.let { Field("Notes", it) }
        }
    }
}

@Composable
private fun Field(label: String, value: String) {
    Column(Modifier.fillMaxWidth()) {
        Text(label, style = MaterialTheme.typography.labelMedium, color = MaterialTheme.colorScheme.primary)
        Text(value, style = MaterialTheme.typography.bodyLarge)
    }
}

@Composable
private fun CopyField(label: String, value: String, onCopy: () -> Unit) {
    Row(Modifier.fillMaxWidth(), Arrangement.SpaceBetween, Alignment.CenterVertically) {
        Column(Modifier.weight(1f)) {
            Text(label, style = MaterialTheme.typography.labelMedium, color = MaterialTheme.colorScheme.primary)
            Text(value, style = MaterialTheme.typography.bodyLarge)
        }
        IconButton(onClick = onCopy) { Icon(Icons.Filled.ContentCopy, "Copy $label") }
    }
}
