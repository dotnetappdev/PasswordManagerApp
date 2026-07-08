package com.vaultguard.app

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.unit.Density
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.vaultguard.app.ui.navigation.VaultGuardNavGraph
import com.vaultguard.app.ui.theme.ThemeViewModel
import com.vaultguard.app.ui.theme.VaultGuardTheme
import dagger.hilt.android.AndroidEntryPoint

@AndroidEntryPoint
class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        enableEdgeToEdge()
        super.onCreate(savedInstanceState)
        setContent {
            val themeViewModel: ThemeViewModel = hiltViewModel()
            val settings by themeViewModel.settings.collectAsStateWithLifecycle()

            VaultGuardTheme(appTheme = settings.theme, dynamicColor = settings.dynamicColor) {
                // Apply the accessibility "UI zoom" + base font size to the whole app, like the WPF zoom.
                val base = LocalDensity.current
                val fontScale = base.fontScale * settings.uiZoom * (settings.fontSizePt / 14f)
                CompositionLocalProvider(
                    LocalDensity provides Density(density = base.density * settings.uiZoom, fontScale = fontScale),
                ) {
                    Surface(
                        modifier = Modifier.fillMaxSize(),
                        color = MaterialTheme.colorScheme.background,
                    ) {
                        VaultGuardNavGraph()
                    }
                }
            }
        }
    }
}
