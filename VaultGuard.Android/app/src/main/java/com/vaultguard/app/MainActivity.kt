package com.vaultguard.app

import android.os.Bundle
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.SnackbarHost
import androidx.compose.material3.SnackbarHostState
import androidx.compose.material3.Surface
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.unit.Density
import androidx.fragment.app.FragmentActivity
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.vaultguard.app.ui.common.Toaster
import com.vaultguard.app.ui.navigation.VaultGuardNavGraph
import com.vaultguard.app.ui.theme.ThemeViewModel
import com.vaultguard.app.ui.theme.VaultGuardTheme
import dagger.hilt.android.AndroidEntryPoint
import javax.inject.Inject

@AndroidEntryPoint
class MainActivity : FragmentActivity() {

    @Inject lateinit var toaster: Toaster

    override fun onCreate(savedInstanceState: Bundle?) {
        enableEdgeToEdge()
        super.onCreate(savedInstanceState)
        setContent {
            val themeViewModel: ThemeViewModel = hiltViewModel()
            val settings by themeViewModel.settings.collectAsStateWithLifecycle()
            val snackbarHostState = remember { SnackbarHostState() }

            LaunchedEffect(Unit) {
                toaster.messages.collect { snackbarHostState.showSnackbar(it) }
            }

            VaultGuardTheme(appTheme = settings.theme, dynamicColor = settings.dynamicColor, accentArgb = settings.accentArgb, fontChoice = settings.fontChoice) {
                val base = LocalDensity.current
                val fontScale = base.fontScale * settings.uiZoom * (settings.fontSizePt / 14f)
                CompositionLocalProvider(
                    LocalDensity provides Density(density = base.density * settings.uiZoom, fontScale = fontScale),
                ) {
                    Surface(modifier = Modifier.fillMaxSize(), color = MaterialTheme.colorScheme.background) {
                        Box(Modifier.fillMaxSize()) {
                            VaultGuardNavGraph()
                            // App-wide number-matching approval prompt (GitHub-style).
                            com.vaultguard.app.ui.approval.ApprovalHost()
                            SnackbarHost(
                                hostState = snackbarHostState,
                                modifier = Modifier.align(Alignment.BottomCenter),
                            )
                        }
                    }
                }
            }
        }
    }
}
