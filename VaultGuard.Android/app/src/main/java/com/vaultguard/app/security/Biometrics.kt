package com.vaultguard.app.security

import android.content.Context
import androidx.biometric.BiometricManager
import androidx.biometric.BiometricPrompt
import androidx.core.content.ContextCompat
import androidx.fragment.app.FragmentActivity
import javax.crypto.Cipher

object Biometrics {

    private const val AUTHENTICATORS =
        BiometricManager.Authenticators.BIOMETRIC_STRONG or BiometricManager.Authenticators.DEVICE_CREDENTIAL

    // A CryptoObject-bound prompt can only use a class-3 (STRONG) sensor — DEVICE_CREDENTIAL (PIN/pattern)
    // can't be combined with a Keystore key that requires biometric auth, so this is biometric-only.
    private const val CRYPTO_AUTHENTICATORS = BiometricManager.Authenticators.BIOMETRIC_STRONG

    fun isAvailable(context: Context): Boolean =
        BiometricManager.from(context).canAuthenticate(AUTHENTICATORS) == BiometricManager.BIOMETRIC_SUCCESS

    fun isStrongBiometricAvailable(context: Context): Boolean =
        BiometricManager.from(context).canAuthenticate(CRYPTO_AUTHENTICATORS) == BiometricManager.BIOMETRIC_SUCCESS

    /** Show the system biometric/credential prompt as a second factor. Proves presence only — releases no secret. */
    fun prompt(
        activity: FragmentActivity,
        title: String = "Unlock VaultGuard",
        subtitle: String = "Confirm it's you to finish unlocking your vault",
        onResult: (Boolean) -> Unit,
    ) {
        if (!isAvailable(activity)) { onResult(true); return } // no biometrics enrolled → don't block

        val executor = ContextCompat.getMainExecutor(activity)
        val prompt = BiometricPrompt(activity, executor, object : BiometricPrompt.AuthenticationCallback() {
            override fun onAuthenticationSucceeded(result: BiometricPrompt.AuthenticationResult) = onResult(true)
            override fun onAuthenticationError(errorCode: Int, errString: CharSequence) = onResult(false)
            // onAuthenticationFailed = a single bad attempt; the prompt stays open, so don't resolve here.
        })

        val info = BiometricPrompt.PromptInfo.Builder()
            .setTitle(title)
            .setSubtitle(subtitle)
            .setAllowedAuthenticators(AUTHENTICATORS)
            .build()
        prompt.authenticate(info)
    }

    /**
     * Show the system biometric prompt bound to [cipher] via a CryptoObject. The Keystore's secure
     * hardware — not this app process — decides whether [cipher] is allowed to run; [onResult]
     * receives the authenticated Cipher (ready to decrypt) on success, or null on cancel/failure.
     * Use this (not the boolean [prompt]) whenever a secret is actually being released.
     */
    fun promptForDecrypt(
        activity: FragmentActivity,
        cipher: Cipher,
        title: String = "Unlock VaultGuard",
        subtitle: String = "Use your fingerprint or face to unlock your vault",
        onResult: (Cipher?) -> Unit,
    ) {
        val executor = ContextCompat.getMainExecutor(activity)
        val prompt = BiometricPrompt(activity, executor, object : BiometricPrompt.AuthenticationCallback() {
            override fun onAuthenticationSucceeded(result: BiometricPrompt.AuthenticationResult) =
                onResult(result.cryptoObject?.cipher)
            override fun onAuthenticationError(errorCode: Int, errString: CharSequence) = onResult(null)
        })

        val info = BiometricPrompt.PromptInfo.Builder()
            .setTitle(title)
            .setSubtitle(subtitle)
            .setAllowedAuthenticators(CRYPTO_AUTHENTICATORS)
            .build()
        prompt.authenticate(info, BiometricPrompt.CryptoObject(cipher))
    }
}
