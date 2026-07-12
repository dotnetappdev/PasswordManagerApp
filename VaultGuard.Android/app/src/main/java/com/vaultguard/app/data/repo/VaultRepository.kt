package com.vaultguard.app.data.repo

import com.vaultguard.app.config.ConfigStore
import com.vaultguard.app.config.ConnectionMode
import com.vaultguard.app.config.SecureStore
import com.vaultguard.app.data.local.VaultDao
import com.vaultguard.app.data.local.VaultItemEntity
import com.vaultguard.app.data.model.CategoryDto
import com.vaultguard.app.data.model.CustomFieldData
import com.vaultguard.app.data.model.CreateEncryptedPasswordItem
import com.vaultguard.app.data.model.CreateLoginItem
import com.vaultguard.app.data.model.CreateVaultDto
import com.vaultguard.app.data.model.ItemType
import com.vaultguard.app.data.model.QrAuthenticateRequest
import com.vaultguard.app.data.model.RegisterDeviceRequest
import com.vaultguard.app.data.model.UpdateVaultDto
import com.vaultguard.app.data.model.TagDto
import com.vaultguard.app.data.model.VaultDto
import com.vaultguard.app.data.model.UpdateLoginItem
import com.vaultguard.app.data.model.UpdatePasswordItem
import com.vaultguard.app.data.model.VaultItem
import com.vaultguard.app.data.remote.ApiProvider
import com.vaultguard.app.domain.VaultCrypto
import kotlinx.coroutines.flow.first
import kotlinx.serialization.decodeFromString
import kotlinx.serialization.encodeToString
import kotlinx.serialization.json.Json
import javax.inject.Inject
import javax.inject.Singleton

/**
 * Single entry point the UI uses for vault data. It delegates to the REST API or the local SQLite (Room)
 * store depending on the current [ConnectionMode], so ViewModels never need to know which backend is active.
 */
