package com.vaultguard.app.ui.theme

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.vaultguard.app.config.AppSettings
import com.vaultguard.app.config.SettingsStore
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.stateIn
import javax.inject.Inject

@HiltViewModel
class ThemeViewModel @Inject constructor(
    settingsStore: SettingsStore,
) : ViewModel() {
    val settings = settingsStore.settings
        .stateIn(viewModelScope, SharingStarted.Eagerly, AppSettings())
}
