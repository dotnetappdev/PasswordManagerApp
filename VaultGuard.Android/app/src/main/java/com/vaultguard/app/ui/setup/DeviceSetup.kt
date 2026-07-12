package com.vaultguard.app.ui.setup

import android.graphics.Bitmap
import android.graphics.Color as AndroidColor
import androidx.compose.foundation.Image
import androidx.compose.foundation.layout.size
import androidx.compose.runtime.Composable
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.asImageBitmap
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import com.google.zxing.BarcodeFormat
import com.google.zxing.EncodeHintType
import com.google.zxing.qrcode.QRCodeWriter
import com.google.zxing.qrcode.decoder.ErrorCorrectionLevel
import java.net.URLDecoder
import java.net.URLEncoder

/**
 * Encodes/decodes the "Set Up Another Device" payload — the connection details another VaultGuard device
 * can scan to configure itself and sign in (like 1Password's device-setup QR). Format:
 *   vaultguard://setup?url=<apiUrl>&key=<apiKey>[&email=<email>]
 */
object DeviceSetup {
    fun encode(url: String, key: String, email: String? = null): String {
        fun e(s: String) = URLEncoder.encode(s, "UTF-8")
        val base = "vaultguard://setup?url=${e(url)}&key=${e(key)}"
        return if (email.isNullOrBlank()) base else "$base&email=${e(email)}"
    }

    data class Payload(val url: String, val key: String, val email: String?)

    fun parse(scanned: String): Payload? {
        if (!scanned.startsWith("vaultguard://setup", ignoreCase = true)) return null
        val params = scanned.substringAfter('?', "").split('&').mapNotNull {
            val i = it.indexOf('=')
            if (i < 0) null else URLDecoder.decode(it.substring(0, i), "UTF-8") to URLDecoder.decode(it.substring(i + 1), "UTF-8")
        }.toMap()
        val url = params["url"]; val key = params["key"]
        return if (!url.isNullOrBlank() && !key.isNullOrBlank()) Payload(url, key, params["email"]) else null
    }
}

/** Renders a QR code for the given content using ZXing. */
@Composable
fun QrImage(content: String, modifier: Modifier = Modifier, sizeDp: Dp = 240.dp) {
    val px = with(LocalDensity.current) { sizeDp.roundToPx() }.coerceAtLeast(256)
    val bitmap = remember(content, px) { generateQrBitmap(content, px) }
    Image(bitmap.asImageBitmap(), contentDescription = "Setup QR code", modifier = modifier.size(sizeDp))
}

private fun generateQrBitmap(content: String, size: Int): Bitmap {
    val hints = mapOf(
        EncodeHintType.ERROR_CORRECTION to ErrorCorrectionLevel.M,
        EncodeHintType.MARGIN to 1,
    )
    val matrix = QRCodeWriter().encode(content, BarcodeFormat.QR_CODE, size, size, hints)
    val bmp = Bitmap.createBitmap(size, size, Bitmap.Config.ARGB_8888)
    for (x in 0 until size) {
        for (y in 0 until size) {
            bmp.setPixel(x, y, if (matrix[x, y]) AndroidColor.BLACK else AndroidColor.WHITE)
        }
    }
    return bmp
}