@Singleton
class VaultRepository @Inject constructor(
    private val apiProvider: ApiProvider,
    private val dao: VaultDao,
    private val vaultsDao: com.vaultguard.app.data.local.VaultsDao,
    private val crypto: VaultCrypto,
    private val session: SessionManager,
    private val secureStore: SecureStore,
    private val configStore: ConfigStore,
) {
    private val json = Json { ignoreUnknownKeys = true }

    /** Serialize custom fields for the local JSON column (null when empty). */
    private fun encodeCustomFields(fields: List<CustomFieldData>): String? =
        fields.filter { it.name.isNotBlank() || it.value.isNotBlank() }
            .takeIf { it.isNotEmpty() }
            ?.let { json.encodeToString(it) }

    private fun decodeCustomFields(raw: String?): List<CustomFieldData> =
        raw?.takeIf { it.isNotBlank() }
            ?.let { runCatching { json.decodeFromString<List<CustomFieldData>>(it) }.getOrNull() }
            ?: emptyList()

    private suspend fun mode(): ConnectionMode = configStore.config.first().mode

    private fun requireMaster(): String =
        session.masterPassword ?: error("Vault is locked.")

    /** The active LOCAL profile (account) whose vault is in use. */
    private fun pid(): String = session.activeProfileId

    private fun localKey(): ByteArray {
        val saltB64 = secureStore.localSalt(pid()) ?: error("Local vault is not initialized.")
        return crypto.deriveKey(requireMaster(), crypto.fromBase64(saltB64))
    }

    // ---- Listing -----------------------------------------------------------

    /** Returns every item (including archived/deleted); the UI filters per selected sidebar section. */
    suspend fun list(): List<VaultItem> = when (mode()) {
        ConnectionMode.API -> apiProvider.api().getItems().map { VaultItem.from(it) }
        ConnectionMode.LOCAL -> dao.getAll(pid()).map { it.toVaultItem() }
    }

    suspend fun categories(): List<CategoryDto> = when (mode()) {
        ConnectionMode.API -> runCatching { apiProvider.api().getCategories() }.getOrDefault(emptyList())
            .ifEmpty { DefaultData.categories }
        // Local mode always shows the same default categories as WPF, plus any custom ones on items.
        ConnectionMode.LOCAL -> {
            val custom = dao.getAll(pid()).mapNotNull { it.categoryName }.distinct()
                .filter { name -> DefaultData.categories.none { it.name == name } }
                .mapIndexed { i, name -> CategoryDto(id = 100 + i, name = name) }
            DefaultData.categories + custom
        }
    }

    suspend fun tags(): List<TagDto> = when (mode()) {
        ConnectionMode.API -> runCatching { apiProvider.api().getTags() }.getOrDefault(emptyList())
        ConnectionMode.LOCAL -> emptyList()
    }

    suspend fun vaults(): List<VaultDto> = when (mode()) {
        ConnectionMode.API -> runCatching { apiProvider.api().getVaults() }.getOrDefault(emptyList())
            .ifEmpty { listOf(DefaultData.personalVault) }
        ConnectionMode.LOCAL -> {
            ensureLocalDefaultVault()
            val itemsByVault = dao.getAll(pid()).filter { !it.isDeleted }.groupingBy { it.vaultId }.eachCount()
            vaultsDao.getAll().map { v ->
                VaultDto(
                    id = v.id, name = v.name, description = v.description, color = v.color,
                    icon = v.icon, isDefault = v.isDefault, itemCount = itemsByVault[v.id] ?: 0,
                )
            }
        }
    }

    /** Ensure the local vault list has the default "Personal" vault (matches the WPF seed). */
    private suspend fun ensureLocalDefaultVault() {
        if (vaultsDao.count() == 0) {
            vaultsDao.insert(
                com.vaultguard.app.data.local.VaultEntity(
                    name = DefaultData.DEFAULT_VAULT_NAME, description = "Your personal password vault",
                    color = null, icon = "🔐", isDefault = true,
                )
            )
        }
    }

    suspend fun search(term: String): List<VaultItem> = when (mode()) {
        ConnectionMode.API -> apiProvider.api().search(term).map { VaultItem.from(it) }
        ConnectionMode.LOCAL -> dao.search(pid(), term).map { it.toVaultItem() }
    }

    suspend fun get(id: Int): VaultItem? = when (mode()) {
        ConnectionMode.API -> runCatching { VaultItem.from(apiProvider.api().getItem(id)) }.getOrNull()
        ConnectionMode.LOCAL -> dao.getById(id)?.toVaultItem()
    }

    // ---- Secrets (decrypt on demand) --------------------------------------

    suspend fun secret(id: Int): ItemSecret = when (mode()) {
        ConnectionMode.API -> {
            val d = apiProvider.api().decrypt(id).loginItem
            ItemSecret(password = d?.password, totpSecret = d?.totpSecret)
        }
        ConnectionMode.LOCAL -> {
            val e = dao.getById(id) ?: return ItemSecret()
            val key = localKey()
            ItemSecret(
                password = e.encPassword?.let { crypto.decrypt(it, key) },
                totpSecret = e.encTotp?.let { crypto.decrypt(it, key) },
            )
        }
    }

    // ---- QR login handoff --------------------------------------------------

    /**
     * Approve a desktop/web QR login by scanning its code on the phone. Uses the credentials this device
     * signed in with, so the desktop is signed into the same account. API mode only.
     */
    suspend fun signInComputer(scanned: String): Result<String> {
        if (mode() != ConnectionMode.API) return Result.failure(Exception("QR sign-in requires API connection mode."))
        val email = session.user.value?.email
        val password = session.masterPassword
        if (email.isNullOrBlank() || password.isNullOrBlank())
            return Result.failure(Exception("Sign in on this device first."))
        val token = extractQrToken(scanned)
        return runCatching {
            val resp = apiProvider.api().qrAuthenticate(
                QrAuthenticateRequest(
                    token = token, email = email, password = password,
                    deviceName = android.os.Build.MODEL, deviceType = "Mobile", platform = "Android",
                )
            )
            if (resp.success) "Signed in ${resp.deviceName ?: "computer"}."
            else throw Exception(resp.message.ifBlank { "QR sign-in failed." })
        }
    }

    private fun extractQrToken(scanned: String): String {
        // Accept a raw token, a URL with ?token=, or a vaultguard://login?token= deep link.
        val marker = "token="
        return if (scanned.contains(marker)) scanned.substringAfter(marker).substringBefore('&').trim()
        else scanned.trim()
    }

    // ---- Passkeys (API mode) -----------------------------------------------

    suspend fun passkeys(): List<com.vaultguard.app.data.model.PasskeyDto> {
        if (mode() != ConnectionMode.API) return emptyList()
        return runCatching { apiProvider.api().getPasskeys().passkeys }.getOrDefault(emptyList())
    }

    suspend fun passkeyStatus(): com.vaultguard.app.data.model.PasskeyStatus? {
        if (mode() != ConnectionMode.API) return null
        return runCatching { apiProvider.api().passkeyStatus() }.getOrNull()
    }

    suspend fun passkeyRegisterStart(name: String): Result<com.vaultguard.app.data.model.PasskeyRegistrationStartResponse> {
        val master = session.masterPassword ?: return Result.failure(Exception("Vault is locked."))
        return runCatching {
            apiProvider.api().passkeyRegisterStart(
                com.vaultguard.app.data.model.PasskeyRegistrationStartRequest(master, name, storeInVault = true)
            )
        }
    }

    suspend fun passkeyRegisterComplete(
        challenge: String, optionsJson: String, responseJson: String, name: String,
    ): Result<Unit> = runCatching {
        apiProvider.api().passkeyRegisterComplete(
            com.vaultguard.app.data.model.PasskeyRegistrationComplete(
                challenge = challenge, credentialResponse = responseJson,
                originalOptionsJson = optionsJson, passkeyName = name, storeInVault = true,
                deviceType = android.os.Build.MODEL,
            )
        )
    }

    suspend fun deletePasskey(id: Int): Result<Unit> {
        val master = session.masterPassword ?: return Result.failure(Exception("Vault is locked."))
        return runCatching {
            apiProvider.api().deletePasskey(id, com.vaultguard.app.data.model.PasskeyDeleteRequest(master))
        }
    }

    // ---- Number-matching approvals (API mode) ------------------------------

    suspend fun pendingApprovals(): List<com.vaultguard.app.data.model.PendingApprovalDto> {
        if (mode() != ConnectionMode.API) return emptyList()
        return runCatching { apiProvider.api().pendingApprovals() }.getOrDefault(emptyList())
    }

    suspend fun respondApproval(id: String, selectedNumber: Int, approve: Boolean): Result<String> =
        runCatching {
            apiProvider.api().respondApproval(
                id, com.vaultguard.app.data.model.RespondApprovalRequest(selectedNumber, approve)
            ).state
        }

    /** Register an FCM push token with the server (API mode). Call from FirebaseMessagingService.onNewToken. */
    suspend fun registerPushToken(token: String): Result<Unit> {
        if (mode() != ConnectionMode.API) return Result.success(Unit)
        return runCatching { apiProvider.api().registerPush(RegisterDeviceRequest(token, "Android")) }
    }

    // ---- Vault CRUD (works in both API and local SQLite modes) -------------

    suspend fun createVault(name: String, description: String?): Result<Unit> = runCatching {
        when (mode()) {
            ConnectionMode.API -> apiProvider.api().createVault(CreateVaultDto(name = name, description = description))
            ConnectionMode.LOCAL -> {
                ensureLocalDefaultVault()
                vaultsDao.insert(
                    com.vaultguard.app.data.local.VaultEntity(name = name, description = description, color = null, icon = "🔐")
                )
            }
        }
    }

    suspend fun updateVault(id: Int, name: String, description: String?): Result<Unit> = runCatching {
        when (mode()) {
            ConnectionMode.API -> apiProvider.api().updateVault(id, UpdateVaultDto(name = name, description = description))
            ConnectionMode.LOCAL -> {
                val v = vaultsDao.getById(id) ?: error("Vault not found.")
                vaultsDao.update(v.copy(name = name, description = description))
            }
        }
    }

    suspend fun deleteVault(id: Int): Result<Unit> = runCatching {
        when (mode()) {
            ConnectionMode.API -> apiProvider.api().deleteVault(id)
            ConnectionMode.LOCAL -> {
                val v = vaultsDao.getById(id) ?: error("Vault not found.")
                if (v.isDefault) error("The default vault can't be deleted.")
                // Move any items in this vault back to the default vault, then delete it.
                val defaultId = vaultsDao.defaultVaultId() ?: DefaultData.DEFAULT_VAULT_ID
                dao.reassignVault(id, defaultId)
                vaultsDao.delete(v)
            }
        }
    }

    // ---- Mutations ---------------------------------------------------------

    suspend fun create(input: LoginItemInput) {
        when (mode()) {
            ConnectionMode.API -> apiProvider.api().createEncrypted(
                CreateEncryptedPasswordItem(
                    title = input.title,
                    description = input.description,
                    type = input.type.code,
                    isFavorite = input.isFavorite,
                    categoryId = input.categoryId,
                    masterPassword = requireMaster(),
                    loginItem = CreateLoginItem(
                        website = input.website,
                        username = input.username,
                        email = input.email,
                        password = input.password,
                        totpSecret = input.totpSecret,
                        loginUrl = input.loginUrl,
                        notes = input.notes,
                    ),
                )
            )
            ConnectionMode.LOCAL -> {
                val key = localKey()
                val now = System.currentTimeMillis()
                dao.insert(
                    VaultItemEntity(
                        profileId = pid(),
                        title = input.title,
                        description = input.description,
                        type = input.type.code,
                        isFavorite = input.isFavorite,
                        username = input.username,
                        email = input.email,
                        website = input.website,
                        loginUrl = input.loginUrl,
                        notes = input.notes,
                        encPassword = input.password?.takeIf { it.isNotEmpty() }?.let { crypto.encrypt(it, key) },
                        encTotp = input.totpSecret?.takeIf { it.isNotEmpty() }?.let { crypto.encrypt(it, key) },
                        categoryName = input.categoryName,
                        tagsCsv = null,
                        customFieldsJson = encodeCustomFields(input.customFields),
                        createdAt = now,
                        lastModified = now,
                    )
                )
            }
        }
    }

    suspend fun update(id: Int, input: LoginItemInput) {
        when (mode()) {
            ConnectionMode.API -> apiProvider.api().update(
                id,
                UpdatePasswordItem(
                    title = input.title,
                    description = input.description,
                    isFavorite = input.isFavorite,
                    categoryId = input.categoryId,
                    loginItem = UpdateLoginItem(
                        website = input.website,
                        username = input.username,
                        email = input.email,
                        password = input.password?.takeIf { it.isNotEmpty() },
                        totpSecret = input.totpSecret,
                        loginUrl = input.loginUrl,
                        notes = input.notes,
                    ),
                )
            )
            ConnectionMode.LOCAL -> {
                val existing = dao.getById(id) ?: return
                val key = localKey()
                dao.update(
                    existing.copy(
                        title = input.title,
                        description = input.description,
                        isFavorite = input.isFavorite,
                        username = input.username,
                        email = input.email,
                        website = input.website,
                        loginUrl = input.loginUrl,
                        notes = input.notes,
                        encPassword = input.password?.takeIf { it.isNotEmpty() }
                            ?.let { crypto.encrypt(it, key) } ?: existing.encPassword,
                        encTotp = input.totpSecret?.takeIf { it.isNotEmpty() }
                            ?.let { crypto.encrypt(it, key) } ?: existing.encTotp,
                        categoryName = input.categoryName ?: existing.categoryName,
                        customFieldsJson = encodeCustomFields(input.customFields),
                        lastModified = System.currentTimeMillis(),
                    )
                )
            }
        }
    }

    suspend fun delete(id: Int) {
        when (mode()) {
            ConnectionMode.API -> apiProvider.api().delete(id)
            ConnectionMode.LOCAL -> dao.deleteById(id)
        }
    }

    suspend fun toggleFavorite(id: Int) {
        when (mode()) {
            ConnectionMode.API -> apiProvider.api().toggleFavorite(id)
            ConnectionMode.LOCAL -> {
                val e = dao.getById(id) ?: return
                dao.update(e.copy(isFavorite = !e.isFavorite))
            }
        }
    }

    /**
     * Seed a few demo items into the local SQLite vault on first use, mirroring the desktop/web
     * TestDataSeeder so a fresh local install isn't empty. No-op in API mode or if items already exist.
     */
    suspend fun seedLocalDemoIfEmpty() {
        if (mode() != ConnectionMode.LOCAL) return
        if (dao.getAll(pid()).isNotEmpty()) return
        seedLocalDemo()
    }

    /** Seed the active profile's local vault with demo items in the default WPF categories. */
    suspend fun seedLocalDemo() {
        if (mode() != ConnectionMode.LOCAL || session.masterPassword == null) return
        val key = localKey()
        val now = System.currentTimeMillis()
        DEMO_ITEMS.forEach { d ->
            dao.insert(
                VaultItemEntity(
                    profileId = pid(),
                    title = d.title,
                    description = d.notes,
                    type = d.type.code,
                    isFavorite = d.title == "GitHub",
                    username = d.username,
                    email = d.email,
                    website = d.website,
                    loginUrl = d.loginUrl,
                    notes = d.notes,
                    encPassword = d.password?.let { crypto.encrypt(it, key) },
                    encTotp = d.totp?.let { crypto.encrypt(it, key) },
                    categoryName = d.category,
                    tagsCsv = null,
                    createdAt = now,
                    lastModified = now,
                )
            )
        }
    }

    /** Remove the active profile's locally-stored items (Settings → Maintenance). No effect in API mode. */
    suspend fun resetLocal() {
        if (mode() != ConnectionMode.LOCAL) return
        dao.deleteAllForProfile(pid())
    }

    /**
     * Full reset of the active profile's local vault to recover from a forgotten/mismatched master
     * password: clears that profile's verifier + salt AND its items (undecryptable once the key is gone),
     * then locks the session so the next unlock re-establishes the vault with a fresh master password.
     */
    suspend fun forgetLocalVault() {
        val profileId = session.activeProfileId
        dao.deleteAllForProfile(profileId)
        secureStore.clearLocalProfile(profileId)
        session.lock()
    }

    private data class DemoItem(
        val title: String, val type: ItemType, val username: String?, val email: String?,
        val website: String?, val loginUrl: String?, val password: String?, val totp: String?,
        val notes: String?, val category: String,
    )

    private companion object {
        // A representative slice of the desktop/web TestDataSeeder demo vault (same titles, usernames and
        // passwords) so a fresh local install looks like the WPF sample data instead of an empty vault.
        val DEMO_ITEMS = listOf(
            // Banking & finance
            DemoItem("Chase Bank Online", ItemType.Login, "john.doe@email.com", "john.doe@email.com", "chase.com", "https://chase.com", "Tr0ub4d0r&3!", null, "Primary checking & savings.", "Logins"),
            DemoItem("Bank of America", ItemType.Login, "john.doe@email.com", "john.doe@email.com", "bankofamerica.com", "https://bankofamerica.com", "M0nk3yBr@in$!", null, null, "Logins"),
            DemoItem("PayPal", ItemType.Login, "john.doe@gmail.com", "john.doe@gmail.com", "paypal.com", "https://paypal.com", "P@yP4l\$ecure!", "JBSWY3DPEHPK3PXP", null, "Logins"),
            DemoItem("Coinbase", ItemType.Login, "john.doe@email.com", "john.doe@email.com", "coinbase.com", "https://coinbase.com", "C01nb@se#Crypto!", null, "Crypto exchange.", "Logins"),
            // Email
            DemoItem("Personal Gmail", ItemType.Login, "john.doe@gmail.com", "john.doe@gmail.com", "gmail.com", "https://gmail.com", "Gm@1l#S3cure!", "JBSWY3DPEHPK3PXP", null, "Emails"),
            DemoItem("Work Email (Outlook)", ItemType.Login, "john.doe@company.com", "john.doe@company.com", "outlook.com", "https://outlook.com", "0utl00k#W0rk!", null, "Corporate mailbox.", "Emails"),
            DemoItem("iCloud / Apple ID", ItemType.Login, "john.doe@icloud.com", "john.doe@icloud.com", "appleid.apple.com", "https://appleid.apple.com", "Appl3#1Cl0ud!", null, null, "Logins"),
            // Work / dev
            DemoItem("GitHub", ItemType.Login, "johndoe-dev", null, "github.com", "https://github.com", "G1tHub#D3v2024!", "JBSWY3DPEHPK3PXP", "Open-source & work repos.", "Logins"),
            DemoItem("AWS Console", ItemType.Login, "john.doe@company.com", "john.doe@company.com", "aws.amazon.com", "https://aws.amazon.com", "AWS#Cl0ud2024!", null, "Root account — use with care.", "Logins"),
            DemoItem("Slack", ItemType.Login, "john.doe@company.com", "john.doe@company.com", "slack.com", "https://slack.com", "Sl@ck#T3@m!", null, null, "Logins"),
            // Streaming & shopping
            DemoItem("Netflix", ItemType.Login, "john.doe@gmail.com", "john.doe@gmail.com", "netflix.com", "https://netflix.com", "N3tfl1x#Str3@m!", null, null, "Logins"),
            DemoItem("Spotify", ItemType.Login, "john.doe@gmail.com", "john.doe@gmail.com", "spotify.com", "https://spotify.com", "Sp0t1fy#Mus1c!", null, null, "Logins"),
            DemoItem("Amazon", ItemType.Login, "john.doe@gmail.com", "john.doe@gmail.com", "amazon.com", "https://amazon.com", "Am@z0n#Sh0p!", null, null, "Logins"),
            DemoItem("Steam", ItemType.Login, "johndoe_steam", null, "store.steampowered.com", "https://store.steampowered.com", "St3@m#G@mes!", null, null, "Logins"),
            // Credit cards
            DemoItem("Chase Sapphire Preferred", ItemType.CreditCard, "John Doe", null, null, null, "4532 1234 5678 9012", null, "Visa · Exp 12/2027 · CVV 123 · Chase Bank", "Credit Cards"),
            DemoItem("American Express Gold", ItemType.CreditCard, "John Doe", null, null, null, "3714 496353 98431", null, "Amex · Exp 06/2028 · CVV 7890", "Credit Cards"),
            DemoItem("Apple Card", ItemType.CreditCard, "John Doe", null, null, null, "4147 2024 9999 0001", null, "Visa · Exp 07/2027 · CVV 999 · Daily Cash", "Credit Cards"),
            // Wi-Fi
            DemoItem("Home WiFi — 5GHz", ItemType.WiFi, null, null, null, null, "MyS3cur3H0m32024!", null, "SSID: DoeFamily_5G · WPA3 · ASUS AX6000", "WiFi Networks"),
            DemoItem("Office WiFi", ItemType.WiFi, null, null, null, null, "C0rp0r@t3#2024!", null, "SSID: CompanyWiFi_Corp · WPA2 · Cisco Meraki", "WiFi Networks"),
            // Passkeys
            DemoItem("Google Account Passkey", ItemType.Passkey, "john.doe@gmail.com", "john.doe@gmail.com", "accounts.google.com", "https://accounts.google.com", null, null, "Biometric passkey.", "Passkeys"),
            DemoItem("GitHub Passkey", ItemType.Passkey, "johndoe-dev", null, "github.com", "https://github.com", null, null, "Biometric passkey.", "Passkeys"),
            // Passwords / notes
            DemoItem("MacBook Pro Login", ItemType.Password, "johndoe", null, null, null, "M@cB00kPr0#L0g1n!", null, "MacBook Pro 14-inch M3 user account.", "Passwords"),
            DemoItem("Router Admin", ItemType.Password, "admin", null, null, "https://192.168.1.1", "R0ut3r@dm1n#!", null, "ASUS AX6000 admin console.", "Passwords"),
            DemoItem("Emergency Contacts", ItemType.SecureNote, null, null, null, null, null, null, "Spouse: Jane Doe — (555) 123-4567\nDoctor: Dr. Smith — (555) 234-5678\nBlood Type: O+  Allergies: Penicillin", "Secure Notes"),
            DemoItem("API Keys Reference", ItemType.SecureNote, null, null, null, null, null, null, "SendGrid: SG.xxxx\nStripe Test: sk_test_xxxx\nProduction keys in AWS Secrets Manager.", "Secure Notes"),
        )
    }

    private fun VaultItemEntity.toVaultItem() = VaultItem(
        id = id,
        title = title,
        description = description,
        type = ItemType.fromCode(type),
        isFavorite = isFavorite,
        isArchived = isArchived,
        isDeleted = isDeleted,
        username = username,
        email = email,
        website = website,
        loginUrl = loginUrl,
        totpSecret = null, // decrypted on demand via secret()
        notes = notes,
        categoryName = categoryName,
        tags = tagsCsv?.split(',')?.filter { it.isNotBlank() } ?: emptyList(),
        customFields = decodeCustomFields(customFieldsJson),
        vaultId = vaultId,
    )
}
