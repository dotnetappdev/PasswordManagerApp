package com.vaultguard.app.config

import android.content.Context
import androidx.datastore.preferences.core.booleanPreferencesKey
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.floatPreferencesKey
import androidx.datastore.preferences.core.intPreferencesKey
import androidx.datastore.preferences.core.stringPreferencesKey
import androidx.datastore.preferences.preferencesDataStore
import dagger.hilt.android.qualifiers.ApplicationContext
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.map
import javax.inject.Inject
import javax.inject.Singleton

private val Context.settingsDataStore by preferencesDataStore(name = "vaultguard_settings")

enum class AppTheme { SYSTEM, LIGHT, DARK, HIGH_CONTRAST }
enum class DefaultView { DASHBOARD, ALL_ITEMS, FAVORITES }

/**
 * Full mirror of the WPF Settings surface (Appearance / Accessibility / Security / Storage /
 * Backup-Import-Export / Maintenance). Persisted with DataStore so choices survive restarts.
 */
data class AppSettings(
    // Appearance
    val theme: AppTheme = AppTheme.SYSTEM,
    val dynamicColor: Boolean = true,
    val defaultView: DefaultView = DefaultView.ALL_ITEMS,
    val showItemCount: Boolean = true,
    val animateTransitions: Boolean = true,
    // Accessibility
    val uiZoom: Float = 1.0f,
    val fontSizePt: Int = 14,
    val scaleMenu: Float = 1.0f,
    val scaleQuickActions: Float = 1.0f,
    val scaleDetails: Float = 1.0f,
    val scaleDialogs: Float = 1.0f,
    val scaleGlobal: Float = 1.0f,
    val scaleCardIcons: Float = 1.0f,
    val reduceMotion: Boolean = false,
    val highContrast: Boolean = false,
    // Security
    val requirePasscodeOnLaunch: Boolean = true,
    val biometricUnlock: Boolean = false,
    val autoLockMinutes: Int = 5,
    val clipboardClearSeconds: Int = 30,
    val confirmOnDelete: Boolean = true,
    // Password generator defaults
    val pwLength: Int = 20,
    val pwUpper: Boolean = true,
    val pwLower: Boolean = true,
    val pwDigits: Boolean = true,
    val pwSymbols: Boolean = true,
    val pwAvoidAmbiguous: Boolean = false,
)

