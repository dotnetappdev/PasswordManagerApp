package com.vaultguard.app.domain

import java.security.SecureRandom
import javax.inject.Inject
import javax.inject.Singleton

data class PasswordOptions(
    val length: Int = 20,
    val upper: Boolean = true,
    val lower: Boolean = true,
    val digits: Boolean = true,
    val symbols: Boolean = true,
    val avoidAmbiguous: Boolean = false,
)

/** Cryptographically-strong password generator mirroring the WPF/Blazor generator. */
@Singleton
class PasswordGenerator @Inject constructor() {

    private val random = SecureRandom()

    fun generate(options: PasswordOptions): String {
        val ambiguous = "O0oIl1|`'\""
        fun filter(s: String) = if (options.avoidAmbiguous) s.filterNot { it in ambiguous } else s

        val pools = buildList {
            if (options.upper) add(filter("ABCDEFGHIJKLMNOPQRSTUVWXYZ"))
            if (options.lower) add(filter("abcdefghijklmnopqrstuvwxyz"))
            if (options.digits) add(filter("0123456789"))
            if (options.symbols) add(filter("!@#\$%^&*()-_=+[]{};:,.?/"))
        }.filter { it.isNotEmpty() }

        if (pools.isEmpty()) return ""
        val length = options.length.coerceIn(4, 128)
        val all = pools.joinToString("")
        val out = StringBuilder(length)

        // Guarantee at least one char from each selected pool.
        pools.forEach { pool -> out.append(pool[random.nextInt(pool.length)]) }
        while (out.length < length) out.append(all[random.nextInt(all.length)])

        // Shuffle so the guaranteed characters aren't always first.
        return out.toString().toCharArray().also { chars ->
            for (i in chars.indices.reversed()) {
                val j = random.nextInt(i + 1)
                val tmp = chars[i]; chars[i] = chars[j]; chars[j] = tmp
            }
        }.concatToString()
    }

    /** 0..4 strength estimate for a live preview meter. */
    fun strength(password: String): Int {
        if (password.isEmpty()) return 0
        var score = 0
        if (password.length >= 12) score++
        if (password.length >= 16) score++
        if (password.any { it.isDigit() } && password.any { it.isLetter() }) score++
        if (password.any { !it.isLetterOrDigit() }) score++
        return score.coerceIn(0, 4)
    }
}
