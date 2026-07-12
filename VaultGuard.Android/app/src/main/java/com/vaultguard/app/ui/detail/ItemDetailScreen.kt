package com.vaultguard.app.ui.detail

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.ContentCopy
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material.icons.filled.Edit
import androidx.compose.material.icons.filled.MoreHoriz
import androidx.compose.material.icons.filled.Visibility
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.LinearProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.material3.TopAppBarDefaults
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalClipboardManager
import androidx.compose.ui.text.AnnotatedString
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.vaultguard.app.ui.common.MonogramTile

/** 1Password-style field label colour (lavender). */
private val Lavender = Color(0xFFB9A7F5)

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
        containerColor = MaterialTheme.colorScheme.background,
        topBar = {
            TopAppBar(
                title = {},
                navigationIcon = { CircleButton(Icons.AutoMirrored.Filled.ArrowBack, "Back", onBack) },
                actions = {
                    TextButton(onClick = onEdit) {
                        Icon(Icons.Filled.Edit, null, modifier = Modifier.size(18.dp))
                        Text("  Edit", fontWeight = FontWeight.SemiBold)
                    }
                    CircleButton(Icons.Filled.Delete, "Delete", viewModel::delete)
                    Spacer(Modifier.width(4.dp))
                },
                colors = TopAppBarDefaults.topAppBarColors(containerColor = MaterialTheme.colorScheme.background),
            )
        },
    ) { padding ->
        val item = state.item
        Column(
            Modifier
                .fillMaxSize()
                .padding(padding)
                .padding(horizontal = 16.dp)
                .verticalScroll(rememberScrollState()),
            verticalArrangement = Arrangement.spacedBy(14.dp),
        ) {
            if (item == null) {
                Box(Modifier.fillMaxWidth().padding(top = 48.dp), Alignment.Center) {
                    when {
                        state.loading -> CircularProgressIndicator()
                        state.error != null -> Text(state.error!!, color = MaterialTheme.colorScheme.error)
                        else -> Text(
                            "This item couldn't be found. It may have been deleted.",
                            color = MaterialTheme.colorScheme.onSurfaceVariant,
                        )
                    }
                }
                return@Column
            }

            // Owner ｜ vault row (like 1Password's "David Buckley | Personal").
            Row(verticalAlignment = Alignment.CenterVertically, modifier = Modifier.padding(top = 2.dp)) {
                Text(
                    item.categoryName?.takeIf { it.isNotBlank() } ?: "Personal",
                    style = MaterialTheme.typography.labelLarge,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
            }

            // Large icon + bold title.
            Row(verticalAlignment = Alignment.CenterVertically) {
                MonogramTile(item, size = 64.dp)
                Spacer(Modifier.width(16.dp))
                Text(
                    item.title,
                    style = MaterialTheme.typography.headlineSmall,
                    fontWeight = FontWeight.Bold,
                    maxLines = 2,
                    overflow = TextOverflow.Ellipsis,
                )
            }

            // Grouped field card — the signature 1Password look.
            val rows = buildList {
                item.description?.takeIf { it.isNotBlank() }?.let { add(Triple("description", it, false)) }
                item.username?.takeIf { it.isNotBlank() }?.let { add(Triple("username", it, false)) }
                item.email?.takeIf { it.isNotBlank() }?.let { add(Triple("email", it, false)) }
                item.website?.takeIf { it.isNotBlank() }?.let { add(Triple("website", it, false)) }
                item.loginUrl?.takeIf { it.isNotBlank() }?.let { add(Triple("login url", it, false)) }
            }
            if (rows.isNotEmpty() || state.revealedPassword != null || item.type.label.isNotBlank()) {
                FieldCard {
                    rows.forEachIndexed { i, (label, value, mono) ->
                        DetailRow(label, value, mono) { clipboard.setText(AnnotatedString(value)) }
                        if (i < rows.lastIndex) RowDivider()
                    }
                    if (rows.isNotEmpty()) RowDivider()
                    PasswordRow(
                        revealed = state.revealedPassword,
                        revealing = state.revealing,
                        onReveal = viewModel::reveal,
                        onCopy = { pwd -> clipboard.setText(AnnotatedString(pwd)) },
                    )
                }
            }

            // One-time code (TOTP).
            state.totpCode?.let { code ->
                FieldCard {
                    Column(Modifier.padding(horizontal = 16.dp, vertical = 12.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
                        Text("one-time code", style = MaterialTheme.typography.labelMedium, color = Lavender)
                        Row(Modifier.fillMaxWidth(), Arrangement.SpaceBetween, Alignment.CenterVertically) {
                            Text(
                                code.chunked(3).joinToString(" "),
                                style = MaterialTheme.typography.headlineSmall,
                                fontFamily = FontFamily.Monospace,
                            )
                            CircleButton(Icons.Filled.ContentCopy, "Copy code") { clipboard.setText(AnnotatedString(code)) }
                        }
                        LinearProgressIndicator(
                            progress = { state.totpRemaining / 30f },
                            modifier = Modifier.fillMaxWidth().height(3.dp),
                        )
                        Text("Refreshes in ${state.totpRemaining}s", style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
                    }
                }
            }

            // Custom fields.
            if (item.customFields.isNotEmpty()) {
                FieldCard {
                    item.customFields.forEachIndexed { i, field ->
                        CustomFieldRow(field) { clipboard.setText(AnnotatedString(field.value)) }
                        if (i < item.customFields.lastIndex) RowDivider()
                    }
                }
            }

            // Notes.
            item.notes?.takeIf { it.isNotBlank() }?.let { notes ->
                FieldCard {
                    Column(Modifier.padding(horizontal = 16.dp, vertical = 12.dp)) {
                        Text("notes", style = MaterialTheme.typography.labelMedium, color = Lavender)
                        Spacer(Modifier.height(4.dp))
                        Text(notes, style = MaterialTheme.typography.bodyLarge)
                    }
                }
            }

            Spacer(Modifier.height(8.dp))
        }
    }
}

@Composable
private fun FieldCard(content: @Composable androidx.compose.foundation.layout.ColumnScope.() -> Unit) {
    Surface(
        shape = RoundedCornerShape(16.dp),
        color = MaterialTheme.colorScheme.surfaceVariant,
        modifier = Modifier.fillMaxWidth(),
    ) {
        Column(content = content)
    }
}

@Composable
private fun RowDivider() =
    HorizontalDivider(color = MaterialTheme.colorScheme.outlineVariant, modifier = Modifier.padding(start = 16.dp))

@Composable
private fun DetailRow(label: String, value: String, mono: Boolean, onCopy: () -> Unit) {
    Row(
        Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 12.dp),
        verticalAlignment = Alignment.CenterVertically,
    ) {
        Column(Modifier.weight(1f)) {
            Text(label, style = MaterialTheme.typography.labelMedium, color = Lavender)
            Spacer(Modifier.height(2.dp))
            Text(
                value,
                style = MaterialTheme.typography.bodyLarge,
                fontFamily = if (mono) FontFamily.Monospace else FontFamily.Default,
                maxLines = 2,
                overflow = TextOverflow.Ellipsis,
            )
        }
        Spacer(Modifier.width(8.dp))
        CircleButton(Icons.Filled.ContentCopy, "Copy $label", onCopy)
    }
}

@Composable
private fun CustomFieldRow(field: com.vaultguard.app.data.model.CustomFieldData, onCopy: () -> Unit) {
    val type = field.fieldType
    val masked = field.isMasked
    var revealed by remember(field) { mutableStateOf(!masked) }
    // Toggle fields read as Yes/No; everything else shows its (optionally masked) value.
    val display = when {
        type == com.vaultguard.app.data.model.CustomFieldType.Toggle ->
            if (field.value.equals("true", true) || field.value == "1") "Yes" else "No"
        revealed -> field.value
        else -> "•".repeat(field.value.length.coerceIn(6, 12))
    }
    Row(
        Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 12.dp),
        verticalAlignment = Alignment.CenterVertically,
    ) {
        Column(Modifier.weight(1f)) {
            Text(field.name.ifBlank { "field" }.lowercase(), style = MaterialTheme.typography.labelMedium, color = Lavender)
            Spacer(Modifier.height(2.dp))
            Text(
                display,
                style = MaterialTheme.typography.bodyLarge,
                fontFamily = if (masked) FontFamily.Monospace else FontFamily.Default,
                maxLines = if (type.isMultiline) 6 else 2,
                overflow = TextOverflow.Ellipsis,
            )
        }
        Spacer(Modifier.width(8.dp))
        if (masked && !revealed) {
            CircleButton(Icons.Filled.Visibility, "Reveal ${field.name}") { revealed = true }
        } else if (type != com.vaultguard.app.data.model.CustomFieldType.Toggle) {
            CircleButton(Icons.Filled.ContentCopy, "Copy ${field.name}", onCopy)
        }
    }
}

