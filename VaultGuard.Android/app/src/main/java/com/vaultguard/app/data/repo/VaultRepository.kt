package com.vaultguard.app.data.repo

import com.vaultguard.app.config.ConfigStore
import com.vaultguard.app.config.ConnectionMode
import com.vaultguard.app.config.SecureStore
import com.vaultguard.app.data.local.VaultDao
import com.vaultguard.app.data.local.VaultItemEntity
import com.vaultguard.app.data.model.CategoryDto
import com.vaultguard.app.data.model.CreateEncryptedPasswordItem
import com.vaultguard.app.data.model.CreateLoginItem
import com.vaultguard.app.data.model.ItemType
import com.vaultguard.app.data.model.TagDto
import com.vaultguard.app.data.model.VaultDto
import com.vaultguard.app.data.model.UpdateLoginItem
import com.vaultguard.app.data.model.UpdatePasswordItem
import com.vaultguard.app.data.model.VaultItem
import com.vaultguard.app.data.remote.ApiProvider
import com.vaultguard.app.domain.VaultCrypto
import kotlinx.coroutines.flow.first
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
    private val crypto: VaultCrypto,
    private val session: SessionManager,
    private val secureStore: SecureStore,
    private val configStore: ConfigStore,
) {
    private suspend fun mode(): ConnectionMode = configStore.config.first().mode

    private fun requireMaster(): String =
        session.masterPassword ?: error("Vault is locked.")

    private fun localKey(): ByteArray {
        val saltB64 = secureStore.localVaultSalt ?: error("Local vault is not initialized.")
        return crypto.deriveKey(requireMaster(), crypto.fromBase64(saltB64))
    }

    // ---- Listing -----------------------------------------------------------

    /** Returns every item (including archived/deleted); the UI filters per selected sidebar section. */
    suspend fun list(): List<VaultItem> = when (mode()) {
        ConnectionMode.API -> apiProvider.api().getItems().map { VaultItem.from(it) }
        ConnectionMode.LOCAL -> dao.getAll().map { it.toVaultItem() }
    }

    suspend fun categories(): List<CategoryDto> = when (mode()) {
        ConnectionMode.API -> runCatching { apiProvider.api().getCategories() }.getOrDefault(emptyList())
            .ifEmpty { DefaultData.categories }
        // Local mode always shows the same six default categories as WPF, plus any custom ones on items.
        ConnectionMode.LOCAL -> {
            val custom = dao.getAll().mapNotNull { it.categoryName }.distinct()
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
        ConnectionMode.LOCAL -> listOf(
            DefaultData.personalVault.copy(itemCount = dao.getAll().count { !it.isDeleted }),
        )
    }

    suspend fun search(term: String): List<VaultItem> = when (mode()) {
        ConnectionMode.API -> apiProvider.api().search(term).map { VaultItem.from(it) }
        ConnectionMode.LOCAL -> dao.search(term).map { it.toVaultItem() }
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

    // ---- Mutations ---------------------------------------------------------

    suspend fun create(input: LoginItemInput) {
        when (mode()) {
            ConnectionMode.API -> apiProvider.api().createEncrypted(
                CreateEncryptedPasswordItem(
                    title = input.title,
                    description = input.description,
                    type = input.type.code,
                    isFavorite = input.isFavorite,
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
                        categoryName = null,
                        tagsCsv = null,
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
        if (dao.getAll().isNotEmpty()) return
        seedLocalDemo()
    }

    /** Seed the local vault with demo items in the default WPF categories. Callable from Settings. */
    suspend fun seedLocalDemo() {
        if (mode() != ConnectionMode.LOCAL || session.masterPassword == null) return
        val key = localKey()
        val now = System.currentTimeMillis()
        DEMO_ITEMS.forEach { d ->
            dao.insert(
                VaultItemEntity(
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

    /** Remove all locally-stored items (Settings → Maintenance). No effect in API mode. */
    suspend fun resetLocal() {
        if (mode() != ConnectionMode.LOCAL) return
        dao.getAll().forEach { dao.delete(it) }
    }

    private data class DemoItem(
        val title: String, val type: ItemType, val username: String?, val email: String?,
        val website: String?, val loginUrl: String?, val password: String?, val totp: String?,
        val notes: String?, val category: String,
    )

    private companion object {
        val DEMO_ITEMS = listOf(
            DemoItem("GitHub", ItemType.Login, "octocat", "octocat@example.com", "github.com", "https://github.com/login", "gh_Demo!2024pass", "JBSWY3DPEHPK3PXP", "Personal open-source account.", "Logins"),
            DemoItem("Google", ItemType.Login, "jane.doe", "jane.doe@gmail.com", "google.com", "https://accounts.google.com", "G00gle!Demo#77", null, null, "Logins"),
            DemoItem("Work Email", ItemType.Login, "j.doe", "j.doe@company.com", "outlook.office.com", null, "W0rkMail\$Demo9", null, "Corporate mailbox.", "Logins"),
            DemoItem("Visa •• 4242", ItemType.CreditCard, "Jane Doe", null, null, null, "4242 4242 4242 4242", null, "Exp 04/28 · CVV 123", "Credit Cards"),
            DemoItem("Home Wi-Fi", ItemType.WiFi, null, null, null, null, "MyHomeNetwork#2024", null, "SSID: HomeNet-5G", "WiFi Networks"),
            DemoItem("Recovery Codes", ItemType.SecureNote, null, null, null, null, null, null, "Store your backup/recovery codes here.", "Secure Notes"),
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
    )
}
