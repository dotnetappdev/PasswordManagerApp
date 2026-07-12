package com.vaultguard.app.domain

import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import java.net.HttpURLConnection
import java.net.URL
import java.security.MessageDigest
import javax.inject.Inject
import javax.inject.Singleton

/**
 * Privacy-preserving breach check via the Have I Been Pwned "Pwned Passwords" range API with
 * **k-anonymity**: the password is SHA-1 hashed on-device and only the first five hex characters of the
 * hash are ever sent. The server returns every suffix under that prefix; we match the remaining 35 chars
 * locally. `Add-Padding` hides whether a match exists. No API key required.
 *
 * Returns the number of times the password appears in known breaches, or **null** when the lookup itself
 * failed (offline/timeout) — callers must treat null as "unknown", never "safe".
 */
@Singleton
class BreachChecker @Inject constructor() {

    suspend fun timesSeen(password: String): Int? = withContext(Dispatchers.IO) {
        if (password.isEmpty()) return@withContext 0
        runCatching {
            val hash = sha1Hex(password)
            val prefix = hash.substring(0, 5)
            val suffix = hash.substring(5)

            val conn = (URL(RANGE_URL + prefix).openConnection() as HttpURLConnection).apply {
                requestMethod = "GET"
                connectTimeout = 15_000
                readTimeout = 15_000
                setRequestProperty("Add-Padding", "true")
                setRequestProperty("User-Agent", "VaultGuard-Android")
            }
            try {
                if (conn.responseCode != 200) return@runCatching null
                conn.inputStream.bufferedReader().useLines { lines ->
                    for (line in lines) {
                        val idx = line.indexOf(':')
                        if (idx <= 0) continue
                        if (line.substring(0, idx).trim().equals(suffix, ignoreCase = true)) {
                            return@runCatching line.substring(idx + 1).trim().toIntOrNull() ?: 0
                        }
                    }
                }
                0
            } finally {
                conn.disconnect()
            }
        }.getOrNull()
    }

    private fun sha1Hex(input: String): String =
        MessageDigest.getInstance("SHA-1")
            .digest(input.toByteArray(Charsets.UTF_8))
            .joinToString("") { "%02X".format(it) }

    private companion object {
        const val RANGE_URL = "https://api.pwnedpasswords.com/range/"
    }
}
