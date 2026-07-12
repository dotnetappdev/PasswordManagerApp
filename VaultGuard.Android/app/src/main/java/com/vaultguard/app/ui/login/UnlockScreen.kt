package com.vaultguard.app.ui.login

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.horizontalScroll
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Fingerprint
import androidx.compose.material.icons.filled.Lock
import androidx.compose.material.icons.filled.Shield
import androidx.compose.material3.Button
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.unit.dp
import androidx.fragment.app.FragmentActivity
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.vaultguard.app.config.Account
import com.vaultguard.app.config.ConnectionMode
import com.vaultguard.app.config.LocalProfile
import com.vaultguard.app.security.Biometrics
import com.vaultguard.app.ui.theme.VgAccent

@Composable
fun UnlockScreen(
    onUnlocked: () -> Unit,
    onEditConnection: () -> Unit,
    viewModel: UnlockViewModel = hiltViewModel(),
) {
    val state by viewModel.state.collectAsStateWithLifecycle()
    val context = LocalContext.current

    LaunchedEffect(state.unlocked) { if (state.unlocked) onUnlocked() }
    LaunchedEffect(state.awaitingBiometric) {
        if (state.awaitingBiometric) {
            val activity = context as? FragmentActivity
            if (activity != null) Biometrics.prompt(activity) { ok -> viewModel.biometricResult(ok) }
            else viewModel.biometricResult(true)
        }
    }
    // Bank-style: when quick unlock is set up, prompt for the fingerprint automatically once on open.
    var autoPrompted by rememberSaveable { mutableStateOf(false) }
    LaunchedEffect(state.hasQuickUnlock) {
        if (state.hasQuickUnlock && !autoPrompted && !state.showPasscode && !state.offerQuickSetup) {
            autoPrompted = true
            val activity = context as? FragmentActivity
            if (activity != null && Biometrics.isAvailable(activity)) {
                Biometrics.prompt(activity, subtitle = "Use your fingerprint to unlock your vault") { ok ->
                    if (ok) viewModel.biometricQuickUnlock()
                }
            }
        }
    }

    Column(
        modifier = Modifier.fillMaxSize().padding(24.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp, Alignment.CenterVertically),
        horizontalAlignment = Alignment.CenterHorizontally,
    ) {
        // Branded gradient logo tile.
        Box(
            Modifier.size(84.dp).clip(RoundedCornerShape(24.dp))
                .background(Brush.linearGradient(listOf(VgAccent, MaterialTheme.colorScheme.primary))),
            contentAlignment = Alignment.Center,
        ) {
            Icon(Icons.Filled.Shield, null, tint = androidx.compose.ui.graphics.Color.White, modifier = Modifier.size(44.dp))
        }
        Text("VaultGuard", style = MaterialTheme.typography.headlineMedium, fontWeight = FontWeight.Bold)
        Text(
            when {
                state.awaitingBiometric -> "Confirm your identity"
                state.mode != ConnectionMode.LOCAL -> "Sign in to your vault"
                !state.localVaultExists -> "Create your vault — choose a master password"
                else -> "Unlock your local vault"
            },
            style = MaterialTheme.typography.bodyMedium,
            color = MaterialTheme.colorScheme.onSurfaceVariant,
            textAlign = androidx.compose.ui.text.style.TextAlign.Center,
        )

        if (state.awaitingBiometric) {
            Icon(Icons.Filled.Fingerprint, null, tint = VgAccent, modifier = Modifier.size(64.dp).padding(top = 8.dp))
            state.error?.let { Text(it, color = MaterialTheme.colorScheme.error) }
            return@Column
        }

        // Numeric passcode pad — either setting a new passcode or unlocking with the existing one.
        if (state.showPasscode) {
            PasscodePad(
                title = when {
                    state.settingPasscode && state.confirmingPasscode -> "Confirm your passcode"
                    state.settingPasscode -> "Create a 6-digit passcode"
                    else -> "Enter passcode"
                },
                subtitle = when {
                    state.settingPasscode && state.confirmingPasscode -> "Re-enter the 6 digits to confirm"
                    state.settingPasscode -> "You'll use this to unlock quickly next time"
                    else -> null
                },
                input = state.passcodeInput,
                error = state.error,
                onDigit = viewModel::passcodeDigit,
                onBackspace = viewModel::passcodeBackspace,
                onCancel = viewModel::cancelPasscode,
            )
            return@Column
        }

        // Offer to set up quick unlock right after a successful master-password unlock.
        if (state.offerQuickSetup) {
            QuickSetupOffer(
                biometricAvailable = Biometrics.isAvailable(context),
                onEnableFingerprint = viewModel::enableFingerprintFromOffer,
                onSetPasscode = viewModel::startSetPasscode,
                onSkip = viewModel::skipQuickSetup,
            )
            return@Column
        }

        // Create a new LOCAL account (profile).
        if (state.showCreateAccount) {
            CreateAccountForm(
                name = state.newAccountName,
                password = state.password,
                error = state.error,
                loading = state.loading,
                onName = viewModel::setNewAccountName,
                onPassword = viewModel::setPassword,
                onCreate = viewModel::createAccount,
                onCancel = viewModel::cancelCreateAccount,
            )
            return@Column
        }

        // Primary quick-unlock actions when a fingerprint/passcode has been set up.
        if (state.hasQuickUnlock) {
            QuickUnlockRow(
                hasPasscode = state.hasPasscode,
                loading = state.loading,
                onFingerprint = {
                    val activity = context as? FragmentActivity
                    if (activity != null) Biometrics.prompt(activity, subtitle = "Use your fingerprint to unlock your vault") { ok ->
                        if (ok) viewModel.biometricQuickUnlock()
                    }
                },
                onPasscode = viewModel::showPasscodeUnlock,
            )
            state.error?.let { Text(it, color = MaterialTheme.colorScheme.error, textAlign = androidx.compose.ui.text.style.TextAlign.Center) }
        }

        if (state.needsAppTotp || state.needsTwoFactor) {
            val isAppTotp = state.needsAppTotp
            Text(
                if (isAppTotp) "Enter the 6-digit code from your authenticator app"
                else "Two-factor authentication is required. Enter your 2FA code.",
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
                textAlign = androidx.compose.ui.text.style.TextAlign.Center,
            )
            OutlinedTextField(
                value = if (isAppTotp) state.appTotpCode else state.twoFactorCode,
                onValueChange = if (isAppTotp) viewModel::setAppTotpCode else viewModel::setTwoFactor,
                label = { Text("2FA Code") },
                singleLine = true,
                shape = RoundedCornerShape(14.dp),
                keyboardOptions = androidx.compose.foundation.text.KeyboardOptions(keyboardType = KeyboardType.Number),
                modifier = Modifier.fillMaxWidth(),
            )
            state.error?.let { Text(it, color = MaterialTheme.colorScheme.error) }
            Button(
                onClick = if (isAppTotp) viewModel::submitAppTotp else viewModel::submit,
                shape = RoundedCornerShape(14.dp),
                modifier = Modifier.fillMaxWidth().height(52.dp),
            ) { Text("Verify") }

            if (state.needsTwoFactor) {
                TextButton(onClick = { viewModel.setTwoFactor(""); viewModel.setPassword("") /* reset */ }) {
                    Text("Back to sign in")
                }
            }
            return@Column
        }

        if (state.accounts.isNotEmpty()) {
            AccountSwitcher(
                accounts = state.accounts,
                currentId = state.currentAccountId,
                onSelect = viewModel::selectAccount,
                onAdd = onEditConnection,
            )
        }

        if (state.mode == ConnectionMode.LOCAL) {
            // WPF-style local account picker (admin / parent / user / child + your own accounts).
            LocalProfilesRow(
                profiles = state.localProfiles,
                selectedId = state.selectedProfileId,
                onSelect = viewModel::selectProfile,
                onAdd = viewModel::showCreateAccount,
            )
            androidx.compose.material3.Surface(
                shape = RoundedCornerShape(10.dp),
                color = MaterialTheme.colorScheme.surfaceVariant,
                modifier = Modifier.padding(top = 2.dp),
            ) {
                Text(
                    if (state.localVaultExists) "Enter this account's master password"
                    else "Demo master key:  ${DefaultAccounts.commonMasterKey}",
                    style = MaterialTheme.typography.labelMedium,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                    modifier = Modifier.padding(horizontal = 12.dp, vertical = 6.dp),
                )
            }
        } else {
            SeededAccountsRow(
                mode = state.mode,
                onPick = { email ->
                    viewModel.setEmail(email)
                    viewModel.setPassword(DefaultAccounts.commonMasterKey)
                }
            )
        }

        if (state.mode == ConnectionMode.API) {
            OutlinedTextField(
                value = state.email,
                onValueChange = viewModel::setEmail,
                label = { Text("Email") },
                singleLine = true,
                shape = RoundedCornerShape(14.dp),
                keyboardOptions = androidx.compose.foundation.text.KeyboardOptions(keyboardType = KeyboardType.Email),
                modifier = Modifier.fillMaxWidth(),
            )
        }

        val selectedName = state.localProfiles.firstOrNull { it.id == state.selectedProfileId }?.name
        // The master key is the most important field on the screen — give it a large, comfortable
        // touch target and larger type so it reads clearly on mobile.
        OutlinedTextField(
            value = state.password,
            onValueChange = viewModel::setPassword,
            label = {
                Text(
                    if (state.mode == ConnectionMode.LOCAL && selectedName != null) "Master password · $selectedName"
                    else "Master password"
                )
            },
            leadingIcon = { Icon(Icons.Filled.Lock, null, tint = VgAccent) },
            singleLine = true,
            shape = RoundedCornerShape(16.dp),
            textStyle = MaterialTheme.typography.titleLarge,
            visualTransformation = PasswordVisualTransformation(),
            modifier = Modifier.fillMaxWidth().heightIn(min = 68.dp),
        )

        state.error?.let { Text(it, color = MaterialTheme.colorScheme.error, textAlign = androidx.compose.ui.text.style.TextAlign.Center) }
        state.info?.let { Text(it, color = VgAccent, textAlign = androidx.compose.ui.text.style.TextAlign.Center) }

        Button(
            onClick = viewModel::submit,
            enabled = !state.loading,
            shape = RoundedCornerShape(14.dp),
            modifier = Modifier.fillMaxWidth().height(52.dp),
        ) {
            if (state.loading) CircularProgressIndicator(Modifier.size(20.dp), strokeWidth = 2.dp)
            else Text(
                when {
                    state.mode != ConnectionMode.LOCAL -> "Sign in"
                    !state.localVaultExists -> "Create vault"
                    else -> "Unlock"
                }
            )
        }

        // Recovery for a local vault whose master password was forgotten / set differently earlier.
        if (state.canResetLocal && state.mode == ConnectionMode.LOCAL) {
            TextButton(onClick = viewModel::resetLocalVault) {
                Text("Forgot master password? Reset local vault", color = MaterialTheme.colorScheme.error)
            }
        }

        TextButton(onClick = onEditConnection) { Text("Connection settings") }
        Spacer(Modifier.height(8.dp))
    }
}

