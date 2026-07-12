package com.vaultguard.app.ui.browse

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.GppBad
import androidx.compose.material.icons.filled.GppGood
import androidx.compose.material.icons.filled.Shield
import androidx.compose.material3.Card
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.vaultguard.app.ui.common.iconFor

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun SecurityDashboardScreen(onBack: () -> Unit, viewModel: SecurityViewModel = hiltViewModel()) {
    val stats by viewModel.stats.collectAsStateWithLifecycle()
    val breach by viewModel.breach.collectAsStateWithLifecycle()

    Scaffold(topBar = {
        TopAppBar(
            title = { Text("Security Center") },
            navigationIcon = { IconButton(onClick = onBack) { Icon(Icons.AutoMirrored.Filled.ArrowBack, "Back") } },
        )
    }) { padding ->
        Column(
            Modifier.fillMaxSize().padding(padding).verticalScroll(rememberScrollState()).padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp),
        ) {
            Row(Modifier.fillMaxWidth(), Arrangement.spacedBy(12.dp)) {
                StatCard("Items", stats.total.toString(), Modifier.weight(1f))
                StatCard("Favourites", stats.favorites.toString(), Modifier.weight(1f))
            }
            Row(Modifier.fillMaxWidth(), Arrangement.spacedBy(12.dp)) {
                StatCard("Archived", stats.archived.toString(), Modifier.weight(1f))
                StatCard("Deleted", stats.deleted.toString(), Modifier.weight(1f))
            }

            BreachCard(breach, onScan = viewModel::scanBreaches)

            Card(Modifier.fillMaxWidth()) {
                Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(10.dp)) {
                    Text("By category", style = MaterialTheme.typography.titleMedium, color = MaterialTheme.colorScheme.primary)
                    HorizontalDivider()
                    if (stats.perType.isEmpty()) {
                        Text("No items yet.", color = MaterialTheme.colorScheme.onSurfaceVariant)
                    } else {
                        stats.perType.entries.sortedByDescending { it.value }.forEach { (type, count) ->
                            Row(Modifier.fillMaxWidth(), Arrangement.SpaceBetween) {
                                Row {
                                    Icon(iconFor(type), null, tint = MaterialTheme.colorScheme.primary)
                                    Text("  ${type.label}", modifier = Modifier.width(160.dp))
                                }
                                Text("$count", fontWeight = FontWeight.SemiBold)
                            }
                        }
                    }
                }
            }
        }
    }
}

/** Breach monitoring card — runs an on-demand Have I Been Pwned scan (k-anonymity) over the vault. */
@Composable
private fun BreachCard(state: BreachScan, onScan: () -> Unit) {
    Card(Modifier.fillMaxWidth()) {
        Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(10.dp)) {
            Row {
                Icon(Icons.Filled.Shield, null, tint = MaterialTheme.colorScheme.primary)
                Text("  Breach check", style = MaterialTheme.typography.titleMedium, color = MaterialTheme.colorScheme.primary)
            }
            HorizontalDivider()
            Text(
                "Checks every stored password against known data breaches. Privacy-preserving — only the first " +
                    "five characters of each password's hash are ever sent (Have I Been Pwned).",
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
            )

            when (state) {
                is BreachScan.Scanning -> Row(verticalAlignment = androidx.compose.ui.Alignment.CenterVertically) {
                    CircularProgressIndicator(Modifier.size(18.dp), strokeWidth = 2.dp)
                    Text("  Checking…", style = MaterialTheme.typography.bodyMedium)
                }
                is BreachScan.Done -> {
                    if (state.compromised.isEmpty()) {
                        Row(verticalAlignment = androidx.compose.ui.Alignment.CenterVertically) {
                            Icon(Icons.Filled.GppGood, null, tint = MaterialTheme.colorScheme.primary)
                            Text("  No passwords found in known breaches.", style = MaterialTheme.typography.bodyMedium)
                        }
                    } else {
                        Text("${state.compromised.size} compromised password(s):",
                            style = MaterialTheme.typography.bodyMedium, fontWeight = FontWeight.SemiBold,
                            color = MaterialTheme.colorScheme.error)
                        state.compromised.forEach { hit ->
                            Row(Modifier.fillMaxWidth(), Arrangement.SpaceBetween, androidx.compose.ui.Alignment.CenterVertically) {
                                Row {
                                    Icon(Icons.Filled.GppBad, null, tint = MaterialTheme.colorScheme.error)
                                    Text("  ${hit.title}", modifier = Modifier.width(180.dp), maxLines = 1)
                                }
                                Text("seen ${hit.timesSeen}×", style = MaterialTheme.typography.labelMedium,
                                    color = MaterialTheme.colorScheme.error)
                            }
                        }
                    }
                    if (state.someFailed) {
                        Text("Some passwords couldn't be checked (offline?). Try again when connected.",
                            style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
                    }
                    OutlinedButton(onClick = onScan) { Text("Re-check") }
                }
                BreachScan.Idle -> OutlinedButton(onClick = onScan) { Text("Check for breaches") }
            }
        }
    }
}

@Composable
private fun StatCard(label: String, value: String, modifier: Modifier = Modifier) {
    Card(modifier) {
        Column(Modifier.padding(18.dp)) {
            Text(value, style = MaterialTheme.typography.headlineMedium, fontWeight = FontWeight.Bold, color = MaterialTheme.colorScheme.primary)
            Text(label, style = MaterialTheme.typography.bodyMedium, color = MaterialTheme.colorScheme.onSurfaceVariant)
        }
    }
}
