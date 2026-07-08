package com.vaultguard.app.domain

import java.nio.ByteBuffer
import javax.crypto.Mac
import javax.crypto.spec.SecretKeySpec
import kotlin.experimental.and

/**
 * RFC 6238 TOTP generator (SHA-1, 6 digits, 30s period) — matches VaultGuard's authenticator codes.
 * Also parses `otpauth://` URIs scanned from a QR code.
 */
object Totp {

    data class Code(val value: String, val secondsRemaining: Int)

    fun generate(base32Secret: String, timeMillis: Long = System.currentTimeMillis(), period: Int = 30, digits: Int = 6): Code? {
        val key = base32Decode(base32Secret.replace(" ", "").trim()) ?: return null
        val counter = timeMillis / 1000L / period
        val buffer = ByteBuffer.allocate(8).putLong(counter).array()

        val mac = Mac.getInstance("HmacSHA1")
        mac.init(SecretKeySpec(key, "HmacSHA1"))
        val hash = mac.doFinal(buffer)

        val offset = (hash[hash.size - 1] and 0x0f).toInt()
        val binary = ((hash[offset].toInt() and 0x7f) shl 24) or
            ((hash[offset + 1].toInt() and 0xff) shl 16) or
            ((hash[offset + 2].toInt() and 0xff) shl 8) or
            (hash[offset + 3].toInt() and 0xff)
        val otp = binary % Math.pow(10.0, digits.toDouble()).toInt()
        val code = otp.toString().padStart(digits, '0')
        val remaining = period - ((timeMillis / 1000L) % period).toInt()
        return Code(code, remaining)
    }

    /** Extract the secret from an `otpauth://totp/...?secret=XXXX` URI, or return the raw string. */
    fun secretFromUri(scanned: String): String {
        if (!scanned.startsWith("otpauth://", ignoreCase = true)) return scanned
        val query = scanned.substringAfter('?', "")
        val secret = query.split('&')
            .firstOrNull { it.startsWith("secret=", ignoreCase = true) }
            ?.substringAfter('=')
        return secret ?: scanned
    }

    private fun base32Decode(input: String): ByteArray? {
        val alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567"
        val clean = input.uppercase().replace("=", "")
        if (clean.isEmpty()) return null
        var bits = 0
        var value = 0
        val out = ArrayList<Byte>()
        for (c in clean) {
            val idx = alphabet.indexOf(c)
            if (idx < 0) return null
            value = (value shl 5) or idx
            bits += 5
            if (bits >= 8) {
                bits -= 8
                out.add(((value shr bits) and 0xff).toByte())
            }
        }
        return out.toByteArray()
    }
}
