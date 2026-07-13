package com.vaultguard.app.ui.settings

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ExperimentalLayoutApi
import androidx.compose.foundation.layout.FlowRow
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.Check
import androidx.compose.material.icons.filled.DeleteForever
import androidx.compose.material.icons.filled.WarningAmber
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.FilterChip
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.TextButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.ScrollableTabRow
import androidx.compose.material3.SegmentedButton
import androidx.compose.material3.SegmentedButtonDefaults
import androidx.compose.material3.SingleChoiceSegmentedButtonRow
import androidx.compose.material3.Slider
import androidx.compose.material3.Switch
import androidx.compose.material3.Tab
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.material.icons.filled.ContentCopy
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.luminance
import androidx.compose.ui.platform.LocalClipboardManager
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.semantics.stateDescription
import androidx.compose.ui.text.AnnotatedString
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.vaultguard.app.config.AppTheme
import com.vaultguard.app.config.FontChoice
import com.vaultguard.app.config.ConnectionConfig
import com.vaultguard.app.config.ConnectionMode
import com.vaultguard.app.config.DefaultView
import com.vaultguard.app.ui.setup.QrImage
import kotlin.math.roundToInt

private val TABS = listOf(
    "Appearance", "Accessibility", "Security", "Storage",
    "Backup, Import & Export", "Maintenance", "Shortcuts", "About",
)

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun SettingsScreen(
    onBack: () -> Unit,
    onEditConnection: () -> Unit,
    onLocked: () -> Unit,
    onOpenImport: () -> Unit = {},
    onOpen1Password: () -> Unit = {},
    onOpenDeviceSetup: () -> Unit = {},
    viewModel: SettingsViewModel = hiltViewModel(),
) {
    val s by viewModel.settings.collectAsStateWithLifecycle()
    val config by viewModel.config.collectAsStateWithLifecycle()
    val preview by viewModel.preview.collectAsStateWithLifecycle()
    var tab by remember { mutableIntStateOf(0) }

    androidx.compose.material3.Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Settings") },
                navigationIcon = {
                    IconButton(onClick = onBack) { Icon(Icons.AutoMirrored.Filled.ArrowBack, "Back") }
                },
            )
        },
    ) { padding ->
        Column(Modifier.fillMaxSize().padding(padding)) {
            ScrollableTabRow(selectedTabIndex = tab, edgePadding = 12.dp) {
                TABS.forEachIndexed { i, title ->
                    Tab(selected = tab == i, onClick = { tab = i }, text = { Text(title) })
                }
            }

            Column(
                Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(16.dp),
                verticalArrangement = Arrangement.spacedBy(12.dp),
            ) {
                when (tab) {
                    0 -> AppearanceTab(s, viewModel, preview)
                    1 -> AccessibilityTab(s, viewModel)
                    2 -> SecurityTab(s, viewModel, onLocked)
                    3 -> StorageTab(viewModel, config, onEditConnection, onOpenDeviceSetup)
                    4 -> BackupTab(onOpenImport, onOpen1Password)
                    5 -> MaintenanceTab(viewModel, onLocked)
                    6 -> ShortcutsTab()
                    7 -> AboutTab()
                }
            }
        }
    }
}

// ---- Appearance -----------------------------------------------------------

