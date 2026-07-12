package com.vaultguard.app.ui.qr

import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.Login
import androidx.compose.material.icons.filled.Link
import androidx.compose.material.icons.filled.Timer
import androidx.compose.material.icons.filled.Wifi
import androidx.compose.material.icons.filled.TextSnippet
import androidx.compose.ui.graphics.vector.ImageVector

/** A parsed, human-friendly view of a scanned QR code's contents. */
sealed class QrContent(val raw: String) {
    abstract val title: String
    abstract val icon: ImageVector
    /** Ordered label→value pairs to show in the preview card. */
    abstract val fields: List<Pair<String, String>>

    class Totp(raw: String, val account: String?, val issuer: String?, val secret: String) : QrContent(raw) {
        override val title = "Authenticator setup"
        override val icon = Icons.Filled.Timer
        override val fields = buildList {
            issuer?.takeIf { it.isNotBlank() }?.let { add("Service" to it) }
            account?.takeIf { it.isNotBlank() }?.let { add("Account" to it) }
            add("Secret" to secret.chunked(4).joinToString(" "))
        }
    }

    class Wifi(raw: String, val ssid: String, val security: String?, val password: String?) : QrContent(raw) {
        override val title = "Wi-Fi network"
        override val icon = Icons.Filled.Wifi
        override val fields = buildList {
            add("Network" to ssid)
            security?.takeIf { it.isNotBlank() }?.let { add("Security" to it) }
            password?.takeIf { it.isNotBlank() }?.let { add("Password" to it) }
        }
    }

    class Login(raw: String, val token: String) : QrContent(raw) {
        override val title = "VaultGuard sign-in"
        override val icon = Icons.AutoMirrored.Filled.Login
        override val fields = listOf("Request" to "Approve sign-in on the computer showing this code")
    }

    class Url(raw: String, val url: String) : QrContent(raw) {
        override val title = "Website link"
        override val icon = Icons.Filled.Link
        override val fields = listOf("URL" to url)
    }

    class Text(raw: String) : QrContent(raw) {
        override val title = "Text"
        override val icon = Icons.Filled.TextSnippet
        override val fields = listOf("Content" to raw)
    }

    companion object {
        fun parse(raw: String): QrContent {
            val trimmed = raw.trim()
            return when {
                trimmed.startsWith("otpauth://", true) -> parseTotp(trimmed)
                trimmed.startsWith("WIFI:", true) -> parseWifi(trimmed)
                trimmed.startsWith("vaultguard://", true) ||
                    (trimmed.contains("token=", true) && trimmed.contains("login", true)) ->
                    Login(trimmed, trimmed.substringAfter("token=", trimmed).substringBefore('&'))
                trimmed.startsWith("http://", true) || trimmed.startsWith("https://", true) -> Url(trimmed, trimmed)
                else -> Text(trimmed)
            }
        }

        private fun parseTotp(raw: String): Totp {
            val query = raw.substringAfter('?', "")
            fun param(name: String) = query.split('&')
                .firstOrNull { it.startsWith("$name=", true) }?.substringAfter('=')
                ?.let { java.net.URLDecoder.decode(it, "UTF-8") }
            val label = raw.substringAfter("otpauth://totp/", "").substringBefore('?')
                .let { java.net.URLDecoder.decode(it, "UTF-8") }
            val account = label.substringAfter(':', label).ifBlank { null }
            val issuer = param("issuer") ?: label.substringBefore(':', "").ifBlank { null }
            return Totp(raw, account, issuer, param("secret") ?: "")
        }

        private fun parseWifi(raw: String): Wifi {
            // WIFI:S:MySSID;T:WPA;P:pass;;
            val body = raw.removePrefix("WIFI:").removePrefix("wifi:")
            fun field(key: String) = Regex("$key:((?:[^;\\\\]|\\\\.)*);")
                .find(body)?.groupValues?.get(1)?.replace("\\", "")
            return Wifi(raw, field("S") ?: "(hidden)", field("T"), field("P"))
        }
    }
}
