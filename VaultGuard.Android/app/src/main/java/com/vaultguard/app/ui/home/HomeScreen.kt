package com.vaultguard.app.ui.home

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
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
import androidx.compose.material.icons.filled.Edit
import androidx.compose.material.icons.filled.ExitToApp
import androidx.compose.material.icons.filled.Fingerprint
import androidx.compose.material.icons.filled.Folder
import androidx.compose.material.icons.filled.Home
import androidx.compose.material.icons.filled.Info
import androidx.compose.material.icons.filled.Inventory2
import androidx.compose.material.icons.filled.Lock
import androidx.compose.material.icons.filled.Menu
import androidx.compose.material.icons.filled.Person
import androidx.compose.material.icons.filled.QrCodeScanner
import androidx.compose.material.icons.filled.Search
import androidx.compose.material.icons.filled.Security
import androidx.compose.material.icons.filled.Settings
import androidx.compose.material.icons.filled.Star
import androidx.compose.material.icons.filled.SwitchAccount
import androidx.compose.material.icons.filled.UploadFile
import androidx.compose.material3.Badge
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExposedDropdownMenuBox
import androidx.compose.material3.ExposedDropdownMenuDefaults
import androidx.compose.material3.OutlinedTextField
import androidx.compose.ui.draw.clip
import androidx.compose.material3.DrawerValue
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.ExtendedFloatingActionButton
import androidx.compose.material3.FloatingActionButtonDefaults
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
import androidx.compose.ui.platform.LocalContext
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

@OptIn(ExperimentalMaterial3Api::class)
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
    val context = LocalContext.current
    var tab by remember { mutableStateOf(HomeTab.Home) }
    var accountMenu by remember { mutableStateOf(false) }
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
                    DrawerRow("Security Center", Icons.Filled.Security, false) { close(); onOpenSecurity() }
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
                        Box {
                            IconButton(onClick = { accountMenu = true }) {
                                AccountAvatar(state.user?.email)
                            }
                            AccountMenu(
                                expanded = accountMenu,
                                email = state.user?.email,
                                vaultLabel = state.vaultLabel,
                                onDismiss = { accountMenu = false },
                                onProfile = { accountMenu = false; onOpenProfile() },
                                onSwitchAccount = { accountMenu = false; viewModel.lock(); onSwitchAccount() },
                                onSettings = { accountMenu = false; onOpenSettings() },
                                onLock = { accountMenu = false; viewModel.lock() },
                                onExit = { accountMenu = false; context.findActivity()?.finishAndRemoveTask() },
                            )
                        }
                    },
                    actions = {
                        IconButton(onClick = { scope.launch { drawerState.open() } }) {
                            Icon(Icons.Filled.Menu, "Browse")
                        }
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
                            label = { Text(t.label, maxLines = 1, softWrap = false, style = MaterialTheme.typography.labelSmall) },
                            alwaysShowLabel = true,
                        )
                    }
                }
            },
            floatingActionButton = {
                if (tab != HomeTab.Search) {
                    // Prominent, high-emphasis "create" action — a pencil glyph on the accent colour so it
                    // reads as the primary action, echoing the old desktop action buttons.
                    ExtendedFloatingActionButton(
                        onClick = onAddItem,
                        icon = { Icon(Icons.Filled.Edit, null) },
                        text = { Text("New Item", fontWeight = FontWeight.SemiBold) },
                        containerColor = MaterialTheme.colorScheme.primary,
                        contentColor = MaterialTheme.colorScheme.onPrimary,
                        elevation = FloatingActionButtonDefaults.elevation(defaultElevation = 8.dp),
                    )
                }
            },
        ) { padding ->
            Box(Modifier.fillMaxSize().padding(padding)) {
                when (tab) {
                    HomeTab.Home -> HomeTabContent(state, onOpenItem, viewModel::toggleFavorite)
                    HomeTab.Favourites -> FavouritesTabContent(state, onOpenItem, viewModel::toggleFavorite)
                    HomeTab.Items -> ItemsTabContent(state, onOpenItem, viewModel::setQuery, viewModel::setCategoryFilter, viewModel::toggleFavorite)
                    HomeTab.Security -> WatchtowerContent(state)
                    HomeTab.Search -> SearchTabContent(state, onOpenItem, viewModel::setQuery, viewModel::setCategoryFilter, viewModel::toggleFavorite)
                }
            }
        }
    }
}

