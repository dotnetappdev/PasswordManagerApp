package com.vaultguard.app.ui.home

import androidx.compose.foundation.ExperimentalFoundationApi
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.lazy.rememberLazyListState
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.AccountCircle
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Apps
import androidx.compose.material.icons.filled.Archive
import androidx.compose.material.icons.filled.Category
import androidx.compose.material.icons.filled.Check
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material.icons.filled.Fingerprint
import androidx.compose.material.icons.filled.Folder
import androidx.compose.material.icons.filled.Home
import androidx.compose.material.icons.filled.Info
import androidx.compose.material.icons.filled.Inventory2
import androidx.compose.material.icons.filled.Menu
import androidx.compose.material.icons.filled.QrCodeScanner
import androidx.compose.material.icons.filled.Search
import androidx.compose.material.icons.filled.Security
import androidx.compose.material.icons.filled.Settings
import androidx.compose.material.icons.filled.Star
import androidx.compose.material.icons.filled.SwitchAccount
import androidx.compose.material.icons.filled.UploadFile
import androidx.compose.material3.Badge
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExposedDropdownMenuBox
import androidx.compose.material3.ExposedDropdownMenuDefaults
import androidx.compose.material3.OutlinedTextField
import androidx.compose.ui.draw.clip
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
import androidx.compose.material3.NavigationBar
import androidx.compose.material3.NavigationBarItem
import androidx.compose.material3.NavigationDrawerItem
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TextField
import androidx.compose.material3.TextFieldDefaults
import androidx.compose.material3.TopAppBar
import androidx.compose.material3.rememberDrawerState
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.vaultguard.app.data.model.CategoryDto
import com.vaultguard.app.data.model.ItemType
import com.vaultguard.app.ui.common.VaultItemRow
import com.vaultguard.app.ui.common.iconFor
import kotlinx.coroutines.launch

@OptIn(ExperimentalMaterial3Api::class, ExperimentalFoundationApi::class)
@Composable
fun HomeScreen(
    onOpenItem: (Int) -> Unit,
    onAddItem: () -> Unit,
    onScanAdd: () -> Unit,
    onOpenSettings: () -> Unit,
    onOpenProfile: () -> Unit,
    onSwitchAccount: () -> Unit,
    onOpenVaults: () -> Unit,
    onOpenCategories: () -> Unit,
    onOpenSecurity: () -> Unit,
    onOpenPasskeys: () -> Unit,
    onOpenImport: () -> Unit,
    onOpenAbout: () -> Unit,
    viewModel: HomeViewModel = hiltViewModel(),
) {
    val state by viewModel.state.collectAsStateWithLifecycle()
    val drawerState = rememberDrawerState(DrawerValue.Closed)
    val scope = rememberCoroutineScope()
    var tab by remember { mutableStateOf(HomeTab.Home) }
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
                        supportingContent = { Text(state.vaultLabel) },
                        trailingContent = {
                            IconButton(onClick = { viewModel.lock(); close(); onSwitchAccount() }) {
                                Icon(Icons.Filled.SwitchAccount, "Switch account")
                            }
                        },
                    )
                    DrawerRow("Switch account", Icons.Filled.SwitchAccount, false) {
                        viewModel.lock(); close(); onSwitchAccount()
                    }
                    HorizontalDivider()

                    // Vault switcher — pick which vault the list is scoped to.
                    if (state.vaults.isNotEmpty()) {
                        DrawerLabel("Vaults")
                        DrawerRow("All Vaults", Icons.Filled.Folder, state.currentVaultId == null,
                            state.all.size.takeIf { it > 0 }) { viewModel.setVault(null); close() }
                        state.vaults.forEach { v ->
                            DrawerRow(v.name, Icons.Filled.Folder, state.currentVaultId == v.id,
                                v.displayCount.takeIf { it > 0 }) { viewModel.setVault(v.id); close() }
                        }
                    }

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
                    DrawerRow("Passkeys", Icons.Filled.Fingerprint, false) { close(); onOpenPasskeys() }

                    DrawerLabel("Manage")
                    DrawerRow("Manage Categories", Icons.Filled.Category, false) { close(); onOpenCategories() }
                    DrawerRow("Manage Vaults", Icons.Filled.Folder, false) { close(); onOpenVaults() }
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
                    title = {
                        Column {
                            Text(if (tab == HomeTab.Items) state.section.title else tab.label, fontWeight = FontWeight.SemiBold)
                            // Vault context line — so you always know which vault you're viewing.
                            if (tab == HomeTab.Home || tab == HomeTab.Items) {
                                Row(verticalAlignment = Alignment.CenterVertically) {
                                    Icon(
                                        Icons.Filled.Folder, null,
                                        modifier = Modifier.size(13.dp),
                                        tint = MaterialTheme.colorScheme.onSurfaceVariant,
                                    )
                                    Spacer(Modifier.size(4.dp))
                                    Text(
                                        state.vaultLabel,
                                        style = MaterialTheme.typography.labelMedium,
                                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                                    )
                                }
                            }
                        }
                    },
                    navigationIcon = {
                        IconButton(onClick = { scope.launch { drawerState.open() } }) {
                            AccountAvatar(state.user?.email)
                        }
                    },
                    actions = {
                        IconButton(onClick = onScanAdd) { Icon(Icons.Filled.QrCodeScanner, "Scan QR code") }
                        IconButton(onClick = onOpenSettings) { Icon(Icons.Filled.Settings, "Settings") }
                    },
                )
            },
            bottomBar = {
                NavigationBar {
                    HomeTab.entries.forEach { t ->
                        NavigationBarItem(
                            selected = tab == t,
                            onClick = { tab = t },
                            icon = { Icon(t.icon, t.label) },
                            label = { Text(t.label) },
                        )
                    }
                }
            },
            floatingActionButton = {
                if (tab != HomeTab.Search) {
                    ExtendedFloatingActionButton(
                        onClick = onAddItem,
                        icon = { Icon(Icons.Filled.Add, null) },
                        text = { Text("New Item") },
                    )
                }
            },
        ) { padding ->
            Box(Modifier.fillMaxSize().padding(padding)) {
                when (tab) {
                    HomeTab.Home -> HomeTabContent(state, onOpenItem, viewModel::toggleFavorite)
                    HomeTab.Items -> ItemsTabContent(state, scope, onOpenItem, viewModel::setQuery, viewModel::setCategoryFilter, viewModel::toggleFavorite)
                    HomeTab.Watchtower -> WatchtowerContent(state)
                    HomeTab.Search -> SearchTabContent(state, onOpenItem, viewModel::setQuery, viewModel::toggleFavorite)
                }
            }
        }
    }
}

