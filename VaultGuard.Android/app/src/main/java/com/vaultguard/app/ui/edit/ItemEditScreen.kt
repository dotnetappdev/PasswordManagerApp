package com.vaultguard.app.ui.edit

import androidx.compose.animation.animateColorAsState
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.AutoAwesome
import androidx.compose.material.icons.filled.DeleteOutline
import androidx.compose.material.icons.filled.QrCodeScanner
import androidx.compose.material.icons.filled.Visibility
import androidx.compose.material.icons.filled.VisibilityOff
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.ExposedDropdownMenuBox
import androidx.compose.material3.ExposedDropdownMenuDefaults
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.LinearProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Surface
import androidx.compose.material3.Switch
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateMapOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.text.input.VisualTransformation
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.navigation.NavController
import com.vaultguard.app.ui.common.ItemIconTile
import com.vaultguard.app.data.model.CustomFieldType
import com.vaultguard.app.data.model.ItemType
import com.vaultguard.app.ui.navigation.Routes

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ItemEditScreen(
    itemId: Int,
    autoScan: Boolean = false,
    navController: NavController,
    onDone: () -> Unit,
    onScanTotp: () -> Unit,
    viewModel: ItemEditViewModel = hiltViewModel(),
) {
    val state by viewModel.state.collectAsStateWithLifecycle()
    var autoScanTriggered by rememberSaveable { mutableStateOf(false) }

    LaunchedEffect(itemId) { viewModel.load(itemId) }
    LaunchedEffect(state.saved) { if (state.saved) onDone() }
    LaunchedEffect(autoScan) {
        if (autoScan && !autoScanTriggered) { autoScanTriggered = true; onScanTotp() }
    }

    val entry = navController.currentBackStackEntry
    LaunchedEffect(entry) {
        entry?.savedStateHandle?.getStateFlow<String?>(Routes.SCAN_RESULT, null)?.collect { value ->
            if (!value.isNullOrBlank()) {
                viewModel.applyScannedTotp(value)
                entry.savedStateHandle[Routes.SCAN_RESULT] = null
            }
        }
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(if (state.isNew) "New Item" else "Edit Item", fontWeight = FontWeight.SemiBold) },
                navigationIcon = {
                    IconButton(onClick = onDone) { Icon(Icons.AutoMirrored.Filled.ArrowBack, "Back") }
                },
                actions = { TextButton(onClick = viewModel::save, enabled = !state.saving) { Text("Save") } },
            )
        },
        // Persistent bottom save bar — reachable without scrolling to the end of a long form.
        bottomBar = {
            Surface(tonalElevation = 3.dp, shadowElevation = 8.dp) {
                Button(
                    onClick = viewModel::save,
                    enabled = !state.saving,
                    shape = RoundedCornerShape(14.dp),
                    modifier = Modifier
                        .fillMaxWidth()
                        .navigationBarsPadding()
                        .padding(horizontal = 16.dp, vertical = 10.dp)
                        .height(52.dp),
                ) {
                    if (state.saving) CircularProgressIndicator(Modifier.size(20.dp), strokeWidth = 2.dp)
                    else Text(if (state.isNew) "Create item" else "Save changes")
                }
            }
        },
    ) { padding ->
        Column(
            Modifier
                .fillMaxSize()
                .padding(padding)
                .verticalScroll(rememberScrollState())
                .padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(16.dp),
        ) {
            // Header: item glyph + title, like the 1Password editor.
            Row(verticalAlignment = Alignment.CenterVertically) {
                ItemIconTile(ItemType.Login, Modifier.size(56.dp))
                Spacer(Modifier.size(14.dp))
                OutlinedTextField(
                    value = state.title,
                    onValueChange = viewModel::setTitle,
                    label = { Text("Title") },
                    singleLine = true,
                    shape = RoundedCornerShape(14.dp),
                    modifier = Modifier.weight(1f),
                )
            }

            FormSection("Login details") {
                RoundedField("Username", state.username, viewModel::setUsername)
                RoundedField("Email", state.email, viewModel::setEmail, KeyboardType.Email)
                RoundedField("Website", state.website, viewModel::setWebsite, KeyboardType.Uri)
                RoundedField("Login URL", state.loginUrl, viewModel::setLoginUrl, KeyboardType.Uri)
            }

            FormSection("Password") {
                var visible by remember { mutableStateOf(false) }
                OutlinedTextField(
                    value = state.password,
                    onValueChange = viewModel::setPassword,
                    label = { Text(if (state.isNew) "Password" else "Password (leave blank to keep)") },
                    singleLine = true,
                    shape = RoundedCornerShape(14.dp),
                    visualTransformation = if (visible) VisualTransformation.None else PasswordVisualTransformation(),
                    textStyle = MaterialTheme.typography.bodyLarge.copy(fontFamily = FontFamily.Monospace),
                    trailingIcon = {
                        Row {
                            IconButton(onClick = { visible = !visible }) {
                                Icon(if (visible) Icons.Filled.VisibilityOff else Icons.Filled.Visibility, "Toggle visibility")
                            }
                            IconButton(onClick = viewModel::generatePassword) {
                                Icon(Icons.Filled.AutoAwesome, "Generate")
                            }
                        }
                    },
                    modifier = Modifier.fillMaxWidth(),
                )
                if (state.password.isNotEmpty()) StrengthMeter(state.passwordStrength)
                TextButton(onClick = viewModel::generatePassword) {
                    Icon(Icons.Filled.AutoAwesome, null)
                    Text("  Generate strong password")
                }
            }

            FormSection("One-time password") {
                OutlinedTextField(
                    value = state.totpSecret,
                    onValueChange = viewModel::setTotp,
                    label = { Text("Authenticator (TOTP) secret") },
                    singleLine = true,
                    shape = RoundedCornerShape(14.dp),
                    trailingIcon = {
                        IconButton(onClick = onScanTotp) { Icon(Icons.Filled.QrCodeScanner, "Scan QR code") }
                    },
                    modifier = Modifier.fillMaxWidth(),
                )
            }

            FormSection("Category") {
                CategoryDropdown(
                    categories = state.categories.map { it.name },
                    selected = state.selectedCategory,
                    onSelect = viewModel::setCategory,
                )
            }

            CustomFieldsSection(
                fields = state.customFields,
                onAdd = viewModel::addCustomField,
                onName = viewModel::setCustomFieldName,
                onValue = viewModel::setCustomFieldValue,
                onType = viewModel::setCustomFieldType,
                onRemove = viewModel::removeCustomField,
            )

            FormSection("More") {
                RoundedField("Description", state.description, viewModel::setDescription)
                OutlinedTextField(
                    value = state.notes,
                    onValueChange = viewModel::setNotes,
                    label = { Text("Notes") },
                    shape = RoundedCornerShape(14.dp),
                    minLines = 3,
                    modifier = Modifier.fillMaxWidth(),
                )
                Row(Modifier.fillMaxWidth(), Arrangement.SpaceBetween, Alignment.CenterVertically) {
                    Text("Favourite")
                    Switch(checked = state.isFavorite, onCheckedChange = viewModel::setFavorite)
                }
            }

            state.error?.let { Text(it, color = MaterialTheme.colorScheme.error) }

            // Trailing space so the last field clears the persistent bottom save bar.
            Spacer(Modifier.height(8.dp))
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun CustomFieldsSection(
    fields: List<com.vaultguard.app.data.model.CustomFieldData>,
    onAdd: () -> Unit,
    onName: (Int, String) -> Unit,
    onValue: (Int, String) -> Unit,
    onType: (Int, CustomFieldType) -> Unit,
    onRemove: (Int) -> Unit,
) {
    // Per-field "reveal" toggles for masked (password / OTP) values — display only.
    val revealed = remember { mutableStateMapOf<Int, Boolean>() }

    FormSection("Custom fields") {
        if (fields.isEmpty()) {
            Text(
                "Add your own fields — a PIN, a recovery code, a membership number… " +
                    "with the same field types as the desktop app.",
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
            )
        }
        fields.forEachIndexed { index, field ->
            val type = field.fieldType
            Column(verticalArrangement = Arrangement.spacedBy(6.dp)) {
                Row(verticalAlignment = Alignment.CenterVertically) {
                    OutlinedTextField(
                        value = field.name,
                        onValueChange = { onName(index, it) },
                        label = { Text("Label") },
                        singleLine = true,
                        shape = RoundedCornerShape(14.dp),
                        modifier = Modifier.weight(1f),
                    )
                    IconButton(onClick = { onRemove(index) }) {
                        Icon(Icons.Filled.DeleteOutline, "Remove field", tint = MaterialTheme.colorScheme.error)
                    }
                }

                // Field type picker — mirrors the WPF/Blazor custom-field types exactly.
                CustomFieldTypeDropdown(type) { onType(index, it) }

                when (type) {
                    CustomFieldType.Toggle -> {
                        val on = field.value.equals("true", ignoreCase = true) || field.value == "1"
                        Row(Modifier.fillMaxWidth(), Arrangement.SpaceBetween, Alignment.CenterVertically) {
                            Text(if (on) "Yes" else "No")
                            Switch(checked = on, onCheckedChange = { onValue(index, if (it) "true" else "false") })
                        }
                    }
                    else -> {
                        val masked = field.isMasked && revealed[index] != true
                        OutlinedTextField(
                            value = field.value,
                            onValueChange = { onValue(index, it) },
                            label = { Text(if (field.isMasked) "Value (hidden)" else "Value") },
                            singleLine = !type.isMultiline,
                            minLines = if (type.isMultiline) 3 else 1,
                            shape = RoundedCornerShape(14.dp),
                            keyboardOptions = KeyboardOptions(keyboardType = keyboardTypeFor(type)),
                            visualTransformation = if (masked) PasswordVisualTransformation() else VisualTransformation.None,
                            trailingIcon = if (field.isMasked) {
                                {
                                    IconButton(onClick = { revealed[index] = revealed[index] != true }) {
                                        Icon(
                                            if (masked) Icons.Filled.Visibility else Icons.Filled.VisibilityOff,
                                            if (masked) "Reveal value" else "Hide value",
                                        )
                                    }
                                }
                            } else null,
                            modifier = Modifier.fillMaxWidth(),
                        )
                    }
                }
            }
        }
        TextButton(onClick = onAdd) {
            Icon(Icons.Filled.Add, null)
            Text("  Add custom field")
        }
    }
}

/** Keyboard best suited to a custom-field type (parity with the desktop input hints). */
private fun keyboardTypeFor(type: CustomFieldType): KeyboardType = when (type) {
    CustomFieldType.Number -> KeyboardType.Number
    CustomFieldType.Email -> KeyboardType.Email
    CustomFieldType.Phone -> KeyboardType.Phone
    CustomFieldType.Url, CustomFieldType.SignInWith -> KeyboardType.Uri
    CustomFieldType.Password, CustomFieldType.OneTimePassword -> KeyboardType.Password
    else -> KeyboardType.Text
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun CustomFieldTypeDropdown(selected: CustomFieldType, onSelect: (CustomFieldType) -> Unit) {
    var expanded by remember { mutableStateOf(false) }
    ExposedDropdownMenuBox(expanded = expanded, onExpandedChange = { expanded = it }, modifier = Modifier.fillMaxWidth()) {
        OutlinedTextField(
            value = selected.label,
            onValueChange = {},
            readOnly = true,
            label = { Text("Type") },
            trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(expanded = expanded) },
            shape = RoundedCornerShape(14.dp),
            modifier = Modifier.fillMaxWidth().menuAnchor(),
        )
        ExposedDropdownMenu(expanded = expanded, onDismissRequest = { expanded = false }) {
            CustomFieldType.entries.forEach { t ->
                DropdownMenuItem(text = { Text(t.label) }, onClick = { onSelect(t); expanded = false })
            }
        }
    }
}

@Composable
private fun FormSection(title: String, content: @Composable () -> Unit) {
    Column(verticalArrangement = Arrangement.spacedBy(6.dp)) {
        Text(
            title.uppercase(),
            style = MaterialTheme.typography.labelMedium,
            color = MaterialTheme.colorScheme.primary,
            fontWeight = FontWeight.Bold,
            modifier = Modifier.padding(start = 4.dp, bottom = 2.dp),
        )
        Card(Modifier.fillMaxWidth(), shape = RoundedCornerShape(16.dp)) {
            Column(Modifier.padding(14.dp), verticalArrangement = Arrangement.spacedBy(10.dp)) { content() }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun CategoryDropdown(categories: List<String>, selected: String?, onSelect: (String?) -> Unit) {
    var expanded by remember { mutableStateOf(false) }
    val options = listOf("None") + categories
    ExposedDropdownMenuBox(expanded = expanded, onExpandedChange = { expanded = it }) {
        OutlinedTextField(
            value = selected ?: "None",
            onValueChange = {},
            readOnly = true,
            label = { Text("Category") },
            trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(expanded = expanded) },
            shape = RoundedCornerShape(14.dp),
            modifier = Modifier.fillMaxWidth().menuAnchor(),
        )
        ExposedDropdownMenu(expanded = expanded, onDismissRequest = { expanded = false }) {
            options.forEach { name ->
                DropdownMenuItem(
                    text = { Text(name) },
                    onClick = { onSelect(if (name == "None") null else name); expanded = false },
                )
            }
        }
    }
}

@Composable
private fun RoundedField(
    label: String,
    value: String,
    onChange: (String) -> Unit,
    keyboardType: KeyboardType = KeyboardType.Text,
) {
    OutlinedTextField(
        value = value,
        onValueChange = onChange,
        label = { Text(label) },
        singleLine = true,
        shape = RoundedCornerShape(14.dp),
        keyboardOptions = KeyboardOptions(keyboardType = keyboardType),
        modifier = Modifier.fillMaxWidth(),
    )
}

@Composable
private fun StrengthMeter(strength: Int) {
    val fraction = (strength / 4f).coerceIn(0f, 1f)
    val color by animateColorAsState(
        when (strength) {
            0, 1 -> Color(0xFFE53935)
            2 -> Color(0xFFFB8C00)
            3 -> Color(0xFFFDD835)
            else -> Color(0xFF43A047)
        },
        label = "strength",
    )
    val label = when (strength) {
        0, 1 -> "Weak"
        2 -> "Fair"
        3 -> "Good"
        else -> "Strong"
    }
    Column(Modifier.fillMaxWidth()) {
        LinearProgressIndicator(
            progress = { fraction },
            color = color,
            trackColor = MaterialTheme.colorScheme.surfaceVariant,
            modifier = Modifier.fillMaxWidth().height(6.dp),
        )
        Text(label, style = MaterialTheme.typography.labelSmall, color = color, modifier = Modifier.padding(top = 4.dp))
    }
}
