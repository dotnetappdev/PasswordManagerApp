package com.vaultguard.app.ui.home

import androidx.compose.foundation.ExperimentalFoundationApi
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.AccountCircle
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Archive
import androidx.compose.material.icons.filled.Category
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material.icons.filled.Folder
import androidx.compose.material.icons.filled.Info
import androidx.compose.material.icons.filled.Inventory2
import androidx.compose.material.icons.filled.Menu
import androidx.compose.material.icons.filled.Search
import androidx.compose.material.icons.filled.Security
import androidx.compose.material.icons.filled.Settings
import androidx.compose.material.icons.filled.Star
import androidx.compose.material.icons.filled.UploadFile
import androidx.compose.material3.Badge
import androidx.compose.material3.DrawerValue
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.ExtendedFloatingActionButton
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.ListItem
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.ModalDrawerSheet
import androidx.compose.material3.ModalNavigationDrawer
import androidx.compose.material3.NavigationDrawerItem
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TextField
import androidx.compose.material3.TextFieldDefaults
import androidx.compose.material3.TopAppBar
import androidx.compose.material3.rememberDrawerState
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.vaultguard.app.data.model.ItemType
import com.vaultguard.app.ui.common.VaultItemRow
import com.vaultguard.app.ui.common.iconFor
import kotlinx.coroutines.launch

