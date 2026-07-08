package com.vaultguard.app.ui.theme

import android.os.Build
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Typography
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.dynamicDarkColorScheme
import androidx.compose.material3.dynamicLightColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import com.vaultguard.app.config.AppTheme

/** Brand accent, tuned to feel like the clean blue of the 1Password mobile apps. */
val VgAccent = Color(0xFF0A84FF)
private val VgAccentDark = Color(0xFF4C9AFF)

// Light — soft neutral greys, white cards, calm blue accent (1Password-like).
private val LightColors = lightColorScheme(
    primary = VgAccent,
    onPrimary = Color.White,
    primaryContainer = Color(0xFFE3F0FF),
    onPrimaryContainer = Color(0xFF083A73),
    secondary = Color(0xFF5B6673),
    background = Color(0xFFF2F3F5),
    onBackground = Color(0xFF10141A),
    surface = Color(0xFFFFFFFF),
    onSurface = Color(0xFF10141A),
    surfaceVariant = Color(0xFFE9EBEF),
    onSurfaceVariant = Color(0xFF5C6672),
    outline = Color(0xFFD5D9DF),
    outlineVariant = Color(0xFFE4E7EC),
)

// Dark — deep near-black surfaces with layered cards, brighter accent.
private val DarkColors = darkColorScheme(
    primary = VgAccentDark,
    onPrimary = Color(0xFF04223F),
    primaryContainer = Color(0xFF123A5C),
    onPrimaryContainer = Color(0xFFCDE6FB),
    secondary = Color(0xFF9AA6B2),
    background = Color(0xFF0E1116),
    onBackground = Color(0xFFE6EAF0),
    surface = Color(0xFF171B21),
    onSurface = Color(0xFFE6EAF0),
    surfaceVariant = Color(0xFF232830),
    onSurfaceVariant = Color(0xFFA6B0BC),
    outline = Color(0xFF2C333D),
    outlineVariant = Color(0xFF232830),
)

// High-contrast mirrors the WPF "High Contrast" theme option.
private val HighContrastColors = darkColorScheme(
    primary = Color(0xFF66B2FF),
    onPrimary = Color.Black,
    primaryContainer = Color(0xFF003366),
    onPrimaryContainer = Color.White,
    secondary = Color(0xFFFFFFFF),
    background = Color(0xFF000000),
    surface = Color(0xFF000000),
    surfaceVariant = Color(0xFF0A0A0A),
    onSurface = Color(0xFFFFFFFF),
    onSurfaceVariant = Color(0xFFECECEC),
    onBackground = Color(0xFFFFFFFF),
    outline = Color(0xFF7A7A7A),
)

@Composable
fun VaultGuardTheme(
    appTheme: AppTheme = AppTheme.SYSTEM,
    dynamicColor: Boolean = true,
    content: @Composable () -> Unit,
) {
    val systemDark = isSystemInDarkTheme()
    val darkTheme = when (appTheme) {
        AppTheme.SYSTEM -> systemDark
        AppTheme.LIGHT -> false
        AppTheme.DARK, AppTheme.HIGH_CONTRAST -> true
    }
    val context = LocalContext.current
    val colorScheme = when {
        appTheme == AppTheme.HIGH_CONTRAST -> HighContrastColors
        dynamicColor && Build.VERSION.SDK_INT >= Build.VERSION_CODES.S ->
            if (darkTheme) dynamicDarkColorScheme(context) else dynamicLightColorScheme(context)
        darkTheme -> DarkColors
        else -> LightColors
    }
    MaterialTheme(
        colorScheme = colorScheme,
        typography = Typography(),
        content = content,
    )
}
