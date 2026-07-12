package com.vaultguard.app.data.remote

import com.vaultguard.app.data.model.CustomFieldData
import com.vaultguard.app.data.model.ItemType
import com.vaultguard.app.data.repo.LoginItemInput
import com.vaultguard.app.domain.Totp
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import kotlinx.serialization.Serializable
import kotlinx.serialization.decodeFromString
import kotlinx.serialization.json.Json
import okhttp3.OkHttpClient
import okhttp3.Request
import java.io.IOException
import java.util.concurrent.TimeUnit
import javax.inject.Inject
import javax.inject.Singleton

/**
 * Imports items from a **1Password Connect** server (or a Service Account, which speaks the same REST API).
 * This is the only supported *programmatic* path — 1Password has no consumer read API — so the user
 * supplies their Connect server URL and an access token. The desktop/web apps keep using the existing
 * file (1PUX/CSV) importers; this is a mobile-only convenience.
 *
 * Endpoints used (Connect v1):
 *   GET {host}/v1/vaults
 *   GET {host}/v1/vaults/{vaultId}/items
 *   GET {host}/v1/vaults/{vaultId}/items/{itemId}   (full item incl. fields)
 */
@Singleton
class OnePasswordConnectImporter @Inject constructor() {

    private val json = Json { ignoreUnknownKeys = true; isLenient = true; explicitNulls = false }

    private val client = OkHttpClient.Builder()
        .connectTimeout(20, TimeUnit.SECONDS)
        .readTimeout(30, TimeUnit.SECONDS)
        .build()

    data class Result(val items: List<LoginItemInput>, val error: String? = null)

    /** Fetch and map every readable item from every vault the token can see. */
    suspend fun fetch(host: String, token: String): Result = withContext(Dispatchers.IO) {
        val base = host.trim().trimEnd('/')
        when {
            base.isBlank() -> return@withContext Result(emptyList(), "Enter your 1Password Connect server URL.")
            !base.startsWith("http", true) -> return@withContext Result(emptyList(), "The server URL must start with http:// or https://.")
            token.isBlank() -> return@withContext Result(emptyList(), "Enter your Connect access token.")
        }
        try {
            val vaults = getJson<List<OpVault>>("$base/v1/vaults", token)
            val out = mutableListOf<LoginItemInput>()
            for (v in vaults) {
                val summaries = getJson<List<OpItemSummary>>("$base/v1/vaults/${v.id}/items", token)
                for (s in summaries) {
                    runCatching { getJson<OpItem>("$base/v1/vaults/${v.id}/items/${s.id}", token) }
                        .getOrNull()?.let { out += it.toInput() }
                }
            }
            Result(out)
        } catch (e: Exception) {
            Result(emptyList(), e.message ?: "1Password import failed.")
        }
    }

    private inline fun <reified T> getJson(url: String, token: String): T {
        val req = Request.Builder()
            .url(url)
            .header("Authorization", "Bearer $token")
            .header("Accept", "application/json")
            .build()
        client.newCall(req).execute().use { resp ->
            val body = resp.body?.string().orEmpty()
            if (!resp.isSuccessful) {
                throw IOException(
                    when (resp.code) {
                        401, 403 -> "Access token was rejected (HTTP ${resp.code})."
                        404 -> "Server or vault not found (HTTP 404). Check the URL."
                        else -> "1Password Connect returned HTTP ${resp.code}."
                    }
                )
            }
            return json.decodeFromString(body)
        }
    }

    private fun OpItem.toInput(): LoginItemInput {
        val username = fields.firstOrNull { it.purpose == "USERNAME" }?.value
        val password = fields.firstOrNull { it.purpose == "PASSWORD" }?.value
        val otp = fields.firstOrNull { it.type == "OTP" }?.value
        val website = urls.firstOrNull { it.primary }?.href ?: urls.firstOrNull()?.href
        val note = fields.firstOrNull { it.purpose == "NOTES" || it.id == "notesPlain" }?.value

        // Any remaining valued fields become custom fields (concealed ones stay secret).
        val custom = fields
            .filter {
                it.value.isNotBlank() && it.label.isNotBlank() &&
                    it.purpose != "USERNAME" && it.purpose != "PASSWORD" && it.purpose != "NOTES" &&
                    it.type != "OTP" && it.id != "notesPlain"
            }
            .map { CustomFieldData(name = it.label, value = it.value, secret = it.type == "CONCEALED") }

        return LoginItemInput(
            title = title.ifBlank { "Untitled" },
            type = mapType(category),
            username = username,
            website = website,
            password = password,
            totpSecret = otp?.let { runCatching { Totp.secretFromUri(it) }.getOrNull() ?: it },
            notes = note,
            categoryName = friendlyCategory(category),
            customFields = custom,
        )
    }

    private fun mapType(category: String): ItemType = when (category.uppercase()) {
        "LOGIN" -> ItemType.Login
        "PASSWORD" -> ItemType.Password
        "SECURE_NOTE" -> ItemType.SecureNote
        "CREDIT_CARD" -> ItemType.CreditCard
        "WIRELESS_ROUTER" -> ItemType.WiFi
        "IDENTITY" -> ItemType.Identity
        else -> ItemType.Login
    }

    private fun friendlyCategory(category: String): String = when (category.uppercase()) {
        "LOGIN" -> "Logins"
        "PASSWORD" -> "Passwords"
        "SECURE_NOTE" -> "Secure Notes"
        "CREDIT_CARD" -> "Credit Cards"
        "WIRELESS_ROUTER" -> "WiFi Networks"
        "IDENTITY" -> "Identities"
        else -> "Logins"
    }

    // ---- Connect REST DTOs (subset) ----------------------------------------

    @Serializable private data class OpVault(val id: String = "", val name: String = "")
    @Serializable private data class OpItemSummary(val id: String = "", val title: String = "", val category: String = "")
    @Serializable private data class OpUrl(val href: String = "", val primary: Boolean = false)
    @Serializable private data class OpField(
        val id: String = "",
        val type: String = "",
        val purpose: String = "",
        val label: String = "",
        val value: String = "",
    )
    @Serializable private data class OpItem(
        val id: String = "",
        val title: String = "",
        val category: String = "",
        val favorite: Boolean = false,
        val urls: List<OpUrl> = emptyList(),
        val fields: List<OpField> = emptyList(),
    )
}