@Composable
private fun PasswordRow(
    revealed: String?,
    revealing: Boolean,
    onReveal: () -> Unit,
    onCopy: (String) -> Unit,
) {
    Row(
        Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 12.dp),
        verticalAlignment = Alignment.CenterVertically,
    ) {
        Column(Modifier.weight(1f)) {
            Text("password", style = MaterialTheme.typography.labelMedium, color = Lavender)
            Spacer(Modifier.height(2.dp))
            if (revealed == null) {
                Text(
                    if (revealing) "revealing…" else "•••••••••••",
                    style = MaterialTheme.typography.bodyLarge,
                    fontFamily = FontFamily.Monospace,
                )
            } else {
                Text(revealed, style = MaterialTheme.typography.bodyLarge, fontFamily = FontFamily.Monospace, maxLines = 2, overflow = TextOverflow.Ellipsis)
            }
        }
        Spacer(Modifier.width(8.dp))
        val strength = revealed?.let { strengthLabel(it) }
        if (strength != null) {
            Text(strength.first, style = MaterialTheme.typography.labelMedium, color = strength.second)
            Spacer(Modifier.width(8.dp))
        }
        if (revealed == null) {
            CircleButton(Icons.Filled.Visibility, "Reveal password", onReveal)
        } else {
            CircleButton(Icons.Filled.ContentCopy, "Copy password") { onCopy(revealed) }
        }
    }
}

/** A rough password-strength label + colour for the detail view. */
private fun strengthLabel(pw: String): Pair<String, Color> {
    var score = 0
    if (pw.length >= 12) score++
    if (pw.length >= 16) score++
    if (pw.any { it.isDigit() }) score++
    if (pw.any { it.isUpperCase() } && pw.any { it.isLowerCase() }) score++
    if (pw.any { !it.isLetterOrDigit() }) score++
    return when {
        score >= 4 -> "Excellent" to Color(0xFF34C759)
        score >= 3 -> "Good" to Color(0xFF9ACD32)
        score >= 2 -> "Fair" to Color(0xFFF5A623)
        else -> "Weak" to Color(0xFFE94B3C)
    }
}

@Composable
private fun CircleButton(icon: androidx.compose.ui.graphics.vector.ImageVector, desc: String, onClick: () -> Unit) {
    Box(
        Modifier
            .size(34.dp)
            .clip(CircleShape)
            .border(1.dp, MaterialTheme.colorScheme.outline, CircleShape)
            .clickable(onClick = onClick),
        contentAlignment = Alignment.Center,
    ) {
        Icon(icon, desc, tint = MaterialTheme.colorScheme.onSurfaceVariant, modifier = Modifier.size(18.dp))
    }
}
