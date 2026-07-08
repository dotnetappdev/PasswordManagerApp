package com.vaultguard.app.ui.navigation

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.vaultguard.app.config.ConfigStore
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.map
import kotlinx.coroutines.flow.stateIn
import javax.inject.Inject

enum class StartState { LOADING, SETUP, UNLOCK }

@HiltViewModel
class RootViewModel @Inject constructor(
    configStore: ConfigStore,
) : ViewModel() {
    val startState = configStore.isConfigured
        .map { if (it) StartState.UNLOCK else StartState.SETUP }
        .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5_000), StartState.LOADING)
}
