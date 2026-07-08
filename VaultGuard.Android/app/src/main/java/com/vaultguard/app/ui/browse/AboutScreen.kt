package com.vaultguard.app.ui.browse

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.Security
import androidx.compose.material3.Card
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun AboutScreen(onBack: () -> Unit) {
    Scaffold(topBar = {
        TopAppBar(
            title = { Text("About") },
            navigationIcon = { IconButton(onClick = onBack) { Icon(Icons.AutoMirrored.Filled.ArrowBack, "Back") } },
        )
    }) { padding ->
        Column(
            Modifier.fillMaxSize().padding(padding).padding(24.dp),
            verticalArrangement = Arrangement.spacedBy(16.dp),
            horizontalAlignment = Alignment.CenterHorizontally,
        ) {
            Icon(Icons.Filled.Security, null, Modifier.size(72.dp), tint = MaterialTheme.colorScheme.primary)
            Text("VaultGuard", style = MaterialTheme.typography.headlineMedium, fontWeight = FontWeight.Bold)
            Text("for Android · Version 1.0.0", color = MaterialTheme.colorScheme.onSurfaceVariant)
            Card {
                Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
                    Text("A native, encrypted companion to the VaultGuard desktop (WPF) and web apps.")
                    Text("• API or on-device SQLite modes", style = MaterialTheme.typography.bodySmall)
                    Text("• AES-256-GCM encryption, PBKDF2 (600k)", style = MaterialTheme.typography.bodySmall)
                    Text("• TOTP, QR scanning, password generator", style = MaterialTheme.typography.bodySmall)
                    Text("• Feature parity with the WPF app", style = MaterialTheme.typography.bodySmall)
                }
            }
        }
    }
}