@Composable
private fun AppearanceTab(s: com.vaultguard.app.config.AppSettings, vm: SettingsViewModel, preview: String) {
    SectionCard("Theme") {
        Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
            listOf(
                AppTheme.LIGHT to "Light",
                AppTheme.DARK to "Dark",
                AppTheme.SYSTEM to "System",
                AppTheme.HIGH_CONTRAST to "High Contrast",
            ).forEach { (theme, label) ->
                FilterChip(selected = s.theme == theme, onClick = { vm.update { it.copy(theme = theme) } }, label = { Text(label) })
            }
        }
        SwitchRow("Dynamic colour (Material You)", s.dynamicColor) { vm.update { c -> c.copy(dynamicColor = it) } }
    }

    SectionCard("Accent Colour") {
        Text("Pick an accent to personalise VaultGuard. “Default” follows Material You / the built-in theme. " +
            "(Not applied in High Contrast.)",
            style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
        AccentPicker(selected = s.accentArgb) { argb -> vm.update { it.copy(accentArgb = argb) } }
    }

    SectionCard("Font") {
        Text("Choose a typeface for the whole app.",
            style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
        FlowRowFonts(selected = s.fontChoice) { choice -> vm.update { it.copy(fontChoice = choice) } }
    }

    SectionCard("Default View") {
        Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
            listOf(
                DefaultView.DASHBOARD to "Dashboard",
                DefaultView.ALL_ITEMS to "All Items",
                DefaultView.FAVORITES to "Favourites",
            ).forEach { (view, label) ->
                FilterChip(selected = s.defaultView == view, onClick = { vm.update { it.copy(defaultView = view) } }, label = { Text(label) })
            }
        }
        SwitchRow("Show item count in sidebar", s.showItemCount) { vm.update { c -> c.copy(showItemCount = it) } }
        SwitchRow("Animate list transitions", s.animateTransitions) { vm.update { c -> c.copy(animateTransitions = it) } }
    }

    SectionCard("Password Generator") {
        SliderRow("Default length", s.pwLength.toFloat(), 8f, 64f, "${s.pwLength} characters") {
            vm.update { c -> c.copy(pwLength = it.roundToInt()) }
        }
        SwitchRow("Uppercase (A-Z)", s.pwUpper) { vm.update { c -> c.copy(pwUpper = it) } }
        SwitchRow("Lowercase (a-z)", s.pwLower) { vm.update { c -> c.copy(pwLower = it) } }
        SwitchRow("Digits (0-9)", s.pwDigits) { vm.update { c -> c.copy(pwDigits = it) } }
        SwitchRow("Symbols (!@#…)", s.pwSymbols) { vm.update { c -> c.copy(pwSymbols = it) } }
        SwitchRow("Avoid ambiguous characters", s.pwAvoidAmbiguous) { vm.update { c -> c.copy(pwAvoidAmbiguous = it) } }
        OutlinedButton(onClick = vm::generatePreview) { Text("Generate preview") }
        if (preview.isNotEmpty()) {
            Text(preview, fontFamily = FontFamily.Monospace, style = MaterialTheme.typography.bodyLarge)
        }
    }
}

// ---- Accessibility --------------------------------------------------------

@Composable
private fun AccessibilityTab(s: com.vaultguard.app.config.AppSettings, vm: SettingsViewModel) {
    SectionCard("Display & Zoom") {
        SliderRow("UI zoom", s.uiZoom, 0.8f, 1.5f, "${(s.uiZoom * 100).roundToInt()}%") {
            vm.update { c -> c.copy(uiZoom = it) }
        }
    }
    SectionCard("Text & Menu Size") {
        SliderRow("Base font size", s.fontSizePt.toFloat(), 10f, 24f, "${s.fontSizePt} pt") {
            vm.update { c -> c.copy(fontSizePt = it.roundToInt()) }
        }
    }
    SectionCard("Per-Section Text Size") {
        SliderRow("Menu & navigation", s.scaleMenu, 0.8f, 1.4f, "${(s.scaleMenu * 100).roundToInt()}%") { vm.update { c -> c.copy(scaleMenu = it) } }
        SliderRow("Quick actions", s.scaleQuickActions, 0.8f, 1.4f, "${(s.scaleQuickActions * 100).roundToInt()}%") { vm.update { c -> c.copy(scaleQuickActions = it) } }
        SliderRow("Item details", s.scaleDetails, 0.8f, 1.4f, "${(s.scaleDetails * 100).roundToInt()}%") { vm.update { c -> c.copy(scaleDetails = it) } }
        SliderRow("Dialogs & popups", s.scaleDialogs, 0.8f, 1.4f, "${(s.scaleDialogs * 100).roundToInt()}%") { vm.update { c -> c.copy(scaleDialogs = it) } }
        SliderRow("Global (item cards)", s.scaleGlobal, 0.8f, 1.4f, "${(s.scaleGlobal * 100).roundToInt()}%") { vm.update { c -> c.copy(scaleGlobal = it) } }
        SliderRow("Card icons", s.scaleCardIcons, 0.8f, 1.4f, "${(s.scaleCardIcons * 100).roundToInt()}%") { vm.update { c -> c.copy(scaleCardIcons = it) } }
    }
    SectionCard("Motion & Contrast") {
        SwitchRow("Reduce motion", s.reduceMotion) { vm.update { c -> c.copy(reduceMotion = it) } }
        SwitchRow("High contrast", s.highContrast) { vm.update { c -> c.copy(highContrast = it) } }
    }
}