enum class HomeTab(val label: String, val icon: androidx.compose.ui.graphics.vector.ImageVector) {
    Home("Home", Icons.Filled.Home),
    Favourites("Favourites", Icons.Filled.Star),
    Items("Items", Icons.Filled.Inventory2),
    Security("Security", Icons.Filled.Security),
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

/**
 * 1Password-style account popup anchored to the top-left profile avatar: shows who's signed in and
 * the quick account actions (profile, switch account, settings, lock, exit).
 */
@Composable
private fun AccountMenu(
    expanded: Boolean,
    email: String?,
    vaultLabel: String,
    onDismiss: () -> Unit,
    onProfile: () -> Unit,
    onSwitchAccount: () -> Unit,
    onSettings: () -> Unit,
    onLock: () -> Unit,
    onExit: () -> Unit,
) {
    DropdownMenu(expanded = expanded, onDismissRequest = onDismiss) {
        // Header — signed-in identity and the active vault.
        Row(
            Modifier.padding(horizontal = 16.dp, vertical = 10.dp),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            AccountAvatar(email)
            Spacer(Modifier.size(12.dp))
            Column {
                Text(email ?: "My Account", fontWeight = FontWeight.SemiBold, style = MaterialTheme.typography.bodyMedium)
                Text(vaultLabel, style = MaterialTheme.typography.labelSmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
            }
        }
        HorizontalDivider()
        DropdownMenuItem(
            text = { Text("My Profile") },
            leadingIcon = { Icon(Icons.Filled.Person, null) },
            onClick = onProfile,
        )
        DropdownMenuItem(
            text = { Text("Switch Account") },
            leadingIcon = { Icon(Icons.Filled.SwitchAccount, null) },
            onClick = onSwitchAccount,
        )
        DropdownMenuItem(
            text = { Text("Settings") },
            leadingIcon = { Icon(Icons.Filled.Settings, null) },
            onClick = onSettings,
        )
        DropdownMenuItem(
            text = { Text("Lock") },
            leadingIcon = { Icon(Icons.Filled.Lock, null) },
            onClick = onLock,
        )
        HorizontalDivider()
        DropdownMenuItem(
            text = { Text("Exit", color = MaterialTheme.colorScheme.error) },
            leadingIcon = { Icon(Icons.Filled.ExitToApp, null, tint = MaterialTheme.colorScheme.error) },
            onClick = onExit,
        )
    }
}

/** Walks the [android.content.ContextWrapper] chain to find the hosting Activity (for a clean Exit). */
private fun android.content.Context.findActivity(): android.app.Activity? {
    var ctx: android.content.Context? = this
    while (ctx is android.content.ContextWrapper) {
        if (ctx is android.app.Activity) return ctx
        ctx = ctx.baseContext
    }
    return null
}

/** The main item browser — 1Password's desktop layout: category filter, search, items grouped by month added. */
@Composable
private fun ItemsTabContent(
    state: HomeState,
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
            else -> MonthGroupedList(state.visible, onOpenItem, onToggleFavorite)
        }
    }
}