@Composable
private fun AccountSwitcher(
    accounts: List<Account>,
    currentId: String?,
    onSelect: (String) -> Unit,
    onAdd: () -> Unit,
) {
    // Saved accounts live in a soft card, like the WPF profile picker.
    androidx.compose.material3.Surface(
        shape = RoundedCornerShape(20.dp),
        color = MaterialTheme.colorScheme.surface,
        tonalElevation = 2.dp,
        shadowElevation = 2.dp,
        modifier = Modifier.fillMaxWidth(),
    ) {
        Row(
            Modifier.fillMaxWidth().horizontalScroll(rememberScrollState()).padding(horizontal = 16.dp, vertical = 14.dp),
            horizontalArrangement = Arrangement.spacedBy(18.dp),
            verticalAlignment = Alignment.Top,
        ) {
            accounts.forEach { account ->
                ProfileAvatar(
                    label = account.email ?: account.label,
                    initials = account.initials,
                    color = avatarColorFor(account.id + (account.email ?: account.label)),
                    selected = account.id == currentId,
                    onClick = { onSelect(account.id) },
                )
            }
            // Add-account tile.
            AvatarTile(
                label = "Add",
                selected = false,
                ring = MaterialTheme.colorScheme.outline,
                onClick = onAdd,
            ) {
                Box(
                    Modifier.matchParentSize().clip(CircleShape).background(MaterialTheme.colorScheme.surfaceVariant),
                    contentAlignment = Alignment.Center,
                ) { Icon(Icons.Filled.Add, contentDescription = "Add account", tint = MaterialTheme.colorScheme.onSurfaceVariant) }
            }
        }
    }
}

