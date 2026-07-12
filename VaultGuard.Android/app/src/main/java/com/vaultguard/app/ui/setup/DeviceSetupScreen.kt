package com.vaultguard.app.ui.setup

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.produceState
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.ViewModel
import com.vaultguard.app.config.ConfigStore
import com.vaultguard.app.config.ConnectionMode
import com.vaultguard.app.config.SecureStore
import com.vaultguard.app.data.repo.SessionManager
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.first
import javax.inject.Inject

@HiltViewModel
class DeviceSetupViewModel @Inject constructor(
    private val configStore: ConfigStore,
    private val secureStore: SecureStore,
    private val session: SessionManager,
) : ViewModel() {
    suspend fun payload(): String? {
        val cfg = configStore.config.first()
        if (cfg.mode != ConnectionMode.API || cfg.apiBaseUrl.isBlank()) return null
        val key = secureStore.apiKey ?: return null
        return DeviceSetup.encode(cfg.apiBaseUrl, key, session.user.value?.email)
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun DeviceSetupScreen(onBack: () -> Unit, viewModel: DeviceSetupViewModel = hiltViewModel()) {
    val payload by produceState<String?>(initialValue = null) { value = viewModel.payload() }

    Scaffold(
        topBar = {
            androidx.compose.material3.TopAppBar(
                title = { Text("Set Up Another Device") },
                navigationIcon = { IconButton(onClick = onBack) { Icon(Icons.AutoMirrored.Filled.ArrowBack, "Back") } },
            )
        },
    ) { padding ->
        Column(
            Modifier.fillMaxSize().padding(padding).verticalScroll(rememberScrollState()).padding(24.dp),
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.spacedBy(20.dp),
        ) {
            val p = payload
            if (p == null) {
                Text("Sign in with an API server first — device setup shares this device's server URL and key.",
                    style = MaterialTheme.typography.bodyMedium, color = MaterialTheme.colorScheme.onSurfaceVariant)
            } else {
                Surface(color = Color.White, shape = RoundedCornerShape(20.dp)) {
                    Box(Modifier.padding(16.dp)) { QrImage(p, sizeDp = 240.dp) }
                }
                Text("Use QR code", style = MaterialTheme.typography.titleLarge, fontWeight = FontWeight.Bold)
                Step(1, "Open VaultGuard on the other device where you're not signed in.")
                Step(2, "On the Connect screen, tap “Scan to set up”.")
                Step(3, "Point it at this code — it configures the server and signs in.")
                Text("This code contains your server URL and API key — only scan it on a device you trust.",
                    style = MaterialTheme.typography.labelSmall, color = MaterialTheme.colorScheme.error,
                    modifier = Modifier.padding(top = 8.dp))
            }
        }
    }
}

@Composable
private fun Step(n: Int, text: String) {
    Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(12.dp)) {
        Box(
            Modifier.size(24.dp).background(MaterialTheme.colorScheme.primary, CircleShape),
            contentAlignment = Alignment.Center,
        ) { Text("$n", color = MaterialTheme.colorScheme.onPrimary, style = MaterialTheme.typography.labelMedium) }
        Text(text, style = MaterialTheme.typography.bodyMedium)
    }
}
