package com.vaultguard.app.data.repo

import com.vaultguard.app.config.ConfigStore
import com.vaultguard.app.config.ConnectionMode
import com.vaultguard.app.config.SecureStore
import com.vaultguard.app.data.model.EnhancedLoginRequest
import com.vaultguard.app.data.remote.ApiProvider
import com.vaultguard.app.domain.VaultCrypto
import kotlinx.coroutines.flow.first
import javax.inject.Inject
import javax.inject.Singleton

/** Handles sign-in for API mode and vault unlock for LOCAL mode. */
@Singleton
class AuthRepository @Inject constructor(
    private val apiProvider: ApiProvider,
    private val configStore: ConfigStore,
    private val secureStore: SecureStore,
    private val session: SessionManager,
    private val crypto: VaultCrypto,
) {
    private companion object {
        const val VERIFIER_PLAINTEXT = "vaultguard-local-verifier-v1"
    }

    suspend fun login(email: String, password: String, twoFactorCode: String?): LoginResult {
        return try {
            val api = apiProvider.api()
            val resp = api.login(
                EnhancedLoginRequest(
                    email = email.trim(),
                    password = password,
                    twoFactorCode = twoFactorCode?.takeIf { it.isNotBlank() },
                )
            )
            when {
                resp.requiresTwoFactor && twoFactorCode.isNullOrBlank() ->
                    LoginResult.NeedsTwoFactor(resp.twoFactorToken)

                resp.authResponse?.token?.isNotBlank() == true -> {
                    session.onLoggedIn(resp.authResponse.token, resp.authResponse.user, password)
                    LoginResult.Success
                }

                else -> LoginResult.Error("Login failed. Check your credentials and try again.")
            }
        } catch (e: Exception) {
            LoginResult.Error(e.message ?: "Unable to reach the server.")
        }
    }

    /**
     * LOCAL mode unlock. On first use it establishes the master password by writing an encrypted verifier;
     * afterwards it validates the entered password against that verifier.
     */
    suspend fun unlockLocal(masterPassword: String): LoginResult {
        return try {
            val saltB64 = secureStore.localVaultSalt
            if (saltB64 == null || secureStore.localVerifier == null) {
                // First run: set the master password.
                val salt = crypto.newSalt()
                val key = crypto.deriveKey(masterPassword, salt)
                secureStore.localVaultSalt = crypto.toBase64(salt)
                secureStore.localVerifier = crypto.encrypt(VERIFIER_PLAINTEXT, key)
                session.onLocalUnlocked(masterPassword)
                LoginResult.Success
            } else {
                val salt = crypto.fromBase64(saltB64)
                val key = crypto.deriveKey(masterPassword, salt)
                val ok = runCatching { crypto.decrypt(secureStore.localVerifier!!, key) }
                    .getOrNull() == VERIFIER_PLAINTEXT
                if (ok) {
                    session.onLocalUnlocked(masterPassword)
                    LoginResult.Success
                } else {
                    LoginResult.Error("Incorrect master password.")
                }
            }
        } catch (e: Exception) {
            LoginResult.Error(e.message ?: "Unable to unlock the local vault.")
        }
    }

    suspend fun currentMode(): ConnectionMode = configStore.config.first().mode
}