@Singleton
class SettingsStore @Inject constructor(
    @ApplicationContext private val context: Context,
) {
    private object K {
        val theme = stringPreferencesKey("theme")
        val dynamicColor = booleanPreferencesKey("dynamic_color")
        val defaultView = stringPreferencesKey("default_view")
        val showItemCount = booleanPreferencesKey("show_item_count")
        val animateTransitions = booleanPreferencesKey("animate_transitions")
        val uiZoom = floatPreferencesKey("ui_zoom")
        val fontSizePt = intPreferencesKey("font_size_pt")
        val scaleMenu = floatPreferencesKey("scale_menu")
        val scaleQuickActions = floatPreferencesKey("scale_quick_actions")
        val scaleDetails = floatPreferencesKey("scale_details")
        val scaleDialogs = floatPreferencesKey("scale_dialogs")
        val scaleGlobal = floatPreferencesKey("scale_global")
        val scaleCardIcons = floatPreferencesKey("scale_card_icons")
        val reduceMotion = booleanPreferencesKey("reduce_motion")
        val highContrast = booleanPreferencesKey("high_contrast")
        val requirePasscode = booleanPreferencesKey("require_passcode")
        val biometricUnlock = booleanPreferencesKey("biometric_unlock")
        val autoLockMinutes = intPreferencesKey("auto_lock_minutes")
        val clipboardClearSeconds = intPreferencesKey("clipboard_clear_seconds")
        val confirmOnDelete = booleanPreferencesKey("confirm_on_delete")
        val pwLength = intPreferencesKey("pw_length")
        val pwUpper = booleanPreferencesKey("pw_upper")
        val pwLower = booleanPreferencesKey("pw_lower")
        val pwDigits = booleanPreferencesKey("pw_digits")
        val pwSymbols = booleanPreferencesKey("pw_symbols")
        val pwAvoidAmbiguous = booleanPreferencesKey("pw_avoid_ambiguous")
    }

    val settings: Flow<AppSettings> = context.settingsDataStore.data.map { p ->
        val d = AppSettings()
        AppSettings(
            theme = p[K.theme]?.let { runCatching { AppTheme.valueOf(it) }.getOrNull() } ?: d.theme,
            dynamicColor = p[K.dynamicColor] ?: d.dynamicColor,
            defaultView = p[K.defaultView]?.let { runCatching { DefaultView.valueOf(it) }.getOrNull() } ?: d.defaultView,
            showItemCount = p[K.showItemCount] ?: d.showItemCount,
            animateTransitions = p[K.animateTransitions] ?: d.animateTransitions,
            uiZoom = p[K.uiZoom] ?: d.uiZoom,
            fontSizePt = p[K.fontSizePt] ?: d.fontSizePt,
            scaleMenu = p[K.scaleMenu] ?: d.scaleMenu,
            scaleQuickActions = p[K.scaleQuickActions] ?: d.scaleQuickActions,
            scaleDetails = p[K.scaleDetails] ?: d.scaleDetails,
            scaleDialogs = p[K.scaleDialogs] ?: d.scaleDialogs,
            scaleGlobal = p[K.scaleGlobal] ?: d.scaleGlobal,
            scaleCardIcons = p[K.scaleCardIcons] ?: d.scaleCardIcons,
            reduceMotion = p[K.reduceMotion] ?: d.reduceMotion,
            highContrast = p[K.highContrast] ?: d.highContrast,
            requirePasscodeOnLaunch = p[K.requirePasscode] ?: d.requirePasscodeOnLaunch,
            biometricUnlock = p[K.biometricUnlock] ?: d.biometricUnlock,
            autoLockMinutes = p[K.autoLockMinutes] ?: d.autoLockMinutes,
            clipboardClearSeconds = p[K.clipboardClearSeconds] ?: d.clipboardClearSeconds,
            confirmOnDelete = p[K.confirmOnDelete] ?: d.confirmOnDelete,
            pwLength = p[K.pwLength] ?: d.pwLength,
            pwUpper = p[K.pwUpper] ?: d.pwUpper,
            pwLower = p[K.pwLower] ?: d.pwLower,
            pwDigits = p[K.pwDigits] ?: d.pwDigits,
            pwSymbols = p[K.pwSymbols] ?: d.pwSymbols,
            pwAvoidAmbiguous = p[K.pwAvoidAmbiguous] ?: d.pwAvoidAmbiguous,
        )
    }

    suspend fun update(transform: (AppSettings) -> AppSettings) {
        // Read current, apply, write each field.
        context.settingsDataStore.edit { p ->
            val current = readSnapshot(p)
            val next = transform(current)
            p[K.theme] = next.theme.name
            p[K.dynamicColor] = next.dynamicColor
            p[K.defaultView] = next.defaultView.name
            p[K.showItemCount] = next.showItemCount
            p[K.animateTransitions] = next.animateTransitions
            p[K.uiZoom] = next.uiZoom
            p[K.fontSizePt] = next.fontSizePt
            p[K.scaleMenu] = next.scaleMenu
            p[K.scaleQuickActions] = next.scaleQuickActions
            p[K.scaleDetails] = next.scaleDetails
            p[K.scaleDialogs] = next.scaleDialogs
            p[K.scaleGlobal] = next.scaleGlobal
            p[K.scaleCardIcons] = next.scaleCardIcons
            p[K.reduceMotion] = next.reduceMotion
            p[K.highContrast] = next.highContrast
            p[K.requirePasscode] = next.requirePasscodeOnLaunch
            p[K.biometricUnlock] = next.biometricUnlock
            p[K.autoLockMinutes] = next.autoLockMinutes
            p[K.clipboardClearSeconds] = next.clipboardClearSeconds
            p[K.confirmOnDelete] = next.confirmOnDelete
            p[K.pwLength] = next.pwLength
            p[K.pwUpper] = next.pwUpper
            p[K.pwLower] = next.pwLower
            p[K.pwDigits] = next.pwDigits
            p[K.pwSymbols] = next.pwSymbols
            p[K.pwAvoidAmbiguous] = next.pwAvoidAmbiguous
        }
    }

    private fun readSnapshot(p: androidx.datastore.preferences.core.Preferences): AppSettings {
        val d = AppSettings()
        return AppSettings(
            theme = p[K.theme]?.let { runCatching { AppTheme.valueOf(it) }.getOrNull() } ?: d.theme,
            dynamicColor = p[K.dynamicColor] ?: d.dynamicColor,
            defaultView = p[K.defaultView]?.let { runCatching { DefaultView.valueOf(it) }.getOrNull() } ?: d.defaultView,
            showItemCount = p[K.showItemCount] ?: d.showItemCount,
            animateTransitions = p[K.animateTransitions] ?: d.animateTransitions,
            uiZoom = p[K.uiZoom] ?: d.uiZoom,
            fontSizePt = p[K.fontSizePt] ?: d.fontSizePt,
            scaleMenu = p[K.scaleMenu] ?: d.scaleMenu,
            scaleQuickActions = p[K.scaleQuickActions] ?: d.scaleQuickActions,
            scaleDetails = p[K.scaleDetails] ?: d.scaleDetails,
            scaleDialogs = p[K.scaleDialogs] ?: d.scaleDialogs,
            scaleGlobal = p[K.scaleGlobal] ?: d.scaleGlobal,
            scaleCardIcons = p[K.scaleCardIcons] ?: d.scaleCardIcons,
            reduceMotion = p[K.reduceMotion] ?: d.reduceMotion,
            highContrast = p[K.highContrast] ?: d.highContrast,
            requirePasscodeOnLaunch = p[K.requirePasscode] ?: d.requirePasscodeOnLaunch,
            biometricUnlock = p[K.biometricUnlock] ?: d.biometricUnlock,
            autoLockMinutes = p[K.autoLockMinutes] ?: d.autoLockMinutes,
            clipboardClearSeconds = p[K.clipboardClearSeconds] ?: d.clipboardClearSeconds,
            confirmOnDelete = p[K.confirmOnDelete] ?: d.confirmOnDelete,
            pwLength = p[K.pwLength] ?: d.pwLength,
            pwUpper = p[K.pwUpper] ?: d.pwUpper,
            pwLower = p[K.pwLower] ?: d.pwLower,
            pwDigits = p[K.pwDigits] ?: d.pwDigits,
            pwSymbols = p[K.pwSymbols] ?: d.pwSymbols,
            pwAvoidAmbiguous = p[K.pwAvoidAmbiguous] ?: d.pwAvoidAmbiguous,
        )
    }
}
