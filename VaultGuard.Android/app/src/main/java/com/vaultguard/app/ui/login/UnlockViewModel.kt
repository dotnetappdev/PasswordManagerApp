package com.vaultguard.app.ui.login

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.vaultguard.app.config.Account
import com.vaultguard.app.config.AccountsStore
import com.vaultguard.app.config.ConfigStore
import com.vaultguard.app.config.ConnectionMode
import com.vaultguard.app.config.LocalProfile
import com.vaultguard.app.config.LocalProfilesStore
import com.vaultguard.app.config.SecureStore
import com.vaultguard.app.config.SettingsStore
import com.vaultguard.app.data.repo.AuthRepository
import com.vaultguard.app.data.repo.LoginResult
import com.vaultguard.app.data.repo.SessionManager
import com.vaultguard.app.data.repo.VaultRepository
import com.vaultguard.app.domain.Totp
import com.vaultguard.app.domain.VaultCrypto
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.combine
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
    val awaitingBiometric: Boolean = false,
    val needsAppTotp: Boolean = false,
    val appTotpCode: String = "",
    val accounts: List<Account> = emptyList(),
    val currentAccountId: String? = null,
    /** Set after a failed LOCAL unlock so the UI can offer to reset a forgotten master password. */
    val canResetLocal: Boolean = false,
    val info: String? = null,
    // Quick unlock (fingerprint / passcode)
    val hasQuickUnlock: Boolean = false,
    val hasPasscode: Boolean = false,
    /** After a successful unlock with no quick-unlock configured: offer to set one up. */
    val offerQuickSetup: Boolean = false,
    /** Show the numeric passcode pad — either to unlock (settingPasscode=false) or to set one (true). */
    val showPasscode: Boolean = false,
    val settingPasscode: Boolean = false,
    /** Second phase of setting a passcode: the user re-enters the digits to confirm they match. */
    val confirmingPasscode: Boolean = false,
    /** The first passcode entry, held only in-memory until the confirmation entry is matched. */
    val firstPasscodeEntry: String = "",
    val passcodeInput: String = "",
    /** LOCAL mode: false on first run (typing a password CREATES the vault, it doesn't validate). */
    val localVaultExists: Boolean = true,
    // Local profiles (accounts) — the WPF-style account picker for LOCAL mode.
    val localProfiles: List<LocalProfile> = emptyList(),
    val selectedProfileId: String = "default",
    val showCreateAccount: Boolean = false,
    val newAccountName: String = "",
)