enum class HomeTab(val label: String, val icon: androidx.compose.ui.graphics.vector.ImageVector) {
    Home("Home", Icons.Filled.Home),
    Items("Items", Icons.Filled.Inventory2),
    Watchtower("Watchtower", Icons.Filled.Security),
    Search("Search", Icons.Filled.Search),
}

@Composable
private fun AccountAvatar(email: String?) {
    val initial = (email ?: "?").trim().firstOrNull()?.uppercaseChar()?.toString() ?: "?"
    Box(
        Modifier.size(30.dp).clip(androidx.compose.foundation.shape.CircleShape).background(MaterialTheme.colorScheme.primary),
        contentAlignment = Alignment.Center,
    ) { Text(initial, color = MaterialTheme.colorScheme.onPrimary, style = MaterialTheme.typography.labelLarge) }
}

@OptIn(ExperimentalFoundationApi::class)
@Composable
private fun ItemsTabContent(
    state: HomeState,
    scope: kotlinx.coroutines.CoroutineScope,
    onOpenItem: (Int) -> Unit,
    onQuery: (String) -> Unit,
    onCategory: (String?) -> Unit,
    onToggleFavorite: (Int) -> Unit,
) {
    Column(Modifier.fillMaxSize()) {
        if (state.categories.isNotEmpty()) {
            CategoryCombobox(state.categories, state.categoryFilter, onCategory)
        }
        SearchField(state.query, onQuery)
        when {
            state.error != null -> CenterText(state.error, MaterialTheme.colorScheme.error)
            state.visible.isEmpty() && !state.loading ->
                CenterText("Nothing here yet.", MaterialTheme.colorScheme.onSurfaceVariant)
            else -> GroupedItemList(state.visible, scope, onOpenItem, onToggleFavorite)
        }
    }
}

