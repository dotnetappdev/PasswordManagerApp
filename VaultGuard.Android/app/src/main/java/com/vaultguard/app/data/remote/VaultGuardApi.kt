package com.vaultguard.app.data.remote

import com.vaultguard.app.data.model.ApprovalStateResponse
import com.vaultguard.app.data.model.CategoryDto
import com.vaultguard.app.data.model.CreateEncryptedPasswordItem
import com.vaultguard.app.data.model.PendingApprovalDto
import com.vaultguard.app.data.model.RespondApprovalRequest
import com.vaultguard.app.data.model.CreateVaultDto
import com.vaultguard.app.data.model.UpdateVaultDto
import com.vaultguard.app.data.model.DecryptedPasswordItemDto
import com.vaultguard.app.data.model.EnhancedLoginRequest
import com.vaultguard.app.data.model.LoginResponse
import com.vaultguard.app.data.model.PasswordItemDto
import com.vaultguard.app.data.model.QrAuthenticateRequest
import com.vaultguard.app.data.model.QrAuthenticateResponse
import com.vaultguard.app.data.model.RegisterDeviceRequest
import com.vaultguard.app.data.model.RevealPasswordRequest
import com.vaultguard.app.data.model.RevealPasswordResponse
import com.vaultguard.app.data.model.TagDto
import com.vaultguard.app.data.model.UpdatePasswordItem
import com.vaultguard.app.data.model.VaultDto
import retrofit2.http.Body
import retrofit2.http.DELETE
import retrofit2.http.GET
import retrofit2.http.HTTP
import retrofit2.http.PATCH
import retrofit2.http.POST
import retrofit2.http.PUT
import retrofit2.http.Path
import retrofit2.http.Query

/** VaultGuard REST API surface used by the app in API connection mode. */
interface VaultGuardApi {

    @POST("api/auth/login/enhanced")
    suspend fun login(@Body body: EnhancedLoginRequest): LoginResponse

    /** Approve a desktop/web QR login: signs the displaying device in using this phone's credentials. */
    @POST("api/auth/qr/authenticate")
    suspend fun qrAuthenticate(@Body body: QrAuthenticateRequest): QrAuthenticateResponse

    /** Register this device's FCM push token so the server can send notifications. */
    @POST("api/push/register")
    suspend fun registerPush(@Body body: RegisterDeviceRequest)

    // Passkeys
    @GET("api/passkey")
    suspend fun getPasskeys(): com.vaultguard.app.data.model.PasskeyListResponse

    @GET("api/passkey/status")
    suspend fun passkeyStatus(): com.vaultguard.app.data.model.PasskeyStatus

    @POST("api/passkey/register/start")
    suspend fun passkeyRegisterStart(@Body body: com.vaultguard.app.data.model.PasskeyRegistrationStartRequest): com.vaultguard.app.data.model.PasskeyRegistrationStartResponse

    @POST("api/passkey/register/complete")
    suspend fun passkeyRegisterComplete(@Body body: com.vaultguard.app.data.model.PasskeyRegistrationComplete)

    @HTTP(method = "DELETE", path = "api/passkey/{id}", hasBody = true)
    suspend fun deletePasskey(@Path("id") id: Int, @Body body: com.vaultguard.app.data.model.PasskeyDeleteRequest)

    /** Real WebAuthn sign-in (no email/password): server verifies the assertion against the account's passkey. */
    @POST("api/passkey/authenticate/start")
    suspend fun passkeyAuthenticateStart(@Body body: com.vaultguard.app.data.model.PasskeyAuthenticationStartRequest): com.vaultguard.app.data.model.PasskeyAuthenticationStartResponse

    @POST("api/passkey/authenticate/complete")
    suspend fun passkeyAuthenticateComplete(@Body body: com.vaultguard.app.data.model.PasskeyAuthenticationComplete): com.vaultguard.app.data.model.AuthResponse

    @GET("api/approvals/pending")
    suspend fun pendingApprovals(): List<PendingApprovalDto>

    @POST("api/approvals/{id}/respond")
    suspend fun respondApproval(@Path("id") id: String, @Body body: RespondApprovalRequest): ApprovalStateResponse

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

    @POST("api/vaults")
    suspend fun createVault(@Body body: CreateVaultDto): VaultDto

    @PUT("api/vaults/{id}")
    suspend fun updateVault(@Path("id") id: Int, @Body body: UpdateVaultDto): VaultDto

    @DELETE("api/vaults/{id}")
    suspend fun deleteVault(@Path("id") id: Int)
}