@Composable
private fun SeededAccountsRow(mode: ConnectionMode, onPick: (String) -> Unit) {
    Column(horizontalAlignment = Alignment.CenterHorizontally, modifier = Modifier.fillMaxWidth()) {
        Text(
            if (mode == ConnectionMode.API) "Choose a demo account" else "Quick unlock",
            style = MaterialTheme.typography.titleSmall,
            fontWeight = FontWeight.SemiBold,
            color = MaterialTheme.colorScheme.onSurface,
            modifier = Modifier.padding(bottom = 10.dp).align(Alignment.Start),
        )
        Row(
            Modifier.fillMaxWidth().horizontalScroll(rememberScrollState()),
            horizontalArrangement = Arrangement.spacedBy(18.dp),
        ) {
            if (mode == ConnectionMode.API) {
                DefaultAccounts.users.forEach { user ->
                    RoleAvatar(
                        role = user.role,
                        icon = user.icon,
                        color = user.color,
                        onClick = { onPick(user.email) },
                    )
                }
            } else {
                RoleAvatar(
                    role = "Master",
                    icon = Icons.Filled.Shield,
                    color = VgAccent,
                    onClick = { onPick("") },
                )
            }
        }
        // Demo master key hint, framed as a subtle pill.
        androidx.compose.material3.Surface(
            shape = RoundedCornerShape(10.dp),
            color = MaterialTheme.colorScheme.surfaceVariant,
            modifier = Modifier.padding(top = 12.dp),
        ) {
            Text(
                "Demo master key:  ${DefaultAccounts.commonMasterKey}",
                style = MaterialTheme.typography.labelMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
                modifier = Modifier.padding(horizontal = 12.dp, vertical = 6.dp),
            )
        }
    }
}