@OptIn(ExperimentalFoundationApi::class)
@Composable
private fun GroupedItemList(
    items: List<com.vaultguard.app.data.model.VaultItem>,
    scope: kotlinx.coroutines.CoroutineScope,
    onOpenItem: (Int) -> Unit,
    onToggleFavorite: (Int) -> Unit,
) {
    val grouped = items.sortedBy { it.title.lowercase() }
        .groupBy { it.title.firstOrNull()?.uppercaseChar()?.takeIf { c -> c.isLetter() } ?: '#' }
        .toSortedMap()
    val letterToIndex = remember(grouped.keys.toList()) {
        var idx = 0
        buildMap { grouped.forEach { (l, its) -> put(l, idx); idx += 1 + its.size } }
    }
    val listState = rememberLazyListState()
    Box(Modifier.fillMaxSize()) {
        LazyColumn(Modifier.fillMaxSize().padding(end = 22.dp), state = listState) {
            grouped.forEach { (letter, itemsForLetter) ->
                stickyHeader { SectionHeader(letter.toString()) }
                item(key = "card_$letter") {
                    Column(
                        Modifier.fillMaxWidth().padding(horizontal = 12.dp, vertical = 2.dp)
                            .clip(RoundedCornerShape(16.dp)).background(MaterialTheme.colorScheme.surface),
                    ) {
                        itemsForLetter.forEachIndexed { i, item ->
                            VaultItemRow(item, onClick = { onOpenItem(item.id) }, onToggleFavorite = { onToggleFavorite(item.id) })
                            if (i < itemsForLetter.lastIndex) {
                                HorizontalDivider(Modifier.padding(start = 72.dp), color = MaterialTheme.colorScheme.outlineVariant)
                            }
                        }
                    }
                }
            }
            item { Spacer(Modifier.height(88.dp)) }
        }
        AlphabetIndex(
            letters = grouped.keys.map { it.toString() },
            modifier = Modifier.align(Alignment.CenterEnd).padding(end = 2.dp),
            onLetter = { letter -> letterToIndex[letter.firstOrNull()]?.let { scope.launch { listState.scrollToItem(it) } } },
        )
    }
}

@Composable
private fun HomeTabContent(state: HomeState, onOpenItem: (Int) -> Unit, onToggleFavorite: (Int) -> Unit) {
    val active = state.all.filter { !it.isArchived && !it.isDeleted }
    val favourites = active.filter { it.isFavorite }
    val recent = active.sortedByDescending { it.id }.take(10)
    LazyColumn(Modifier.fillMaxSize().padding(horizontal = 12.dp), verticalArrangement = Arrangement.spacedBy(4.dp)) {
        if (favourites.isNotEmpty()) {
            item { HomeSectionHeader("Favourites") }
            item { GroupCard(favourites, onOpenItem, onToggleFavorite) }
        }
        item { HomeSectionHeader("Recently Added") }
        item {
            if (recent.isEmpty()) CenterText("Nothing here yet.", MaterialTheme.colorScheme.onSurfaceVariant)
            else GroupCard(recent, onOpenItem, onToggleFavorite)
        }
        item { Spacer(Modifier.height(88.dp)) }
    }
}

@Composable
private fun SearchTabContent(state: HomeState, onOpenItem: (Int) -> Unit, onQuery: (String) -> Unit, onToggleFavorite: (Int) -> Unit) {
    val results = state.all.filter { !it.isArchived && !it.isDeleted }.filter {
        state.query.isNotBlank() && (
            it.title.contains(state.query, true) ||
                it.username?.contains(state.query, true) == true ||
                it.website?.contains(state.query, true) == true)
    }
    Column(Modifier.fillMaxSize()) {
        SearchField(state.query, onQuery)
        when {
            state.query.isBlank() -> CenterText("Search your vault.", MaterialTheme.colorScheme.onSurfaceVariant)
            results.isEmpty() -> CenterText("No matches.", MaterialTheme.colorScheme.onSurfaceVariant)
            else -> LazyColumn(Modifier.fillMaxSize().padding(horizontal = 12.dp)) {
                item { GroupCard(results, onOpenItem, onToggleFavorite) }
            }
        }
    }
}

@Composable
private fun WatchtowerContent(state: HomeState) {
    val active = state.all.filter { !it.isArchived && !it.isDeleted }
    val byType = active.groupingBy { it.type }.eachCount().entries.sortedByDescending { it.value }
    LazyColumn(Modifier.fillMaxSize().padding(16.dp), verticalArrangement = Arrangement.spacedBy(12.dp)) {
        item {
            Row(Modifier.fillMaxWidth(), Arrangement.spacedBy(12.dp)) {
                StatCard("Items", active.size, Modifier.weight(1f))
                StatCard("Favourites", active.count { it.isFavorite }, Modifier.weight(1f))
            }
        }
        item {
            Column(
                Modifier.fillMaxWidth().clip(RoundedCornerShape(16.dp)).background(MaterialTheme.colorScheme.surface).padding(4.dp),
            ) {
                byType.forEach { (type, count) ->
                    ListItem(
                        leadingContent = { Icon(iconFor(type), null, tint = MaterialTheme.colorScheme.primary) },
                        headlineContent = { Text(type.label) },
                        trailingContent = { Text("$count", fontWeight = FontWeight.SemiBold) },
                    )
                }
            }
        }
    }
}

@Composable
private fun StatCard(label: String, value: Int, modifier: Modifier = Modifier) {
    Column(
        modifier.clip(RoundedCornerShape(16.dp)).background(MaterialTheme.colorScheme.surface).padding(18.dp),
    ) {
        Text("$value", style = MaterialTheme.typography.headlineMedium, fontWeight = FontWeight.Bold, color = MaterialTheme.colorScheme.primary)
        Text(label, style = MaterialTheme.typography.bodyMedium, color = MaterialTheme.colorScheme.onSurfaceVariant)
    }
}

