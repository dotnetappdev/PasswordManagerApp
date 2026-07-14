package com.vaultguard.app.data.repo

import com.vaultguard.app.config.Account
import com.vaultguard.app.config.AccountsStore
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
    private val accountsStore: AccountsStore,
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
                    rememberAccount(email.trim())
                    LoginResult.Success
                }

                else -> LoginResult.Error("Login failed. Check your credentials and try again.")
            }
        } catch (e: retrofit2.HttpException) {
            // Surface the server's actual reason (e.g. "Invalid email or password") instead of a bare
            // "HTTP 401", so a wrong master key vs. an unreachable/misconfigured server are distinguishable.
            val serverMsg = runCatching { e.response()?.errorBody()?.string() }.getOrNull()
                ?.trim()?.removeSurrounding("\"")?.takeIf { it.isNotBlank() }
            LoginResult.Error(
                when (e.code()) {
                    401 -> serverMsg ?: "Invalid email or master password."
                    429 -> "Too many attempts. Wait a minute and try again."
                    else -> serverMsg ?: "Login failed (HTTP ${e.code()})."
                }
            )
        } catch (e: Exception) {
            LoginResult.Error(e.message ?: "Unable to reach the server.")
        }
    }

    /**
     * LOCAL mode unlock. On first use it establishes the master password by writing an encrypted verifier;
     * afterwards it validates the entered password against that verifier.
     */
    suspend fun unlockLocal(masterPassword: String, profileId: String = "default"): LoginResult {
        return try {
            val saltB64 = secureStore.localSalt(profileId)
            val verifier = secureStore.localVerifier(profileId)
            if (saltB64 == null || verifier == null) {
                // First unlock for this profile: establish its master password + verifier.
                val salt = crypto.newSalt()
                val key = crypto.deriveKey(masterPassword, salt)
                secureStore.setLocalSalt(profileId, crypto.toBase64(salt))
                secureStore.setLocalVerifier(profileId, crypto.encrypt(VERIFIER_PLAINTEXT, key))
                session.onLocalUnlocked(masterPassword, profileId)
                LoginResult.Success
            } else {
                val salt = crypto.fromBase64(saltB64)
                val key = crypto.deriveKey(masterPassword, salt)
                val ok = runCatching { crypto.decrypt(verifier, key) }.getOrNull() == VERIFIER_PLAINTEXT
                if (ok) {
                    session.onLocalUnlocked(masterPassword, profileId)
                    LoginResult.Success
                } else {
                    LoginResult.Error("Incorrect master password.")
                }
            }
        } catch (e: Exception) {
            LoginResult.Error(e.message ?: "Unable to unlock the local vault.")
        }
    }

    /** Starts a real WebAuthn sign-in: asks the server for assertion options for this account's passkey. */
    suspend fun passkeyAuthStart(email: String): Result<com.vaultguard.app.data.model.PasskeyAuthenticationStartResponse> =
        runCatching { apiProvider.api().passkeyAuthenticateStart(com.vaultguard.app.data.model.PasskeyAuthenticationStartRequest(email.trim())) }

    /** Completes the WebAuthn ceremony: the server cryptographically verifies the assertion and issues a real session token. */
    suspend fun passkeyAuthComplete(challenge: String, optionsJson: String, responseJson: String, email: String): LoginResult {
        return try {
            val auth = apiProvider.api().passkeyAuthenticateComplete(
                com.vaultguard.app.data.model.PasskeyAuthenticationComplete(
                    challenge = challenge, credentialResponse = responseJson, originalOptionsJson = optionsJson,
                )
            )
            if (auth.token.isNotBlank()) {
                // No master password: a passkey assertion proves identity, never the zero-knowledge vault key.
                session.onLoggedIn(auth.token, auth.user, null)
                rememberAccount(email.trim())
                LoginResult.Success
            } else LoginResult.Error("Passkey sign-in failed.")
        } catch (e: retrofit2.HttpException) {
            LoginResult.Error(if (e.code() == 401) "Passkey verification failed." else "Passkey sign-in failed (HTTP ${e.code()}).")
        } catch (e: Exception) {
            LoginResult.Error(e.message ?: "Unable to reach the server.")
        }
    }

    suspend fun currentMode(): ConnectionMode = configStore.config.first().mode

    /** Save the just-used connection as a switchable account. */
    private suspend fun rememberAccount(email: String) {
        val cfg = configStore.config.first()
        when (cfg.mode) {
            ConnectionMode.API -> accountsStore.upsert(
                Account(
                    id = "api:${cfg.apiBaseUrl}:$email",
                    label = email.ifBlank { "Account" },
                    email = email,
                    mode = ConnectionMode.API.name,
                    apiBaseUrl = cfg.apiBaseUrl,
                ),
                apiKey = secureStore.apiKey,
            )
            ConnectionMode.LOCAL -> accountsStore.upsert(
                Account(id = "local", label = "Local vault", email = null, mode = ConnectionMode.LOCAL.name),
                apiKey = null,
            )
        }
    }
}