/** A role tile (icon in a colored gradient circle) used for the seeded demo accounts. */
@Composable
private fun RoleAvatar(role: String, icon: androidx.compose.ui.graphics.vector.ImageVector, color: androidx.compose.ui.graphics.Color, onClick: () -> Unit) {
    AvatarTile(label = role, selected = false, ring = androidx.compose.ui.graphics.Color.Transparent, onClick = onClick) {
        Box(
            Modifier.matchParentSize().clip(CircleShape)
                .background(Brush.linearGradient(listOf(color, color.copy(alpha = 0.72f)))),
            contentAlignment = Alignment.Center,
        ) { Icon(icon, contentDescription = role, tint = androidx.compose.ui.graphics.Color.White, modifier = Modifier.size(32.dp)) }
    }
}

/** A saved-account tile (initials in a colored gradient circle) with a selection ring. */
@Composable
private fun ProfileAvatar(label: String, initials: String, color: androidx.compose.ui.graphics.Color, selected: Boolean, onClick: () -> Unit) {
    AvatarTile(label = label, selected = selected, ring = VgAccent, onClick = onClick) {
        Box(
            Modifier.matchParentSize().clip(CircleShape)
                .background(Brush.linearGradient(listOf(color, color.copy(alpha = 0.72f)))),
            contentAlignment = Alignment.Center,
        ) {
            Text(initials, style = MaterialTheme.typography.titleLarge, fontWeight = FontWeight.Bold, color = androidx.compose.ui.graphics.Color.White)
        }
    }
}