@Composable
private fun HomeSectionHeader(text: String) {
    Text(text, style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.Bold,
        modifier = Modifier.padding(start = 8.dp, top = 16.dp, bottom = 6.dp))
}

@Composable
private fun GroupCard(items: List<com.vaultguard.app.data.model.VaultItem>, onOpenItem: (Int) -> Unit, onToggleFavorite: (Int) -> Unit) {
    Column(Modifier.fillMaxWidth().clip(RoundedCornerShape(16.dp)).background(MaterialTheme.colorScheme.surface)) {
        items.forEachIndexed { i, item ->
            VaultItemRow(item, onClick = { onOpenItem(item.id) }, onToggleFavorite = { onToggleFavorite(item.id) })
            if (i < items.lastIndex) HorizontalDivider(Modifier.padding(start = 72.dp), color = MaterialTheme.colorScheme.outlineVariant)
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun CategoryCombobox(
    categories: List<CategoryDto>,
    selected: String?,
    onSelect: (String?) -> Unit,
) {
    var expanded by remember { mutableStateOf(false) }
    val fallback = MaterialTheme.colorScheme.primary
    val selColor = hexColor(categories.firstOrNull { it.name == selected }?.color, fallback)

    ExposedDropdownMenuBox(
        expanded = expanded,
        onExpandedChange = { expanded = it },
        modifier = Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 6.dp),
    ) {
        OutlinedTextField(
            value = selected ?: "All Categories",
            onValueChange = {},
            readOnly = true,
            label = { Text("Category") },
            leadingIcon = {
                if (selected == null) Icon(Icons.Filled.Apps, null, tint = fallback)
                else CategoryGlyph(selected, selColor)
            },
            trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(expanded = expanded) },
            shape = RoundedCornerShape(14.dp),
            modifier = Modifier.fillMaxWidth().menuAnchor(),
        )
        ExposedDropdownMenu(expanded = expanded, onDismissRequest = { expanded = false }) {
            DropdownMenuItem(
                text = { Text("All Categories") },
                leadingIcon = { Icon(Icons.Filled.Apps, null, tint = fallback) },
                trailingIcon = { if (selected == null) Icon(Icons.Filled.Check, null, tint = fallback) },
                onClick = { onSelect(null); expanded = false },
            )
            categories.forEach { cat ->
                val c = hexColor(cat.color, fallback)
                DropdownMenuItem(
                    text = { Text(cat.name) },
                    leadingIcon = { CategoryGlyph(cat.name, c) },
                    trailingIcon = { if (selected == cat.name) Icon(Icons.Filled.Check, null, tint = c) },
                    onClick = { onSelect(cat.name); expanded = false },
                )
            }
        }
    }
}

/** A rounded, tinted glyph for a category, coloured to match the desktop category colours. */
@Composable
private fun CategoryGlyph(name: String, color: Color) {
    Box(
        Modifier.size(28.dp).clip(RoundedCornerShape(8.dp)).background(color.copy(alpha = 0.18f)),
        contentAlignment = Alignment.Center,
    ) {
        Icon(iconFor(categoryType(name)), null, tint = color, modifier = Modifier.size(17.dp))
    }
}

private fun categoryType(name: String): ItemType = when {
    name.startsWith("Login", true) -> ItemType.Login
    name.startsWith("Credit", true) -> ItemType.CreditCard
    name.startsWith("Secure", true) -> ItemType.SecureNote
    name.startsWith("WiFi", true) || name.startsWith("Wi-Fi", true) -> ItemType.WiFi
    name.startsWith("Passkey", true) -> ItemType.Passkey
    name.startsWith("Ident", true) -> ItemType.Identity
    else -> ItemType.Password
}

private fun hexColor(hex: String?, fallback: Color): Color =
    try {
        if (hex.isNullOrBlank()) fallback
        else Color(android.graphics.Color.parseColor(if (hex.startsWith("#")) hex else "#$hex"))
    } catch (_: Exception) { fallback }

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
private fun AlphabetIndex(letters: List<String>, modifier: Modifier = Modifier, onLetter: (String) -> Unit) {
    if (letters.size < 2) return
    Column(modifier, horizontalAlignment = Alignment.CenterHorizontally) {
        letters.forEach { l ->
            Text(
                l,
                style = MaterialTheme.typography.labelSmall,
                fontWeight = FontWeight.Bold,
                color = MaterialTheme.colorScheme.primary,
                modifier = Modifier.clickable { onLetter(l) }.padding(horizontal = 3.dp, vertical = 1.dp),
            )
        }
    }
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
