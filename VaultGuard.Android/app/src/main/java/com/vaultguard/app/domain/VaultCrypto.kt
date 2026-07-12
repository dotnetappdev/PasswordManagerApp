package com.vaultguard.app.domain

import android.util.Base64
import java.security.SecureRandom
import javax.crypto.Cipher
import javax.crypto.SecretKeyFactory
import javax.crypto.spec.GCMParameterSpec
import javax.crypto.spec.PBEKeySpec
import javax.crypto.spec.SecretKeySpec
import javax.inject.Inject
import javax.inject.Singleton

/**
 * On-device encryption for LOCAL connection mode. Byte-for-byte compatible with the desktop
 * `VaultGuard.Crypto` scheme:
 *  - Key derivation: PBKDF2-HMAC-SHA256, 600,000 iterations, 32-byte (256-bit) key (OWASP 2024).
 *  - Cipher: AES-256-GCM, 12-byte nonce, 16-byte auth tag, no associated data.
 *  - Container: the same three components WPF stores in `EncryptedPasswordData`
 *    (Nonce, EncryptedPassword/ciphertext, AuthenticationTag), here joined as
 *    `base64(nonce):base64(ciphertext):base64(tag)`.
 *
 * This means a value encrypted here maps directly onto WPF's EncryptedPassword/Nonce/PasswordAuthTag
 * columns and vice-versa (given the same derived key). In API mode the server does the crypto instead.
 */
@Singleton
class VaultCrypto @Inject constructor() {

    fun newSalt(): ByteArray = ByteArray(SALT_BYTES).also { SecureRandom().nextBytes(it) }

    fun deriveKey(masterPassword: String, salt: ByteArray): ByteArray {
        val spec = PBEKeySpec(masterPassword.toCharArray(), salt, ITERATIONS, KEY_BITS)
        val factory = SecretKeyFactory.getInstance("PBKDF2WithHmacSHA256")
        return factory.generateSecret(spec).encoded
    }

    fun encrypt(plaintext: String, key: ByteArray): String {
        val nonce = ByteArray(NONCE_BYTES).also { SecureRandom().nextBytes(it) }
        val cipher = Cipher.getInstance(TRANSFORM)
        cipher.init(Cipher.ENCRYPT_MODE, SecretKeySpec(key, "AES"), GCMParameterSpec(TAG_BITS, nonce))
        // Java's GCM output is ciphertext || tag(16); split them to match WPF's separate fields.
        val ctWithTag = cipher.doFinal(plaintext.toByteArray(Charsets.UTF_8))
        val ct = ctWithTag.copyOfRange(0, ctWithTag.size - TAG_BYTES)
        val tag = ctWithTag.copyOfRange(ctWithTag.size - TAG_BYTES, ctWithTag.size)
        return "${b64(nonce)}:${b64(ct)}:${b64(tag)}"
    }

    fun decrypt(encoded: String, key: ByteArray): String {
        val parts = encoded.split(':')
        require(parts.size == 3) { "Unexpected encrypted value format" }
        val nonce = fromBase64(parts[0])
        val ct = fromBase64(parts[1])
        val tag = fromBase64(parts[2])
        val cipher = Cipher.getInstance(TRANSFORM)
        cipher.init(Cipher.DECRYPT_MODE, SecretKeySpec(key, "AES"), GCMParameterSpec(TAG_BITS, nonce))
        return String(cipher.doFinal(ct + tag), Charsets.UTF_8)
    }

    fun toBase64(bytes: ByteArray): String = b64(bytes)
    fun fromBase64(s: String): ByteArray = Base64.decode(s, Base64.NO_WRAP)
    private fun b64(bytes: ByteArray): String = Base64.encodeToString(bytes, Base64.NO_WRAP)

    private companion object {
        const val ITERATIONS = 600_000
        const val KEY_BITS = 256
        const val SALT_BYTES = 16
        const val NONCE_BYTES = 12
        const val TAG_BYTES = 16
        const val TAG_BITS = 128
        const val TRANSFORM = "AES/GCM/NoPadding"
    }
}