/**
 * Shared avatar-tile scaffold: a 56dp circle (drawn by [content]) with an optional selection ring and a
 * caption below. Keeps every avatar on the login screen visually consistent.
 */
@Composable
private fun AvatarTile(
    label: String,
    selected: Boolean,
    ring: androidx.compose.ui.graphics.Color,
    onClick: () -> Unit,
    content: @Composable androidx.compose.foundation.layout.BoxScope.() -> Unit,
) {
    Column(
        horizontalAlignment = Alignment.CenterHorizontally,
        modifier = Modifier.width(84.dp).clickable(onClick = onClick),
    ) {
        Box(
            Modifier.size(72.dp)
                .border(
                    width = if (selected) 3.dp else 0.dp,
                    color = if (selected) ring else androidx.compose.ui.graphics.Color.Transparent,
                    shape = CircleShape,
                )
                .padding(if (selected) 3.dp else 0.dp),
            content = content,
        )
        Spacer(Modifier.height(8.dp))
        Text(
            label,
            style = MaterialTheme.typography.labelMedium,
            color = if (selected) VgAccent else MaterialTheme.colorScheme.onSurfaceVariant,
            fontWeight = if (selected) FontWeight.SemiBold else FontWeight.Normal,
            maxLines = 1,
            overflow = androidx.compose.ui.text.style.TextOverflow.Ellipsis,
        )
    }
}

/** Deterministic pleasant avatar color from an arbitrary key (saved accounts have no assigned color). */
private fun avatarColorFor(key: String): androidx.compose.ui.graphics.Color {
    val palette = listOf(
        androidx.compose.ui.graphics.Color(0xFF2563EB),
        androidx.compose.ui.graphics.Color(0xFF7C3AED),
        androidx.compose.ui.graphics.Color(0xFF059669),
        androidx.compose.ui.graphics.Color(0xFFDB2777),
        androidx.compose.ui.graphics.Color(0xFFD97706),
        androidx.compose.ui.graphics.Color(0xFF0891B2),
    )
    return palette[(key.hashCode() and 0x7fffffff) % palette.size]
}

// ── Quick unlock (fingerprint / passcode) ────────────────────────────────────

@Composable
private fun QuickUnlockRow(
    hasPasscode: Boolean,
    loading: Boolean,
    onFingerprint: () -> Unit,
    onPasscode: () -> Unit,
) {
    Column(horizontalAlignment = Alignment.CenterHorizontally, modifier = Modifier.fillMaxWidth()) {
        Button(
            onClick = onFingerprint,
            enabled = !loading,
            shape = RoundedCornerShape(14.dp),
            modifier = Modifier.fillMaxWidth().height(52.dp),
        ) {
            if (loading) CircularProgressIndicator(Modifier.size(20.dp), strokeWidth = 2.dp)
            else {
                Icon(Icons.Filled.Fingerprint, null, modifier = Modifier.size(22.dp))
                Spacer(Modifier.width(8.dp))
                Text("Unlock with fingerprint")
            }
        }
        if (hasPasscode) {
            TextButton(onClick = onPasscode) { Text("Use passcode instead") }
        }
        Text(
            "or enter your master password below",
            style = MaterialTheme.typography.labelSmall,
            color = MaterialTheme.colorScheme.onSurfaceVariant,
            modifier = Modifier.padding(top = 4.dp),
        )
    }
}

