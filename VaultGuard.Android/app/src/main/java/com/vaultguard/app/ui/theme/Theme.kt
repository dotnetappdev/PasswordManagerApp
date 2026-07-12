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
import androidx.compose.ui.graphics.compositeOver
import androidx.compose.ui.graphics.luminance
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontFamily
import com.vaultguard.app.config.AppTheme
import com.vaultguard.app.config.FontChoice

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

// High-contrast — Windows "High Contrast Black": pure black surfaces, white body text and bright
// yellow (#FFFF00) for every interactive/accent element and border.
private val HcYellow = Color(0xFFFFFF00)
private val HighContrastColors = darkColorScheme(
    primary = HcYellow,
    onPrimary = Color.Black,
    primaryContainer = Color.Black,
    onPrimaryContainer = HcYellow,
    secondary = HcYellow,
    onSecondary = Color.Black,
    secondaryContainer = Color.Black,
    onSecondaryContainer = HcYellow,
    tertiary = HcYellow,
    onTertiary = Color.Black,
    background = Color.Black,
    onBackground = Color.White,
    surface = Color.Black,
    onSurface = Color.White,
    surfaceVariant = Color.Black,
    onSurfaceVariant = HcYellow,
    outline = HcYellow,
    outlineVariant = HcYellow,
    error = Color(0xFFFF8A80),
    onError = Color.Black,
    inverseSurface = Color.White,
    inverseOnSurface = Color.Black,
)

/** Pick readable on-accent text (black or white) for an arbitrary custom accent. */
private fun onColorFor(accent: Color): Color =
    if (accent.luminance() > 0.5f) Color.Black else Color.White

/** Rebuild the default Material 3 type scale with a chosen font family (accessibility/personalisation). */
private fun typographyFor(choice: FontChoice): Typography {
    val family = when (choice) {
        FontChoice.SYSTEM -> FontFamily.Default
        FontChoice.SANS_SERIF -> FontFamily.SansSerif
        FontChoice.SERIF -> FontFamily.Serif
        FontChoice.MONOSPACE -> FontFamily.Monospace
    }
    if (choice == FontChoice.SYSTEM) return Typography()
    val b = Typography()
    return b.copy(
        displayLarge = b.displayLarge.copy(fontFamily = family),
        displayMedium = b.displayMedium.copy(fontFamily = family),
        displaySmall = b.displaySmall.copy(fontFamily = family),
        headlineLarge = b.headlineLarge.copy(fontFamily = family),
        headlineMedium = b.headlineMedium.copy(fontFamily = family),
        headlineSmall = b.headlineSmall.copy(fontFamily = family),
        titleLarge = b.titleLarge.copy(fontFamily = family),
        titleMedium = b.titleMedium.copy(fontFamily = family),
        titleSmall = b.titleSmall.copy(fontFamily = family),
        bodyLarge = b.bodyLarge.copy(fontFamily = family),
        bodyMedium = b.bodyMedium.copy(fontFamily = family),
        bodySmall = b.bodySmall.copy(fontFamily = family),
        labelLarge = b.labelLarge.copy(fontFamily = family),
        labelMedium = b.labelMedium.copy(fontFamily = family),
        labelSmall = b.labelSmall.copy(fontFamily = family),
    )
}

@Composable
fun VaultGuardTheme(
    appTheme: AppTheme = AppTheme.SYSTEM,
    dynamicColor: Boolean = true,
    accentArgb: Long = 0L,
    fontChoice: FontChoice = FontChoice.SYSTEM,
    content: @Composable () -> Unit,
) {
    val systemDark = isSystemInDarkTheme()
    val darkTheme = when (appTheme) {
        AppTheme.SYSTEM -> systemDark
        AppTheme.LIGHT -> false
        AppTheme.DARK, AppTheme.HIGH_CONTRAST -> true
    }
    val context = LocalContext.current
    // A user-chosen accent overrides Material You (but never the high-contrast theme).
    val hasCustomAccent = accentArgb != 0L && appTheme != AppTheme.HIGH_CONTRAST
    val base = when {
        appTheme == AppTheme.HIGH_CONTRAST -> HighContrastColors
        !hasCustomAccent && dynamicColor && Build.VERSION.SDK_INT >= Build.VERSION_CODES.S ->
            if (darkTheme) dynamicDarkColorScheme(context) else dynamicLightColorScheme(context)
        darkTheme -> DarkColors
        else -> LightColors
    }
    val colorScheme = if (hasCustomAccent) {
        val accent = Color(accentArgb)
        base.copy(
            primary = accent,
            onPrimary = onColorFor(accent),
            primaryContainer = accent.copy(alpha = 0.18f).compositeOver(base.surface),
            onPrimaryContainer = accent,
        )
    } else base
    MaterialTheme(
        colorScheme = colorScheme,
        typography = typographyFor(fontChoice),
        content = content,
    )
}
