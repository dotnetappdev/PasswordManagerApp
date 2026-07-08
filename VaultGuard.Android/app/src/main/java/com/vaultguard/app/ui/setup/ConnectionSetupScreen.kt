package com.vaultguard.app.ui.setup

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.Error
import androidx.compose.material3.Button
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.SegmentedButton
import androidx.compose.material3.SegmentedButtonDefaults
import androidx.compose.material3.SingleChoiceSegmentedButtonRow
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.vaultguard.app.config.ConnectionMode

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ConnectionSetupScreen(
    onDone: () -> Unit,
    viewModel: ConnectionSetupViewModel = hiltViewModel(),
) {
    val state by viewModel.state.collectAsStateWithLifecycle()

    LaunchedEffect(state.saved) { if (state.saved) onDone() }

    Scaffold(topBar = { TopAppBar(title = { Text("Connect VaultGuard") }) }) { padding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
                .padding(20.dp)
                .verticalScroll(rememberScrollState()),
            verticalArrangement = Arrangement.spacedBy(16.dp),
        ) {
            Text(
                "Choose how this device connects to your vault.",
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
            )

            SingleChoiceSegmentedButtonRow(Modifier.fillMaxWidth()) {
                SegmentedButton(
                    selected = state.mode == ConnectionMode.API,
                    onClick = { viewModel.setMode(ConnectionMode.API) },
                    shape = SegmentedButtonDefaults.itemShape(0, 2),
                ) { Text("API Server") }
                SegmentedButton(
                    selected = state.mode == ConnectionMode.LOCAL,
                    onClick = { viewModel.setMode(ConnectionMode.LOCAL) },
                    shape = SegmentedButtonDefaults.itemShape(1, 2),
                ) { Text("Local (SQLite)") }
            }

            if (state.mode == ConnectionMode.API) {
                Text(
                    "Enter your API URL and the API key generated on the VaultGuard web app " +
                        "(Settings → API Keys).",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
                OutlinedTextField(
                    value = state.apiUrl,
                    onValueChange = viewModel::setUrl,
                    label = { Text("API URL") },
                    placeholder = { Text("https://your-server:7001") },
                    singleLine = true,
                    keyboardOptions = androidx.compose.foundation.text.KeyboardOptions(keyboardType = KeyboardType.Uri),
                    modifier = Modifier.fillMaxWidth(),
                )
                OutlinedTextField(
                    value = state.apiKey,
                    onValueChange = viewModel::setKey,
                    label = { Text("API Key") },
                    singleLine = true,
                    visualTransformation = PasswordVisualTransformation(),
                    modifier = Modifier.fillMaxWidth(),
                )

                Row(horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                    OutlinedButton(onClick = viewModel::test, enabled = !state.testing) {
                        if (state.testing) {
                            CircularProgressIndicator(Modifier.height(18.dp), strokeWidth = 2.dp)
                            Spacer(Modifier.height(0.dp))
                            Text("  Testing…")
                        } else {
                            Text("Test connection")
                        }
                    }
                }

                state.testMessage?.let { msg ->
                    val ok = state.testSuccess == true
                    Row(verticalAlignment = Alignment.CenterVertically) {
                        Icon(
                            imageVector = if (ok) Icons.Filled.CheckCircle else Icons.Filled.Error,
                            contentDescription = null,
                            tint = if (ok) Color(0xFF2E7D32) else MaterialTheme.colorScheme.error,
                        )
                        Text("  $msg", color = if (ok) Color(0xFF2E7D32) else MaterialTheme.colorScheme.error)
                    }
                }
            } else {
                Text(
                    "Your vault will be stored only on this device in an encrypted SQLite database. " +
                        "You'll set a master password on the next screen.",
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
            }

            Spacer(Modifier.height(8.dp))
            Button(onClick = viewModel::save, modifier = Modifier.fillMaxWidth()) {
                Text("Save & continue")
            }
        }
    }
}
