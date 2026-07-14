package com.vaultguard.app.security

import android.security.keystore.KeyGenParameterSpec
import android.security.keystore.KeyProperties
import android.util.Base64
import java.security.KeyPairGenerator
import java.security.KeyStore
import java.security.PrivateKey
import java.security.PublicKey
import javax.crypto.Cipher
import javax.crypto.KeyGenerator
import javax.crypto.spec.GCMParameterSpec
import javax.crypto.spec.SecretKeySpec

/**
 * Envelope encryption bound to a hardware-backed Android Keystore key that requires a fresh
 * biometric authentication to unwrap. Sealing (locking a secret away) needs no prompt — it only
 * uses the RSA public key. Opening needs a BiometricPrompt to hand back an authenticated
 * [Cipher] wrapping the RSA private key; the Keystore's TEE/StrongBox refuses that private-key
 * operation without a matching recent biometric event, so release can't be bypassed by reading
 * SharedPreferences directly the way a plain boolean-callback prompt could be.
 */
object BiometricCrypto {
    private const val ANDROID_KEYSTORE = "AndroidKeyStore"
    private const val KEY_ALIAS = "vg_quick_unlock_rsa"
    private const val RSA_TRANSFORM = "RSA/ECB/OAEPWithSHA-256AndMGF1Padding"
    private const val AES_TRANSFORM = "AES/GCM/NoPadding"
    private const val GCM_TAG_BITS = 128

    private fun keyStore(): KeyStore = KeyStore.getInstance(ANDROID_KEYSTORE).apply { load(null) }

    private fun getOrCreateKeyPair(): Pair<PublicKey, PrivateKey> {
        val ks = keyStore()
        (ks.getEntry(KEY_ALIAS, null) as? KeyStore.PrivateKeyEntry)?.let {
            return it.certificate.publicKey to it.privateKey
        }

        val generator = KeyPairGenerator.getInstance(KeyProperties.KEY_ALGORITHM_RSA, ANDROID_KEYSTORE)
        val spec = KeyGenParameterSpec.Builder(KEY_ALIAS, KeyProperties.PURPOSE_ENCRYPT or KeyProperties.PURPOSE_DECRYPT)
            .setDigests(KeyProperties.DIGEST_SHA256)
            .setEncryptionPaddings(KeyProperties.ENCRYPTION_PADDING_RSA_OAEP)
            .setKeySize(2048)
            // Decrypt (private-key) use requires a biometric within this call; encrypt (public-key) never does.
            .setUserAuthenticationRequired(true)
            .setUserAuthenticationParameters(0, KeyProperties.AUTH_BIOMETRIC_STRONG)
            // A newly-enrolled fingerprint/face invalidates the key — an attacker who adds their
            // own biometric to an unlocked phone can't grandfather their way into the old secret.
            .setInvalidatedByBiometricEnrollment(true)
            .build()
        generator.initialize(spec)
        val pair = generator.generateKeyPair()
        return pair.public to pair.private
    }

    /** True once the hardware key exists (i.e. biometric quick unlock was set up at least once). */
    fun hasKey(): Boolean = runCatching { keyStore().containsAlias(KEY_ALIAS) }.getOrDefault(false)

    /** Discards the hardware key — call when disabling biometric quick unlock. */
    fun deleteKey() { runCatching { keyStore().deleteEntry(KEY_ALIAS) } }

    /** Encrypts [plaintext] behind the biometric-gated key. No prompt required — public-key only. */
    fun seal(plaintext: String): SealedSecret {
        val (publicKey, _) = getOrCreateKeyPair()
        val aesKey = KeyGenerator.getInstance(KeyProperties.KEY_ALGORITHM_AES).apply { init(256) }.generateKey()
        val aesCipher = Cipher.getInstance(AES_TRANSFORM).apply { init(Cipher.ENCRYPT_MODE, aesKey) }
        val ciphertext = aesCipher.doFinal(plaintext.toByteArray(Charsets.UTF_8))
        val iv = aesCipher.iv

        val rsaCipher = Cipher.getInstance(RSA_TRANSFORM).apply { init(Cipher.ENCRYPT_MODE, publicKey) }
        val wrappedKey = rsaCipher.doFinal(aesKey.encoded)
        return SealedSecret(wrappedKey, iv, ciphertext)
    }

    /** Cipher to hand to BiometricPrompt's CryptoObject; the returned, authenticated Cipher does the real unwrap. */
    fun unwrapCipher(): Cipher {
        val (_, privateKey) = getOrCreateKeyPair()
        return Cipher.getInstance(RSA_TRANSFORM).apply { init(Cipher.DECRYPT_MODE, privateKey) }
    }

    /** Finishes unwrapping [sealed] using the Cipher BiometricPrompt returned after a successful auth. */
    fun open(authenticatedCipher: Cipher, sealed: SealedSecret): String {
        val aesKeyBytes = authenticatedCipher.doFinal(sealed.wrappedKey)
        val aesKey = SecretKeySpec(aesKeyBytes, KeyProperties.KEY_ALGORITHM_AES)
        val aesCipher = Cipher.getInstance(AES_TRANSFORM).apply {
            init(Cipher.DECRYPT_MODE, aesKey, GCMParameterSpec(GCM_TAG_BITS, sealed.iv))
        }
        return String(aesCipher.doFinal(sealed.ciphertext), Charsets.UTF_8)
    }
}

/** Base64-joined envelope produced by [BiometricCrypto.seal], persisted as a single string. */
data class SealedSecret(val wrappedKey: ByteArray, val iv: ByteArray, val ciphertext: ByteArray) {
    fun encode(): String = listOf(wrappedKey, iv, ciphertext)
        .joinToString("|") { Base64.encodeToString(it, Base64.NO_WRAP) }

    companion object {
        fun decode(s: String): SealedSecret {
            val parts = s.split("|")
            require(parts.size == 3) { "Malformed sealed secret" }
            fun b(i: Int) = Base64.decode(parts[i], Base64.NO_WRAP)
            return SealedSecret(b(0), b(1), b(2))
        }
    }
}