/** Dedicated Favourites tab — quick access to starred items, promoted to the bottom bar. */
@Composable
private fun FavouritesTabContent(state: HomeState, onOpenItem: (Int) -> Unit, onToggleFavorite: (Int) -> Unit) {
    val favourites = state.all.filter { !it.isArchived && !it.isDeleted && it.isFavorite }
    if (favourites.isEmpty()) {
        CenterText("No favourites yet. Tap the ☆ on any item to pin it here.", MaterialTheme.colorScheme.onSurfaceVariant)
        return
    }
    LazyColumn(Modifier.fillMaxSize().padding(horizontal = 12.dp), verticalArrangement = Arrangement.spacedBy(4.dp)) {
        item { HomeSectionHeader("Favourites") }
        item { GroupCard(favourites, onOpenItem, onToggleFavorite) }
        item { Spacer(Modifier.height(88.dp)) }
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

/**
 * 1Password-style "Quick Access" search: a category filter, a search field, and matching items
 * grouped by the month they were added — mirroring the desktop app's quick-access popup.
 */
@Composable
private fun SearchTabContent(
    state: HomeState,
    onOpenItem: (Int) -> Unit,
    onQuery: (String) -> Unit,
    onCategory: (String?) -> Unit,
    onToggleFavorite: (Int) -> Unit,
) {
    val results = state.all.filter { !it.isArchived && !it.isDeleted }
        .filter { state.categoryFilter == null || it.categoryName == state.categoryFilter }
        .filter {
            state.query.isBlank() ||
                it.title.contains(state.query, true) ||
                it.username?.contains(state.query, true) == true ||
                it.website?.contains(state.query, true) == true
        }
    Column(Modifier.fillMaxSize()) {
        if (state.categories.isNotEmpty()) {
            CategoryCombobox(state.categories, state.categoryFilter, onCategory)
        }
        SearchField(state.query, onQuery)
        when {
            results.isEmpty() && state.query.isBlank() && state.categoryFilter == null ->
                CenterText("Search your vault.", MaterialTheme.colorScheme.onSurfaceVariant)
            results.isEmpty() -> CenterText("No matches.", MaterialTheme.colorScheme.onSurfaceVariant)
            else -> MonthGroupedList(results, onOpenItem, onToggleFavorite)
        }
    }
}

/** Groups items by "Month Year" (newest first), matching 1Password's date-sectioned item list. */
@Composable
fun MonthGroupedList(
    items: List<com.vaultguard.app.data.model.VaultItem>,
    onOpenItem: (Int) -> Unit,
    onToggleFavorite: (Int) -> Unit,
) {
    val monthFormat = remember { java.text.SimpleDateFormat("MMMM yyyy", java.util.Locale.getDefault()) }
    val grouped = items.sortedByDescending { it.createdAt }
        .groupBy { item ->
            if (item.createdAt <= 0L) "Undated" else monthFormat.format(java.util.Date(item.createdAt)).uppercase()
        }
    LazyColumn(Modifier.fillMaxSize().padding(horizontal = 12.dp)) {
        grouped.forEach { (month, itemsForMonth) ->
            item(key = "month_header_$month") { HomeSectionHeader(month) }
            item(key = "month_card_$month") { GroupCard(itemsForMonth, onOpenItem, onToggleFavorite) }
        }
        item { Spacer(Modifier.height(88.dp)) }
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
        // Distribution chart — a lightweight horizontal bar chart of items per category/type.
        if (byType.isNotEmpty()) {
            item {
                Column(
                    Modifier.fillMaxWidth().clip(RoundedCornerShape(16.dp)).background(MaterialTheme.colorScheme.surface).padding(16.dp),
                    verticalArrangement = Arrangement.spacedBy(10.dp),
                ) {
                    Text("Items by type", style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.Bold)
                    val max = byType.maxOf { it.value }.coerceAtLeast(1)
                    byType.forEach { (type, count) ->
                        BarRow(type.label, count, max)
                    }
                }
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

/** One horizontal bar in the Watchtower distribution chart. */
@Composable
private fun BarRow(label: String, value: Int, max: Int) {
    Row(verticalAlignment = Alignment.CenterVertically) {
        Text(
            label, maxLines = 1, softWrap = false,
            style = MaterialTheme.typography.labelMedium,
            modifier = Modifier.width(96.dp),
        )
        Box(
            Modifier.weight(1f).height(20.dp).clip(RoundedCornerShape(6.dp))
                .background(MaterialTheme.colorScheme.surfaceVariant),
        ) {
            Box(
                Modifier.fillMaxHeight()
                    .fillMaxWidth((value.toFloat() / max).coerceIn(0.02f, 1f))
                    .clip(RoundedCornerShape(6.dp))
                    .background(MaterialTheme.colorScheme.primary),
            )
        }
        Text(
            "$value", fontWeight = FontWeight.SemiBold,
            style = MaterialTheme.typography.labelMedium,
            modifier = Modifier.width(32.dp).padding(start = 8.dp),
        )
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
fun HomeSectionHeader(text: String) {
    Text(text, style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.Bold,
        modifier = Modifier.padding(start = 8.dp, top = 16.dp, bottom = 6.dp))
}

@Composable
fun GroupCard(items: List<com.vaultguard.app.data.model.VaultItem>, onOpenItem: (Int) -> Unit, onToggleFavorite: (Int) -> Unit) {
    Column(Modifier.fillMaxWidth().clip(RoundedCornerShape(16.dp)).background(MaterialTheme.colorScheme.surface)) {
        items.forEachIndexed { i, item ->
            VaultItemRow(item, onClick = { onOpenItem(item.id) }, onToggleFavorite = { onToggleFavorite(item.id) })
            if (i < items.lastIndex) HorizontalDivider(Modifier.padding(start = 72.dp), color = MaterialTheme.colorScheme.outlineVariant)
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun CategoryCombobox(
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
fun SearchField(query: String, onChange: (String) -> Unit) {
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
fun CenterText(text: String, color: Color) {
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
