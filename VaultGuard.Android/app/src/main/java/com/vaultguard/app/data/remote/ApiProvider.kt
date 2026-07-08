package com.vaultguard.app.data.remote

import retrofit2.converter.kotlinx.serialization.asConverterFactory
import com.vaultguard.app.config.ConfigStore
import com.vaultguard.app.config.SecureStore
import com.vaultguard.app.data.repo.SessionManager
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.withContext
import kotlinx.serialization.json.Json
import okhttp3.Interceptor
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.Retrofit
import javax.inject.Inject
import javax.inject.Singleton

/**
 * Builds (and caches) a [VaultGuardApi] for the currently configured base URL. Retrofit needs the base
 * URL at build time, so we rebuild when the configured URL changes. Auth headers are added per-request by
 * [authInterceptor] so a key/token change takes effect without a rebuild.
 */
@Singleton
class ApiProvider @Inject constructor(
    private val configStore: ConfigStore,
    private val secureStore: SecureStore,
    private val session: SessionManager,
) {
    private val json = Json {
        ignoreUnknownKeys = true
        isLenient = true
        explicitNulls = false
    }

    @Volatile private var cachedUrl: String? = null
    @Volatile private var cachedApi: VaultGuardApi? = null

    private val authInterceptor = Interceptor { chain ->
        val builder = chain.request().newBuilder()
        secureStore.apiKey?.let { builder.header("X-API-Key", it) }
        // Session token doubles as the vault-session id for reveal/decrypt endpoints.
        session.sessionToken?.let { builder.header("Authorization", "Bearer $it") }
        builder.header("Accept", "application/json")
        chain.proceed(builder.build())
    }

    suspend fun api(): VaultGuardApi {
        val cfg = configStore.config.first()
        val base = cfg.normalizedBaseUrl
        require(base.isNotBlank()) { "API base URL is not configured." }

        cachedApi?.let { if (cachedUrl == base) return it }

        val logging = HttpLoggingInterceptor().apply { level = HttpLoggingInterceptor.Level.BASIC }
        val client = OkHttpClient.Builder()
            .addInterceptor(authInterceptor)
            .addInterceptor(logging)
            .build()

        val retrofit = Retrofit.Builder()
            .baseUrl(base)
            .client(client)
            .addConverterFactory(json.asConverterFactory("application/json".toMediaType()))
            .build()

        return retrofit.create(VaultGuardApi::class.java).also {
            cachedApi = it
            cachedUrl = base
        }
    }

    /**
     * Verify a URL + key combination before saving it. Hits an authorized endpoint so both reachability
     * and key validity are checked. Returns a human-readable error message on failure.
     */
    suspend fun testConnection(baseUrl: String, apiKey: String): Result<Unit> = withContext(Dispatchers.IO) {
        try {
            val normalized = if (baseUrl.endsWith("/")) baseUrl else "$baseUrl/"
            val request = Request.Builder()
                .url("${normalized}api/passworditems")
                .header("X-API-Key", apiKey)
                .header("Accept", "application/json")
                .get()
                .build()
            OkHttpClient().newCall(request).execute().use { resp ->
                when {
                    resp.isSuccessful -> Result.success(Unit)
                    resp.code == 401 -> Result.failure(Exception("Unauthorized — check the API key."))
                    else -> Result.failure(Exception("Server returned HTTP ${resp.code}."))
                }
            }
        } catch (e: Exception) {
            Result.failure(Exception(e.message ?: "Could not reach the server."))
        }
    }
}