@Composable
private fun QuickSetupOffer(
    biometricAvailable: Boolean,
    onEnableFingerprint: () -> Unit,
    onSetPasscode: () -> Unit,
    onSkip: () -> Unit,
) {
    androidx.compose.material3.Surface(
        shape = RoundedCornerShape(20.dp),
        color = MaterialTheme.colorScheme.surface,
        tonalElevation = 2.dp,
        shadowElevation = 2.dp,
        modifier = Modifier.fillMaxWidth(),
    ) {
        Column(
            Modifier.padding(20.dp),
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.spacedBy(12.dp),
        ) {
            Icon(Icons.Filled.Fingerprint, null, tint = VgAccent, modifier = Modifier.size(40.dp))
            Text("Unlock faster next time", style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.SemiBold)
            Text(
                "Skip typing your master password — unlock with your fingerprint or a 6-digit passcode.",
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
                textAlign = androidx.compose.ui.text.style.TextAlign.Center,
            )
            if (biometricAvailable) {
                Button(
                    onClick = onEnableFingerprint,
                    shape = RoundedCornerShape(14.dp),
                    modifier = Modifier.fillMaxWidth().height(50.dp),
                ) {
                    Icon(Icons.Filled.Fingerprint, null, modifier = Modifier.size(20.dp))
                    Spacer(Modifier.width(8.dp))
                    Text("Enable fingerprint")
                }
            }
            androidx.compose.material3.OutlinedButton(
                onClick = onSetPasscode,
                shape = RoundedCornerShape(14.dp),
                modifier = Modifier.fillMaxWidth().height(50.dp),
            ) { Text("Set a passcode") }
            TextButton(onClick = onSkip) { Text("Not now") }
        }
    }
}

@Composable
private fun PasscodePad(
    title: String,
    input: String,
    error: String?,
    onDigit: (Char) -> Unit,
    onBackspace: () -> Unit,
    onCancel: () -> Unit,
    subtitle: String? = null,
) {
    Column(
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.spacedBy(18.dp),
        modifier = Modifier.fillMaxWidth(),
    ) {
        Text(title, style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.SemiBold)
        subtitle?.let {
            Text(
                it,
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
                textAlign = androidx.compose.ui.text.style.TextAlign.Center,
            )
        }
        Row(horizontalArrangement = Arrangement.spacedBy(14.dp)) {
            repeat(6) { i ->
                Box(
                    Modifier.size(16.dp).clip(CircleShape)
                        .background(if (i < input.length) VgAccent else MaterialTheme.colorScheme.surfaceVariant),
                )
            }
        }
        error?.let { Text(it, color = MaterialTheme.colorScheme.error) }
        listOf(listOf('1', '2', '3'), listOf('4', '5', '6'), listOf('7', '8', '9')).forEach { row ->
            Row(horizontalArrangement = Arrangement.spacedBy(24.dp)) {
                row.forEach { d -> PadKey(d.toString()) { onDigit(d) } }
            }
        }
        Row(horizontalArrangement = Arrangement.spacedBy(24.dp), verticalAlignment = Alignment.CenterVertically) {
            Box(Modifier.size(72.dp))
            PadKey("0") { onDigit('0') }
            Box(
                Modifier.size(72.dp).clip(CircleShape).clickable(onClick = onBackspace),
                contentAlignment = Alignment.Center,
            ) { Text("⌫", style = MaterialTheme.typography.headlineSmall) }
        }
        TextButton(onClick = onCancel) { Text("Cancel") }
    }
}

@Composable
private fun PadKey(label: String, onClick: () -> Unit) {
    Box(
        Modifier.size(72.dp).clip(CircleShape)
            .background(MaterialTheme.colorScheme.surfaceVariant)
            .clickable(onClick = onClick),
        contentAlignment = Alignment.Center,
    ) { Text(label, style = MaterialTheme.typography.headlineSmall) }
}

