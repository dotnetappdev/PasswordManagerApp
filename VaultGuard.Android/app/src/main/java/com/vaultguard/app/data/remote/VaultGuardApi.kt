package com.vaultguard.app.data.remote

import com.vaultguard.app.data.model.CategoryDto
import com.vaultguard.app.data.model.CreateEncryptedPasswordItem
import com.vaultguard.app.data.model.DecryptedPasswordItemDto
import com.vaultguard.app.data.model.EnhancedLoginRequest
import com.vaultguard.app.data.model.LoginResponse
import com.vaultguard.app.data.model.PasswordItemDto
import com.vaultguard.app.data.model.RevealPasswordRequest
import com.vaultguard.app.data.model.RevealPasswordResponse
import com.vaultguard.app.data.model.TagDto
import com.vaultguard.app.data.model.UpdatePasswordItem
import com.vaultguard.app.data.model.VaultDto
import retrofit2.http.Body
import retrofit2.http.DELETE
import retrofit2.http.GET
import retrofit2.http.PATCH
import retrofit2.http.POST
import retrofit2.http.PUT
import retrofit2.http.Path
import retrofit2.http.Query

/** VaultGuard REST API surface used by the app in API connection mode. */
interface VaultGuardApi {

    @POST("api/auth/login/enhanced")
    suspend fun login(@Body body: EnhancedLoginRequest): LoginResponse

    @GET("api/passworditems")
    suspend fun getItems(): List<PasswordItemDto>

    @GET("api/passworditems/{id}")
    suspend fun getItem(@Path("id") id: Int): PasswordItemDto

    @GET("api/passworditems/search")
    suspend fun search(@Query("searchTerm") term: String): List<PasswordItemDto>

    @POST("api/passworditems/encrypted")
    suspend fun createEncrypted(@Body body: CreateEncryptedPasswordItem): PasswordItemDto

    @PUT("api/passworditems/{id}")
    suspend fun update(@Path("id") id: Int, @Body body: UpdatePasswordItem): PasswordItemDto

    @DELETE("api/passworditems/{id}")
    suspend fun delete(@Path("id") id: Int)

    @PATCH("api/passworditems/{id}/toggle-favorite")
    suspend fun toggleFavorite(@Path("id") id: Int)

    /** Reveal a single password using the master password (sent over TLS, verified server-side). */
    @POST("api/passworditems/{id}/reveal")
    suspend fun reveal(@Path("id") id: Int, @Body body: RevealPasswordRequest): RevealPasswordResponse

    /** Full session-based decrypt (password + TOTP secret + fields). Uses the Bearer session token. */
    @POST("api/passworditems/{id}/decrypt")
    suspend fun decrypt(@Path("id") id: Int): DecryptedPasswordItemDto

    @GET("api/categories")
    suspend fun getCategories(): List<CategoryDto>

    @GET("api/tags")
    suspend fun getTags(): List<TagDto>

    @GET("api/vaults")
    suspend fun getVaults(): List<VaultDto>
}