@HiltViewModel
class UnlockViewModel @Inject constructor(
    private val authRepository: AuthRepository,
    private val vaultRepository: VaultRepository,
    private val configStore: ConfigStore,
    private val settingsStore: SettingsStore,
    private val accountsStore: AccountsStore,
    private val secureStore: SecureStore,
    private val session: SessionManager,
    private val crypto: VaultCrypto,
    private val localProfilesStore: LocalProfilesStore,
) : ViewModel() {

    private val _state = MutableStateFlow(UnlockState())
    val state = _state.asStateFlow()

    /** In-memory: user tapped "Not now" on the quick-unlock offer this session. */
    private var quickSetupDismissed = false

    init {
        viewModelScope.launch {
            _state.update {
                it.copy(
                    mode = configStore.config.first().mode,
                    hasQuickUnlock = secureStore.hasQuickUnlock,
                    hasPasscode = secureStore.hasPasscode,
                )
            }
        }
        viewModelScope.launch {
            combine(accountsStore.accounts, accountsStore.currentId) { accts, current -> accts to current }
                .collect { (accts, current) -> _state.update { it.copy(accounts = accts, currentAccountId = current) } }
        }
        // Seed the demo local profiles (admin/parent/user/child) and observe them for the LOCAL picker.
        viewModelScope.launch {
            localProfilesStore.seedDefaultsIfEmpty()
            combine(localProfilesStore.profiles, localProfilesStore.activeId) { list, active -> list to active }
                .collect { (list, active) ->
                    val selected = active ?: _state.value.selectedProfileId.takeIf { id -> list.any { it.id == id } }
                        ?: list.firstOrNull()?.id ?: "default"
                    _state.update {
                        it.copy(
                            localProfiles = list,
                            selectedProfileId = selected,
                            localVaultExists = secureStore.localVerifier(selected) != null,
                        )
                    }
                }
        }
    }

    /** Pick a LOCAL profile (account) to unlock. */
    fun selectProfile(id: String) {
        _state.update {
            it.copy(
                selectedProfileId = id,
                localVaultExists = secureStore.localVerifier(id) != null,
                password = "",
                error = null,
                info = null,
                canResetLocal = false,
            )
        }
    }

    fun showCreateAccount() = _state.update { it.copy(showCreateAccount = true, newAccountName = "", password = "", error = null) }
    fun cancelCreateAccount() = _state.update { it.copy(showCreateAccount = false, newAccountName = "", password = "") }
    fun setNewAccountName(v: String) = _state.update { it.copy(newAccountName = v, error = null) }

    /** Create a new LOCAL account (profile) with the given name + master password, and unlock it. */
    fun createAccount() {
        val s = _state.value
        if (s.newAccountName.isBlank()) { _state.update { it.copy(error = "Enter a name for the account.") }; return }
        if (s.password.length < 4) { _state.update { it.copy(error = "Choose a master password (min 4 chars).") }; return }
        viewModelScope.launch {
            val id = "profile-${System.currentTimeMillis()}"
            localProfilesStore.upsert(LocalProfile(id = id, name = s.newAccountName.trim(), role = "User"))
            _state.update { it.copy(loading = true, error = null, selectedProfileId = id, showCreateAccount = false) }
            when (authRepository.unlockLocal(s.password, id)) {
                is LoginResult.Success -> proceedAfterPassword()
                else -> _state.update { it.copy(loading = false, error = "Couldn't create the account.") }
            }
        }
    }

    /** Switch the active account: apply its connection config and prefill its email. */
    fun selectAccount(id: String) {
        viewModelScope.launch {
            accountsStore.select(id)
            val account = accountsStore.list().firstOrNull { it.id == id }
            _state.update {
                it.copy(
                    mode = account?.connectionMode ?: it.mode,
                    email = account?.email ?: "",
                    currentAccountId = id,
                    needsTwoFactor = false,
                    error = null,
                )
            }
        }
    }

    fun setEmail(v: String) = _state.update { it.copy(email = v, error = null, info = null) }
    fun setPassword(v: String) = _state.update { it.copy(password = v, error = null, info = null, canResetLocal = false) }
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
                ConnectionMode.LOCAL -> authRepository.unlockLocal(s.password, s.selectedProfileId)
            }
            when (result) {
                is LoginResult.Success -> {
                    if (s.mode == ConnectionMode.LOCAL) localProfilesStore.select(s.selectedProfileId)
                    if (!secureStore.appTotpSecret.isNullOrBlank())
                        _state.update { it.copy(loading = false, needsAppTotp = true) }
                    else proceedAfterPassword()
                }
                is LoginResult.NeedsTwoFactor ->
                    _state.update { it.copy(loading = false, needsTwoFactor = true, error = "Enter your 2FA code.") }
                is LoginResult.Error -> _state.update {
                    it.copy(
                        loading = false,
                        error = result.message,
                        // Offer a recovery path only for a genuinely locked local vault.
                        canResetLocal = s.mode == ConnectionMode.LOCAL,
                    )
                }
            }
        }
    }

    /**
     * Recover a LOCAL vault whose master password was forgotten (or set to something else during earlier
     * testing): wipe the verifier/salt and any locally-stored items so the next unlock re-establishes the
     * vault with a fresh master password. Destructive by nature — the old local items can't be decrypted
     * without the original password.
     */
    fun resetLocalVault() {
        viewModelScope.launch {
            vaultRepository.forgetLocalVault()
            _state.update {
                it.copy(
                    password = "",
                    error = null,
                    canResetLocal = false,
                    info = "Local vault reset. Enter a master password to set it up again.",
                )
            }
        }
    }

    fun setAppTotpCode(v: String) = _state.update { it.copy(appTotpCode = v, error = null) }

    /** Verify the app authenticator (TOTP) second factor. */
    fun submitAppTotp() {
        val secret = secureStore.appTotpSecret ?: return
        if (Totp.verify(secret, _state.value.appTotpCode)) {
            _state.update { it.copy(needsAppTotp = false) }
            proceedAfterPassword()
        } else {
            _state.update { it.copy(error = "Invalid authenticator code.") }
        }
    }

    /** After the master password (and app TOTP), require biometrics if enabled, else finish. */
    private fun proceedAfterPassword() {
        viewModelScope.launch {
            val requireBiometric = settingsStore.settings.first().biometricUnlock
            if (requireBiometric) _state.update { it.copy(loading = false, awaitingBiometric = true) }
            else finishUnlock()
        }
    }

    /** Result of the biometric second factor after a master-password unlock. */
    fun biometricResult(ok: Boolean) {
        if (ok) {
            _state.update { it.copy(awaitingBiometric = false) }
            finishUnlock()
        } else {
            session.lock()
            _state.update { it.copy(awaitingBiometric = false, error = "Biometric verification was cancelled.") }
        }
    }

    /**
     * Complete a master-password unlock. In LOCAL mode, if quick unlock isn't set up yet, offer to enable
     * fingerprint/passcode so the master key doesn't have to be retyped; otherwise finish.
     */
    private fun finishUnlock() {
        val s = _state.value
        if (s.mode == ConnectionMode.LOCAL && !secureStore.hasQuickUnlock && !quickSetupDismissed) {
            _state.update { it.copy(loading = false, offerQuickSetup = true) }
        } else {
            _state.update { it.copy(loading = false, unlocked = true) }
        }
    }

    // ---- Quick unlock (fingerprint / passcode) -----------------------------

    /** Called by the screen after a successful biometric prompt to unlock with the cached master password. */
    fun biometricQuickUnlock() = completeQuickUnlock()

    private fun completeQuickUnlock() {
        val master = secureStore.quickUnlockMaster
        if (master.isNullOrBlank()) {
            _state.update { it.copy(error = "Quick unlock isn't set up. Enter your master password.") }
            return
        }
        viewModelScope.launch {
            _state.update { it.copy(loading = true, error = null) }
            when (authRepository.unlockLocal(master, _state.value.selectedProfileId)) {
                is LoginResult.Success -> _state.update { it.copy(loading = false, unlocked = true, showPasscode = false) }
                else -> {
                    // The cached master no longer matches (vault was reset?) — clear it and fall back to typing.
                    secureStore.clearQuickUnlock()
                    _state.update {
                        it.copy(loading = false, hasQuickUnlock = false, hasPasscode = false,
                            showPasscode = false, error = "Couldn't unlock. Enter your master password.")
                    }
                }
            }
        }
    }

    /** Enable fingerprint unlock from the offer: cache the just-used master password (Keystore-encrypted). */
    fun enableFingerprintFromOffer() {
        val master = session.masterPassword
        if (!master.isNullOrBlank()) {
            secureStore.quickUnlockMaster = master
            viewModelScope.launch { settingsStore.update { it.copy(biometricUnlock = true) } }
        }
        _state.update { it.copy(offerQuickSetup = false, hasQuickUnlock = true, unlocked = true) }
    }

    /** From the offer: open the pad to choose a numeric passcode. */
    fun startSetPasscode() =
        _state.update {
            it.copy(
                offerQuickSetup = true, settingPasscode = true, confirmingPasscode = false,
                firstPasscodeEntry = "", showPasscode = true, passcodeInput = "", error = null,
            )
        }

    /** Dismiss the quick-unlock offer and just finish unlocking. */
    fun skipQuickSetup() {
        quickSetupDismissed = true
        _state.update { it.copy(offerQuickSetup = false, unlocked = true) }
    }

    /** Show the passcode pad to unlock an existing vault. */
    fun showPasscodeUnlock() =
        _state.update { it.copy(showPasscode = true, settingPasscode = false, passcodeInput = "", error = null) }

    fun cancelPasscode() =
        _state.update {
            it.copy(
                showPasscode = false, settingPasscode = false, confirmingPasscode = false,
                firstPasscodeEntry = "", passcodeInput = "",
            )
        }

    fun passcodeDigit(d: Char) {
        val cur = _state.value.passcodeInput
        if (cur.length >= PASSCODE_LEN) return
        val next = cur + d
        _state.update { it.copy(passcodeInput = next, error = null) }
        if (next.length != PASSCODE_LEN) return

        val s = _state.value
        when {
            // Unlocking with an existing passcode.
            !s.settingPasscode -> verifyPasscodeAndUnlock(next)
            // First entry while setting a new passcode → move to the confirm phase.
            !s.confirmingPasscode -> _state.update {
                it.copy(firstPasscodeEntry = next, confirmingPasscode = true, passcodeInput = "", error = null)
            }
            // Confirm phase: the two entries must match before we save.
            next == s.firstPasscodeEntry -> savePasscode(next)
            else -> _state.update {
                it.copy(
                    passcodeInput = "", firstPasscodeEntry = "", confirmingPasscode = false,
                    error = "Those passcodes didn't match. Start again.",
                )
            }
        }
    }

    fun passcodeBackspace() = _state.update { it.copy(passcodeInput = it.passcodeInput.dropLast(1)) }

    private fun savePasscode(pin: String) {
        val master = session.masterPassword
        if (master.isNullOrBlank()) {
            _state.update { it.copy(error = "Unlock first, then set a passcode.", showPasscode = false, settingPasscode = false) }
            return
        }
        val salt = crypto.newSalt()
        val hash = crypto.toBase64(crypto.deriveKey(pin, salt))
        secureStore.passcode = "${crypto.toBase64(salt)}:$hash"
        secureStore.quickUnlockMaster = master
        _state.update {
            it.copy(showPasscode = false, settingPasscode = false, confirmingPasscode = false,
                firstPasscodeEntry = "", passcodeInput = "",
                hasPasscode = true, hasQuickUnlock = true, offerQuickSetup = false, unlocked = true)
        }
    }

    private fun verifyPasscodeAndUnlock(pin: String) {
        val parts = secureStore.passcode?.split(':')
        val ok = parts?.size == 2 && runCatching {
            crypto.toBase64(crypto.deriveKey(pin, crypto.fromBase64(parts[0]))) == parts[1]
        }.getOrDefault(false)
        if (ok) completeQuickUnlock()
        else _state.update { it.copy(passcodeInput = "", error = "Incorrect passcode.") }
    }

    private companion object { const val PASSCODE_LEN = 6 }
}
