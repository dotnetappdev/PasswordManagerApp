package com.vaultguard.app.ui.browse

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.UploadFile
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.ListItem
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp

private val PROVIDERS = listOf(
    "1Password", "Bitwarden", "LastPass", "Dashlane", "NordPass", "Keeper",
    "Chrome", "Edge", "Firefox", "Safari", "KeePass", "Enpass", "RoboForm", "Apple Passwords",
)

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ImportScreen(onBack: () -> Unit) {
    Scaffold(topBar = {
        TopAppBar(
            title = { Text("Import") },
            navigationIcon = { IconButton(onClick = onBack) { Icon(Icons.AutoMirrored.Filled.ArrowBack, "Back") } },
        )
    }) { padding ->
        Column(Modifier.fillMaxSize().padding(padding)) {
            Text(
                "VaultGuard can import from all major password managers. Export a CSV/1PUX from the app " +
                    "below, then import it on the VaultGuard desktop or web app to sync here.",
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
                modifier = Modifier.padding(16.dp),
            )
            LazyColumn(Modifier.fillMaxSize()) {
                items(PROVIDERS) { name ->
                    ListItem(
                        leadingContent = { Icon(Icons.Filled.UploadFile, null, tint = MaterialTheme.colorScheme.primary) },
                        headlineContent = { Text(name) },
                        supportingContent = { Text("CSV / export file") },
                    )
                    HorizontalDivider()
                }
            }
        }
    }
}