// ---- Security -------------------------------------------------------------

@Composable
private fun SecurityTab(s: com.vaultguard.app.config.AppSettings, vm: SettingsViewModel, onLocked: () -> Unit) {
    val context = androidx.compose.ui.platform.LocalContext.current
    val autofillManager = remember { context.getSystemService(android.view.autofill.AutofillManager::class.java) }
    var autofillEnabled by remember { mutableStateOf(autofillManager?.hasEnabledAutofillServices() == true) }
    // Re-check when returning from system Settings so the button reflects the real current state.
    val lifecycleOwner = androidx.lifecycle.compose.LocalLifecycleOwner.current
    androidx.compose.runtime.DisposableEffect(lifecycleOwner) {
        val obs = androidx.lifecycle.LifecycleEventObserver { _, event ->
            if (event == androidx.lifecycle.Lifecycle.Event.ON_RESUME)
                autofillEnabled = autofillManager?.hasEnabledAutofillServices() == true
        }
        lifecycleOwner.lifecycle.addObserver(obs)
        onDispose { lifecycleOwner.lifecycle.removeObserver(obs) }
    }
    SectionCard("Autofill") {
        if (autofillEnabled) {
            Text("VaultGuard is your device autofill provider — it can fill logins in apps and browsers.",
                style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
            OutlinedButton(onClick = {
                runCatching { autofillManager?.disableAutofillServices() }
                autofillEnabled = false
            }) { Text("Turn off autofill") }
        } else {
            Text("Set VaultGuard as your device password manager so it can fill logins in apps and browsers.",
                style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
            OutlinedButton(onClick = {
                runCatching {
                    val intent = android.content.Intent(android.provider.Settings.ACTION_REQUEST_SET_AUTOFILL_SERVICE)
                        .setData(android.net.Uri.parse("package:${context.packageName}"))
                    context.startActivity(intent)
                }.onFailure {
                    runCatching { context.startActivity(android.content.Intent(android.provider.Settings.ACTION_REQUEST_SET_AUTOFILL_SERVICE)) }
                }
            }) { Text("Set as autofill provider") }
        }
    }
    SectionCard("Two-Factor Authentication") {
        Text("Enter your account 2FA code at sign-in when enabled, and use biometrics below as a second factor.",
            style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
    }
    AppTotpSection(vm)
    SectionCard("App Lock") {
        SwitchRow("Require unlock on launch", s.requirePasscodeOnLaunch) { vm.update { c -> c.copy(requirePasscodeOnLaunch = it) } }
        SwitchRow("Biometric unlock (2FA)", s.biometricUnlock) { vm.update { c -> c.copy(biometricUnlock = it) } }
        SliderRow("Auto-lock after", s.autoLockMinutes.toFloat(), 0f, 60f,
            if (s.autoLockMinutes == 0) "Never" else "${s.autoLockMinutes} min") {
            vm.update { c -> c.copy(autoLockMinutes = it.roundToInt()) }
        }
    }
    SectionCard("Approvals") {
        Text("2FA-style number matching on important actions — editing, deleting or saving password items " +
            "and categories, and changing your master password. Approve by tapping the matching number on " +
            "this phone. Codes are valid for 60 seconds.",
            style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
        SwitchRow("Number-matching approvals", s.numberMatchApprovals) { vm.update { c -> c.copy(numberMatchApprovals = it) } }
    }
    SectionCard("Clipboard & Deletion") {
        SliderRow("Clear clipboard after", s.clipboardClearSeconds.toFloat(), 0f, 120f,
            if (s.clipboardClearSeconds == 0) "Never" else "${s.clipboardClearSeconds}s") {
            vm.update { c -> c.copy(clipboardClearSeconds = it.roundToInt()) }
        }
        SliderRow("Hide revealed password after", s.passwordAutoHideSeconds.toFloat(), 0f, 120f,
            if (s.passwordAutoHideSeconds == 0) "Off" else "${s.passwordAutoHideSeconds}s") {
            vm.update { c -> c.copy(passwordAutoHideSeconds = it.roundToInt()) }
        }
        SwitchRow("Confirm before deleting", s.confirmOnDelete) { vm.update { c -> c.copy(confirmOnDelete = it) } }
    }
    OutlinedButton(onClick = { vm.lock(); onLocked() }, modifier = Modifier.fillMaxWidth()) { Text("Lock vault now") }
}

// ---- Storage --------------------------------------------------------------

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun StorageTab(vm: SettingsViewModel, config: ConnectionConfig, onEditConnection: () -> Unit, onOpenDeviceSetup: () -> Unit) {
    val apiMessage by vm.apiMessage.collectAsStateWithLifecycle()
    var mode by remember(config.mode) { mutableStateOf(config.mode) }
    var url by remember(config.apiBaseUrl) { mutableStateOf(config.apiBaseUrl) }
    var key by remember { mutableStateOf(vm.currentApiKey()) }

    SectionCard("Connection Mode") {
        SingleChoiceSegmentedButtonRow(Modifier.fillMaxWidth()) {
            SegmentedButton(
                selected = mode == ConnectionMode.API,
                onClick = { mode = ConnectionMode.API },
                shape = SegmentedButtonDefaults.itemShape(0, 2),
            ) { Text("API Server") }
            SegmentedButton(
                selected = mode == ConnectionMode.LOCAL,
                onClick = { mode = ConnectionMode.LOCAL },
                shape = SegmentedButtonDefaults.itemShape(1, 2),
            ) { Text("Local (SQLite)") }
        }
    }

    if (mode == ConnectionMode.API) {
        SectionCard("API Configuration") {
            Text("Generate an API key in the VaultGuard web app (Settings → API Keys), then paste it here.",
                style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
            OutlinedTextField(
                value = url, onValueChange = { url = it },
                label = { Text("API URL") }, placeholder = { Text("https://your-server:7001") },
                singleLine = true, modifier = Modifier.fillMaxWidth(),
            )
            OutlinedTextField(
                value = key, onValueChange = { key = it },
                label = { Text("API Key") }, singleLine = true,
                visualTransformation = PasswordVisualTransformation(), modifier = Modifier.fillMaxWidth(),
            )
            Row(horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                OutlinedButton(onClick = { vm.testApiConfig(url, key) }) { Text("Test") }
                OutlinedButton(onClick = { vm.saveApiConfig(mode, url, key) }) { Text("Save") }
            }
            apiMessage?.let { Text(it, color = MaterialTheme.colorScheme.primary, style = MaterialTheme.typography.bodySmall) }
        }
    } else {
        SectionCard("Local Database") {
            Text("Your vault is stored only on this device in an encrypted SQLite database.",
                style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
            OutlinedButton(onClick = { vm.saveApiConfig(mode, url, key) }) { Text("Use local mode") }
            apiMessage?.let { Text(it, color = MaterialTheme.colorScheme.primary, style = MaterialTheme.typography.bodySmall) }
        }
    }

    SectionCard("Database Provider (Local Mode Only)") {
        Text("Local mode uses an on-device encrypted SQLite database. Server providers (SQL Server, MySQL, PostgreSQL) are selected on the desktop/web app.",
            style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
    }

    if (mode == ConnectionMode.API) {
        SectionCard("Set Up Another Device") {
            Text("Show a QR code that another VaultGuard device (phone or desktop) can scan to configure itself and sign in — no typing the URL or key.",
                style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
            OutlinedButton(onClick = onOpenDeviceSetup) { Text("Show setup QR code") }
        }
    }

    OutlinedButton(onClick = onEditConnection, modifier = Modifier.fillMaxWidth()) { Text("Open full connection screen") }
}

// ---- Backup / Import / Export --------------------------------------------

@Composable
private fun BackupTab(onOpenImport: () -> Unit, onOpen1Password: () -> Unit = {}) {
    SectionCard("Import") {
        Text("Import from 1Password, Bitwarden, LastPass, Chrome, Edge, Firefox, Safari, KeePass, Dashlane, NordPass, Keeper, Enpass, RoboForm and Apple Passwords.",
            style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
        OutlinedButton(onClick = onOpenImport) { Text("Open import") }
    }
    SectionCard("1Password (Connect API)") {
        Text("Connect a 1Password Connect server or Service Account to import your items directly, and save the connection for next time.",
            style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
        OutlinedButton(onClick = onOpen1Password) { Text("Connect & import") }
    }
    SectionCard("Export") {
        Text("Export is available on the desktop and web apps (Settings → Backup, Import & Export).",
            style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
    }
    SectionCard("Cloud Backup") {
        Text("Encrypted cloud backups (Google Drive, OneDrive, iCloud) are configured on the desktop/web apps and sync automatically.",
            style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
    }
}

// ---- Maintenance ----------------------------------------------------------

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun MaintenanceTab(vm: SettingsViewModel, onLocked: () -> Unit) {
    val maintMessage by vm.maintMessage.collectAsStateWithLifecycle()
    // Independent, combinable delete options — tick any mix (e.g. seed data + accounts, or just seed data).
    var delSeed by remember { mutableStateOf(false) }
    var delItems by remember { mutableStateOf(false) }
    var delAccounts by remember { mutableStateOf(false) }
    var confirm by remember { mutableStateOf(false) }
    val anySelected = delSeed || delItems || delAccounts

    SectionCard("Seed Data") {
        Text("Populate the default \"Personal\" vault with sample logins, a card, Wi-Fi and a secure note — " +
            "in the same categories as the desktop app. Local mode only.",
            style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
        OutlinedButton(onClick = vm::seedDemoData) { Text("Seed demo data") }
    }

    // Danger zone — tick everything you want to remove, then confirm.
    SectionCard("Delete Data") {
        Text("Choose one or more things to remove on this device, then confirm.",
            style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)

        CheckRow("Seed / demo data", "Removes only the built-in demo items.", delSeed) { delSeed = it }
        CheckRow("All my items", "Removes every item in the vault (keeps your accounts).", delItems) { delItems = it }
        CheckRow("User accounts", "Removes all local accounts, their items and saved keys — returns to the account picker.", delAccounts) { delAccounts = it }

        Button(
            onClick = { confirm = true },
            enabled = anySelected,
            colors = ButtonDefaults.buttonColors(
                containerColor = MaterialTheme.colorScheme.error,
                contentColor = MaterialTheme.colorScheme.onError,
            ),
            modifier = Modifier.fillMaxWidth(),
        ) {
            Icon(Icons.Filled.DeleteForever, null)
            Text("  Delete selected")
        }

        maintMessage?.let { Text(it, color = MaterialTheme.colorScheme.primary, style = MaterialTheme.typography.bodySmall) }
    }

    SectionCard("Database Management") {
        Text("Schema is kept up to date automatically. In local mode the encrypted SQLite database lives in the app's private storage.",
            style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
    }
    SectionCard("Diagnostics") {
        Text("Logs are written to the app's private files directory. Share them from your device settings if support requests them.",
            style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
    }

    if (confirm) {
        val summary = buildList {
            if (delSeed) add("• Seed / demo data")
            if (delItems) add("• All your items")
            if (delAccounts) add("• All user accounts and saved keys")
        }.joinToString("\n")
        AlertDialog(
            onDismissRequest = { confirm = false },
            icon = { Icon(Icons.Filled.WarningAmber, null, tint = MaterialTheme.colorScheme.error) },
            title = { Text("Delete the selected data?") },
            text = { Text("This will remove:\n$summary\n\nThis can’t be undone.") },
            confirmButton = {
                TextButton(onClick = {
                    confirm = false
                    vm.runDelete(delSeed, delItems, delAccounts, onReset = onLocked)
                }) { Text("Delete", color = MaterialTheme.colorScheme.error) }
            },
            dismissButton = { TextButton(onClick = { confirm = false }) { Text("Cancel") } },
        )
    }
}

/** A labelled checkbox row with a supporting description, used for the multi-select delete options. */
@Composable
private fun CheckRow(title: String, subtitle: String, checked: Boolean, onChange: (Boolean) -> Unit) {
    Row(Modifier.fillMaxWidth(), verticalAlignment = androidx.compose.ui.Alignment.Top) {
        androidx.compose.material3.Checkbox(checked = checked, onCheckedChange = onChange)
        Column(Modifier.padding(top = 12.dp)) {
            Text(title, style = MaterialTheme.typography.bodyMedium)
            Text(subtitle, style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
        }
    }
}

// ---- Shortcuts ------------------------------------------------------------

@Composable
private fun ShortcutsTab() {
    SectionCard("Keyboard Shortcuts") {
        Text("Keyboard shortcuts apply to the desktop (WPF) app. On mobile, use search and the navigation drawer.",
            style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
    }
    listOf(
        "Ctrl+N" to "New item",
        "Ctrl+F" to "Search",
        "Ctrl+C" to "Copy password",
        "Ctrl+L" to "Lock vault",
    ).forEach { (k, v) ->
        Row(Modifier.fillMaxWidth(), Arrangement.SpaceBetween) {
            Text(v)
            Text(k, fontFamily = FontFamily.Monospace, color = MaterialTheme.colorScheme.primary)
        }
    }
}

// ---- About ----------------------------------------------------------------

@Composable
private fun AboutTab() {
    SectionCard("VaultGuard for Android") {
        Text("Version 1.0.0", style = MaterialTheme.typography.bodyMedium)
        Text("A native companion to the VaultGuard desktop and web apps. Feature parity with the WPF app.",
            style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
    }
}

// ---- App authenticator (TOTP) 2FA ----------------------------------------

@Composable
private fun AppTotpSection(vm: SettingsViewModel) {
    val enabled by vm.appTotpEnabled.collectAsStateWithLifecycle()
    val setup by vm.totpSetup.collectAsStateWithLifecycle()
    val clipboard = LocalClipboardManager.current
    var code by remember { mutableStateOf("") }
    var message by remember { mutableStateOf<String?>(null) }

    SectionCard("Authenticator App (App 2FA)") {
        when {
            enabled -> {
                Text("Enabled — you'll enter a 6-digit code from your authenticator each time you unlock.",
                    style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
                OutlinedButton(onClick = { vm.disableAppTotp() }) { Text("Disable app 2FA") }
            }
            setup == null -> {
                Text("Require a code from Google Authenticator, Microsoft Authenticator, Authy or any TOTP app when you unlock this app.",
                    style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
                OutlinedButton(onClick = { vm.startTotpSetup() }) { Text("Set up app 2FA") }
            }
            else -> {
                val s = setup!!
                Text("Scan this QR code with your authenticator app (Google Authenticator, Microsoft Authenticator, Authy, etc.) to set up 2FA.",
                    style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)

                Row(
                    modifier = Modifier.fillMaxWidth().padding(vertical = 8.dp),
                    horizontalArrangement = Arrangement.Center
                ) {
                    QrImage(s.otpauthUri, sizeDp = 200.dp)
                }

                Text("Or add the key manually:",
                    style = MaterialTheme.typography.labelSmall, color = MaterialTheme.colorScheme.primary)
                CopyKeyRow("Setup key", s.secret) { clipboard.setText(AnnotatedString(s.secret)) }
                CopyKeyRow("otpauth:// link", s.otpauthUri) { clipboard.setText(AnnotatedString(s.otpauthUri)) }
                OutlinedTextField(
                    value = code, onValueChange = { code = it },
                    label = { Text("Enter code to confirm") }, singleLine = true,
                    keyboardOptions = androidx.compose.foundation.text.KeyboardOptions(keyboardType = KeyboardType.Number),
                    modifier = Modifier.fillMaxWidth(),
                )
                Row(horizontalArrangement = Arrangement.spacedBy(12.dp), modifier = Modifier.padding(top = 8.dp)) {
                    OutlinedButton(
                        onClick = { vm.cancelTotpSetup(); code = ""; message = null },
                        modifier = Modifier.weight(1f)
                    ) { Text("Cancel") }
                    androidx.compose.material3.Button(
                        onClick = {
                            message = if (vm.confirmTotpSetup(code)) "App 2FA enabled." else "That code didn't match — try again."
                        },
                        modifier = Modifier.weight(1f)
                    ) { Text("Verify & enable") }
                }
                message?.let { Text(it, style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.primary) }
            }
        }
    }
}

@Composable
private fun CopyKeyRow(label: String, value: String, onCopy: () -> Unit) {
    Column(Modifier.fillMaxWidth()) {
        Text(label, style = MaterialTheme.typography.labelSmall, color = MaterialTheme.colorScheme.primary)
        Row(Modifier.fillMaxWidth(), Arrangement.SpaceBetween, Alignment.CenterVertically) {
            Text(
                value,
                style = MaterialTheme.typography.bodySmall,
                fontFamily = FontFamily.Monospace,
                modifier = Modifier.weight(1f).padding(end = 8.dp),
                maxLines = 2,
            )
            IconButton(onClick = onCopy) { Icon(Icons.Filled.ContentCopy, contentDescription = "Copy $label") }
        }
    }
}

// ---- Reusable pieces ------------------------------------------------------

@Composable
private fun SectionCard(title: String, content: @Composable () -> Unit) {
    Card(Modifier.fillMaxWidth()) {
        Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(10.dp)) {
            Text(title, style = MaterialTheme.typography.titleMedium, color = MaterialTheme.colorScheme.primary)
            HorizontalDivider()
            content()
        }
    }
}

@Composable
private fun SwitchRow(label: String, checked: Boolean, onChange: (Boolean) -> Unit) {
    Row(Modifier.fillMaxWidth(), Arrangement.SpaceBetween, androidx.compose.ui.Alignment.CenterVertically) {
        Text(label, modifier = Modifier.padding(end = 8.dp))
        Switch(checked = checked, onCheckedChange = onChange)
    }
}

@Composable
private fun SliderRow(label: String, value: Float, min: Float, max: Float, valueLabel: String, onChange: (Float) -> Unit) {
    Column(Modifier.fillMaxWidth()) {
        Row(Modifier.fillMaxWidth(), Arrangement.SpaceBetween) {
            Text(label)
            Text(valueLabel, color = MaterialTheme.colorScheme.primary)
        }
        Slider(
            value = value, onValueChange = onChange, valueRange = min..max,
            modifier = Modifier.semantics { contentDescription = "$label, $valueLabel" },
        )
    }
}

// ---- Accent colour picker (palettes + specific colours) -------------------

private data class AccentPreset(val argb: Long, val name: String)

private val AccentPresets = listOf(
    AccentPreset(0L, "Default"),
    AccentPreset(0xFF0A84FF, "Blue"),
    AccentPreset(0xFF5E5CE6, "Indigo"),
    AccentPreset(0xFF30B0C7, "Teal"),
    AccentPreset(0xFF34C759, "Green"),
    AccentPreset(0xFFFF9F0A, "Orange"),
    AccentPreset(0xFFFF375F, "Pink"),
    AccentPreset(0xFFBF5AF2, "Purple"),
    AccentPreset(0xFFFF453A, "Red"),
)

private fun onColorForArgb(argb: Long): Color =
    if (Color(argb).luminance() > 0.5f) Color.Black else Color.White

@OptIn(ExperimentalLayoutApi::class)
@Composable
private fun FlowRowFonts(selected: FontChoice, onPick: (FontChoice) -> Unit) {
    FlowRow(horizontalArrangement = Arrangement.spacedBy(8.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
        FontChoice.entries.forEach { fc ->
            FilterChip(
                selected = selected == fc,
                onClick = { onPick(fc) },
                label = { Text(fc.label) },
                modifier = Modifier.semantics { stateDescription = if (selected == fc) "Selected" else "Not selected" },
            )
        }
    }
}

@OptIn(ExperimentalLayoutApi::class)
@Composable
private fun AccentPicker(selected: Long, onPick: (Long) -> Unit) {
    val isCustom = selected != 0L && AccentPresets.none { it.argb == selected }
    var showCustom by remember(selected) { mutableStateOf(isCustom) }

    FlowRow(horizontalArrangement = Arrangement.spacedBy(10.dp), verticalArrangement = Arrangement.spacedBy(10.dp)) {
        AccentPresets.forEach { p ->
            AccentSwatch(argb = p.argb, name = p.name, selected = selected == p.argb) {
                showCustom = false; onPick(p.argb)
            }
        }
        AccentSwatch(argb = if (isCustom) selected else 0xFF8E8E93, name = "Custom", selected = isCustom, isCustomTile = true) {
            showCustom = true
        }
    }

    if (showCustom) {
        CustomColorEditor(initial = if (isCustom) selected else 0xFF0A84FF, onApply = onPick)
    }
}

@Composable
private fun AccentSwatch(argb: Long, name: String, selected: Boolean, isCustomTile: Boolean = false, onClick: () -> Unit) {
    val isDefault = argb == 0L
    val fill = if (isDefault) MaterialTheme.colorScheme.surfaceVariant else Color(argb)
    Box(
        Modifier
            .size(44.dp)
            .clip(CircleShape)
            .background(fill)
            .border(
                width = if (selected) 3.dp else 1.dp,
                color = if (selected) MaterialTheme.colorScheme.onSurface else MaterialTheme.colorScheme.outlineVariant,
                shape = CircleShape,
            )
            .clickable(onClick = onClick)
            .semantics {
                contentDescription = "$name accent colour"
                stateDescription = if (selected) "Selected" else "Not selected"
            },
        contentAlignment = Alignment.Center,
    ) {
        when {
            selected -> Icon(Icons.Filled.Check, null,
                tint = if (isDefault) MaterialTheme.colorScheme.onSurface else onColorForArgb(argb))
            isDefault -> Text("A", color = MaterialTheme.colorScheme.onSurfaceVariant, style = MaterialTheme.typography.labelLarge)
            isCustomTile -> Text("+", color = Color.White, style = MaterialTheme.typography.titleMedium)
        }
    }
}

/** RGB editor for a specific custom accent colour (with a live preview + hex readout). */
@Composable
private fun CustomColorEditor(initial: Long, onApply: (Long) -> Unit) {
    var r by remember(initial) { mutableStateOf(((initial shr 16) and 0xFF).toInt()) }
    var g by remember(initial) { mutableStateOf(((initial shr 8) and 0xFF).toInt()) }
    var b by remember(initial) { mutableStateOf((initial and 0xFF).toInt()) }
    val argb = 0xFF000000L or (r.toLong() shl 16) or (g.toLong() shl 8) or b.toLong()

    Column(Modifier.fillMaxWidth().padding(top = 10.dp), verticalArrangement = Arrangement.spacedBy(6.dp)) {
        Row(verticalAlignment = Alignment.CenterVertically) {
            Box(
                Modifier.size(36.dp).clip(CircleShape).background(Color(argb))
                    .semantics { contentDescription = "Selected custom colour preview" },
            )
            Text(
                "#%06X".format(argb and 0xFFFFFF),
                fontFamily = FontFamily.Monospace,
                modifier = Modifier.padding(start = 12.dp),
            )
        }
        ColorSlider("Red", r) { r = it }
        ColorSlider("Green", g) { g = it }
        ColorSlider("Blue", b) { b = it }
        Button(onClick = { onApply(argb) }, modifier = Modifier.fillMaxWidth()) { Text("Use this colour") }
    }
}

@Composable
private fun ColorSlider(label: String, value: Int, onChange: (Int) -> Unit) {
    Column(Modifier.fillMaxWidth()) {
        Row(Modifier.fillMaxWidth(), Arrangement.SpaceBetween) { Text(label); Text("$value") }
        Slider(
            value = value.toFloat(),
            onValueChange = { onChange(it.roundToInt()) },
            valueRange = 0f..255f,
            modifier = Modifier.semantics { contentDescription = "$label channel, $value of 255" },
        )
    }
}