// ── Local account picker (WPF-style) ─────────────────────────────────────────

@Composable
private fun LocalProfilesRow(
    profiles: List<LocalProfile>,
    selectedId: String,
    onSelect: (String) -> Unit,
    onAdd: () -> Unit,
) {
    Column(horizontalAlignment = Alignment.CenterHorizontally, modifier = Modifier.fillMaxWidth()) {
        Text(
            "Choose an account",
            style = MaterialTheme.typography.titleSmall,
            fontWeight = FontWeight.SemiBold,
            color = MaterialTheme.colorScheme.onSurface,
            modifier = Modifier.padding(bottom = 10.dp).align(Alignment.Start),
        )
        Row(
            Modifier.fillMaxWidth().horizontalScroll(rememberScrollState()),
            horizontalArrangement = Arrangement.spacedBy(18.dp),
        ) {
            profiles.forEach { p ->
                AvatarTile(label = p.name, selected = p.id == selectedId, ring = VgAccent, onClick = { onSelect(p.id) }) {
                    val c = androidx.compose.ui.graphics.Color(p.colorArgb)
                    Box(
                        Modifier.matchParentSize().clip(CircleShape)
                            .background(Brush.linearGradient(listOf(c, c.copy(alpha = 0.72f)))),
                        contentAlignment = Alignment.Center,
                    ) {
                        Text(p.initials, style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.Bold, color = androidx.compose.ui.graphics.Color.White)
                    }
                }
            }
            AvatarTile(label = "Add", selected = false, ring = MaterialTheme.colorScheme.outline, onClick = onAdd) {
                Box(
                    Modifier.matchParentSize().clip(CircleShape).background(MaterialTheme.colorScheme.surfaceVariant),
                    contentAlignment = Alignment.Center,
                ) { Icon(Icons.Filled.Add, contentDescription = "Add account", tint = MaterialTheme.colorScheme.onSurfaceVariant) }
            }
        }
    }
}

@Composable
private fun CreateAccountForm(
    name: String,
    password: String,
    error: String?,
    loading: Boolean,
    onName: (String) -> Unit,
    onPassword: (String) -> Unit,
    onCreate: () -> Unit,
    onCancel: () -> Unit,
) {
    Column(
        Modifier.fillMaxWidth(),
        verticalArrangement = Arrangement.spacedBy(14.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
    ) {
        Text("Create a local account", style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.SemiBold)
        Text(
            "This creates a separate on-device vault protected by its own master password.",
            style = MaterialTheme.typography.bodySmall,
            color = MaterialTheme.colorScheme.onSurfaceVariant,
            textAlign = androidx.compose.ui.text.style.TextAlign.Center,
        )
        OutlinedTextField(
            value = name, onValueChange = onName, label = { Text("Account name") },
            singleLine = true, shape = RoundedCornerShape(14.dp), modifier = Modifier.fillMaxWidth(),
        )
        OutlinedTextField(
            value = password, onValueChange = onPassword, label = { Text("Master password") },
            leadingIcon = { Icon(Icons.Filled.Lock, null, tint = VgAccent) },
            singleLine = true, visualTransformation = PasswordVisualTransformation(),
            textStyle = MaterialTheme.typography.titleLarge,
            shape = RoundedCornerShape(16.dp), modifier = Modifier.fillMaxWidth().heightIn(min = 68.dp),
        )
        error?.let { Text(it, color = MaterialTheme.colorScheme.error, textAlign = androidx.compose.ui.text.style.TextAlign.Center) }
        Button(
            onClick = onCreate, enabled = !loading, shape = RoundedCornerShape(14.dp),
            modifier = Modifier.fillMaxWidth().height(52.dp),
        ) {
            if (loading) CircularProgressIndicator(Modifier.size(20.dp), strokeWidth = 2.dp)
            else Text("Create account")
        }
        TextButton(onClick = onCancel) { Text("Cancel") }
    }
}