@OptIn(ExperimentalMaterial3Api::class, ExperimentalFoundationApi::class)
@Composable
fun HomeScreen(
    onOpenItem: (Int) -> Unit,
    onAddItem: () -> Unit,
    onOpenSettings: () -> Unit,
    onOpenProfile: () -> Unit,
    onOpenVaults: () -> Unit,
    onOpenCategories: () -> Unit,
    onOpenSecurity: () -> Unit,
    onOpenImport: () -> Unit,
    onOpenAbout: () -> Unit,
    viewModel: HomeViewModel = hiltViewModel(),
) {
    val state by viewModel.state.collectAsStateWithLifecycle()
    val drawerState = rememberDrawerState(DrawerValue.Closed)
    val scope = rememberCoroutineScope()
    fun close() = scope.launch { drawerState.close() }

    ModalNavigationDrawer(
        drawerState = drawerState,
        drawerContent = {
            ModalDrawerSheet {
                Column(Modifier.verticalScroll(rememberScrollState()).padding(bottom = 24.dp)) {
                    ListItem(
                        modifier = Modifier.clickable { close(); onOpenProfile() },
                        leadingContent = { Icon(Icons.Filled.AccountCircle, null) },
                        headlineContent = { Text(state.user?.email ?: "My Account") },
                        supportingContent = { Text("Personal Vault") },
                    )
                    HorizontalDivider()

                    DrawerLabel("Library")
                    DrawerRow("All Items", Icons.Filled.Inventory2, state.section == VaultSection.AllItems,
                        state.count(VaultSection.AllItems).takeIf { it > 0 }) { viewModel.setSection(VaultSection.AllItems); close() }
                    DrawerRow("Favourites", Icons.Filled.Star, state.section == VaultSection.Favorites,
                        state.count(VaultSection.Favorites).takeIf { it > 0 }) { viewModel.setSection(VaultSection.Favorites); close() }

                    DrawerLabel("Categories")
                    VaultSection.categories.forEach { cat ->
                        DrawerRow(cat.label, iconFor(cat.type), state.section == cat,
                            state.count(cat).takeIf { it > 0 }) { viewModel.setSection(cat); close() }
                    }

                    DrawerLabel("Security")
                    DrawerRow("Security Dashboard", Icons.Filled.Security, false) { close(); onOpenSecurity() }

                    DrawerLabel("Manage")
                    DrawerRow("Manage Categories", Icons.Filled.Category, false) { close(); onOpenCategories() }
                    DrawerRow("Vaults", Icons.Filled.Folder, false) { close(); onOpenVaults() }
                    DrawerRow("Import", Icons.Filled.UploadFile, false) { close(); onOpenImport() }

                    DrawerLabel("More")
                    DrawerRow("Archive", Icons.Filled.Archive, state.section == VaultSection.Archive,
                        state.count(VaultSection.Archive).takeIf { it > 0 }) { viewModel.setSection(VaultSection.Archive); close() }
                    DrawerRow("Recently Deleted", Icons.Filled.Delete, state.section == VaultSection.RecentlyDeleted,
                        state.count(VaultSection.RecentlyDeleted).takeIf { it > 0 }) { viewModel.setSection(VaultSection.RecentlyDeleted); close() }
                    DrawerRow("Settings", Icons.Filled.Settings, false) { close(); onOpenSettings() }
                    DrawerRow("About", Icons.Filled.Info, false) { close(); onOpenAbout() }
                }
            }
        },
    ) {
        androidx.compose.material3.Scaffold(
            topBar = {
                TopAppBar(
                    title = { Text(state.section.title, fontWeight = FontWeight.SemiBold) },
                    navigationIcon = {
                        IconButton(onClick = { scope.launch { drawerState.open() } }) {
                            Icon(Icons.Filled.Menu, contentDescription = "Menu")
                        }
                    },
                    actions = { IconButton(onClick = onOpenSettings) { Icon(Icons.Filled.Settings, "Settings") } },
                )
            },
            floatingActionButton = {
                ExtendedFloatingActionButton(
                    onClick = onAddItem,
                    icon = { Icon(Icons.Filled.Add, null) },
                    text = { Text("New Item") },
                )
            },
        ) { padding ->
            Column(Modifier.fillMaxSize().padding(padding)) {
                SearchField(state.query, viewModel::setQuery)

                when {
                    state.error != null -> CenterText(state.error!!, MaterialTheme.colorScheme.error)
                    state.visible.isEmpty() && !state.loading ->
                        CenterText("Nothing here yet.", MaterialTheme.colorScheme.onSurfaceVariant)
                    else -> {
                        val grouped = state.visible.sortedBy { it.title.lowercase() }
                            .groupBy { it.title.firstOrNull()?.uppercaseChar()?.takeIf { c -> c.isLetter() } ?: '#' }
                        LazyColumn(Modifier.fillMaxSize()) {
                            grouped.forEach { (letter, itemsForLetter) ->
                                stickyHeader { SectionHeader(letter.toString()) }
                                items(itemsForLetter, key = { it.id }) { item ->
                                    VaultItemRow(
                                        item = item,
                                        onClick = { onOpenItem(item.id) },
                                        onToggleFavorite = { viewModel.toggleFavorite(item.id) },
                                    )
                                    HorizontalDivider(Modifier.padding(start = 72.dp), color = MaterialTheme.colorScheme.outlineVariant)
                                }
                            }
                            item { Spacer(Modifier.height(88.dp)) }
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun SearchField(query: String, onChange: (String) -> Unit) {
    TextField(
        value = query,
        onValueChange = onChange,
        placeholder = { Text("Search") },
        leadingIcon = { Icon(Icons.Filled.Search, null) },
        singleLine = true,
        shape = RoundedCornerShape(14.dp),
        colors = TextFieldDefaults.colors(
            focusedContainerColor = MaterialTheme.colorScheme.surfaceVariant,
            unfocusedContainerColor = MaterialTheme.colorScheme.surfaceVariant,
            focusedIndicatorColor = Color.Transparent,
            unfocusedIndicatorColor = Color.Transparent,
            disabledIndicatorColor = Color.Transparent,
        ),
        modifier = Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 8.dp),
    )
}

@Composable
private fun SectionHeader(text: String) {
    Surface(color = MaterialTheme.colorScheme.background, modifier = Modifier.fillMaxWidth()) {
        Text(
            text,
            style = MaterialTheme.typography.labelMedium,
            color = MaterialTheme.colorScheme.onSurfaceVariant,
            fontWeight = FontWeight.Bold,
            modifier = Modifier.padding(start = 16.dp, top = 12.dp, bottom = 4.dp),
        )
    }
}

@Composable
private fun CenterText(text: String, color: Color) {
    Box(Modifier.fillMaxSize(), Alignment.Center) {
        Text(text, color = color, modifier = Modifier.padding(24.dp))
    }
}

@Composable
private fun DrawerLabel(text: String) {
    Spacer(Modifier.height(8.dp))
    Text(
        text.uppercase(),
        style = MaterialTheme.typography.labelSmall,
        color = MaterialTheme.colorScheme.primary,
        modifier = Modifier.padding(start = 28.dp, top = 8.dp, bottom = 4.dp),
    )
}

@Composable
private fun DrawerRow(label: String, icon: ImageVector, selected: Boolean, badge: Int? = null, onClick: () -> Unit) {
    NavigationDrawerItem(
        label = { Text(label) },
        selected = selected,
        icon = { Icon(icon, null) },
        badge = badge?.let { { Badge { Text("$it") } } },
        onClick = onClick,
        modifier = Modifier.padding(horizontal = 12.dp),
    )
}
