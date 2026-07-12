package com.vaultguard.app.ui.navigation

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.navigation.NavController
import androidx.navigation.NavType
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import androidx.navigation.navArgument
import com.vaultguard.app.ui.browse.AboutScreen
import com.vaultguard.app.ui.browse.CategoriesScreen
import com.vaultguard.app.ui.browse.ImportScreen
import com.vaultguard.app.ui.browse.OnePasswordImportScreen
import com.vaultguard.app.ui.browse.ProfileScreen
import com.vaultguard.app.ui.browse.SecurityDashboardScreen
import com.vaultguard.app.ui.browse.VaultsScreen
import com.vaultguard.app.ui.detail.ItemDetailScreen
import com.vaultguard.app.ui.edit.ItemEditScreen
import com.vaultguard.app.ui.home.HomeScreen
import com.vaultguard.app.ui.login.UnlockScreen
import com.vaultguard.app.ui.qr.QrScannerScreen
import com.vaultguard.app.ui.settings.SettingsScreen
import com.vaultguard.app.ui.setup.ConnectionSetupScreen

/**
 * Rapid repeated back presses can queue more than one pop against the same
 * back-stack entry while its exit transition is still in flight, overshooting
 * onto a transient/blank destination. Only pop when the current entry is
 * actually RESUMED so extra taps in flight are no-ops instead of double-pops.
 */
private fun NavController.safePopBackStack(): Boolean {
    val resumed = currentBackStackEntry?.lifecycle?.currentState == Lifecycle.State.RESUMED
    return if (resumed) popBackStack() else false
}

@Composable
fun VaultGuardNavGraph(rootViewModel: RootViewModel = hiltViewModel()) {
    val navController = rememberNavController()
    val start by rootViewModel.startState.collectAsStateWithLifecycle()

    when (start) {
        StartState.LOADING -> Box(Modifier.fillMaxSize(), Alignment.Center) { CircularProgressIndicator() }
        else -> {
            val startRoute = if (start == StartState.SETUP) Routes.SETUP else Routes.UNLOCK
            NavHost(navController = navController, startDestination = startRoute) {

                composable(Routes.SETUP) {
                    ConnectionSetupScreen(
                        onDone = {
                            navController.navigate(Routes.UNLOCK) { popUpTo(Routes.SETUP) { inclusive = true } }
                        },
                    )
                }

                composable(Routes.UNLOCK) {
                    UnlockScreen(
                        onUnlocked = {
                            navController.navigate(Routes.HOME) { popUpTo(Routes.UNLOCK) { inclusive = true } }
                        },
                        onEditConnection = { navController.navigate(Routes.SETUP) },
                    )
                }

                composable(Routes.HOME) {
                    HomeScreen(
                        onOpenItem = { id -> navController.navigate(Routes.detail(id)) },
                        onAddItem = { navController.navigate(Routes.edit()) },
                        onScanAdd = { navController.navigate(Routes.QR_LOGIN) },
                        onOpenSettings = { navController.navigate(Routes.SETTINGS) },
                        onOpenProfile = { navController.navigate(Routes.PROFILE) },
                        onSwitchAccount = { navController.navigate(Routes.UNLOCK) { popUpTo(0) { inclusive = true } } },
                        onOpenVaults = { navController.navigate(Routes.VAULTS) },
                        onOpenCategories = { navController.navigate(Routes.CATEGORIES) },
                        onOpenSecurity = { navController.navigate(Routes.SECURITY) },
                        onOpenPasskeys = { navController.navigate(Routes.PASSKEYS) },
                        onOpenImport = { navController.navigate(Routes.IMPORT) },
                        onOpenAbout = { navController.navigate(Routes.ABOUT) },
                    )
                }

                composable(
                    route = Routes.DETAIL,
                    arguments = listOf(navArgument("id") { type = NavType.IntType }),
                ) { entry ->
                    val id = entry.arguments?.getInt("id")
                    if (id == null) {
                        // Transient null args mid back-stack-transition: bail out to Home
                        // instead of rendering a blank screen with no way forward.
                        LaunchedEffect(Unit) { navController.navigate(Routes.HOME) { popUpTo(Routes.HOME) { inclusive = true } } }
                        Box(Modifier.fillMaxSize(), Alignment.Center) { CircularProgressIndicator() }
                    } else {
                        ItemDetailScreen(
                            itemId = id,
                            onBack = { navController.safePopBackStack() },
                            onEdit = { navController.navigate(Routes.edit(id)) },
                        )
                    }
                }

                composable(
                    route = Routes.EDIT,
                    arguments = listOf(
                        navArgument("id") { type = NavType.IntType; defaultValue = -1 },
                        navArgument("scan") { type = NavType.BoolType; defaultValue = false },
                    ),
                ) { entry ->
                    val id = entry.arguments?.getInt("id") ?: -1
                    val autoScan = entry.arguments?.getBoolean("scan") ?: false
                    ItemEditScreen(
                        itemId = id,
                        autoScan = autoScan,
                        navController = navController,
                        onDone = { navController.safePopBackStack() },
                        onScanTotp = { navController.navigate(Routes.SCAN) },
                    )
                }

                composable(Routes.QR_LOGIN) {
                    com.vaultguard.app.ui.qr.QrLoginScreen(onDone = { navController.safePopBackStack() })
                }

                composable(Routes.SCAN) {
                    QrScannerScreen(
                        onResult = { value ->
                            navController.previousBackStackEntry?.savedStateHandle?.set(Routes.SCAN_RESULT, value)
                            navController.safePopBackStack()
                        },
                        onCancel = { navController.safePopBackStack() },
                    )
                }

                composable(Routes.SETTINGS) {
                    SettingsScreen(
                        onBack = { navController.safePopBackStack() },
                        onEditConnection = { navController.navigate(Routes.SETUP) },
                        onLocked = { navController.navigate(Routes.UNLOCK) { popUpTo(0) { inclusive = true } } },
                        onOpenImport = { navController.navigate(Routes.IMPORT) },
                        onOpen1Password = { navController.navigate(Routes.IMPORT_1PASSWORD) },
                        onOpenDeviceSetup = { navController.navigate(Routes.DEVICE_SETUP) },
                    )
                }

                composable(Routes.DEVICE_SETUP) {
                    com.vaultguard.app.ui.setup.DeviceSetupScreen(onBack = { navController.safePopBackStack() })
                }

                composable(Routes.VAULTS) { VaultsScreen(onBack = { navController.safePopBackStack() }) }
                composable(Routes.CATEGORIES) { CategoriesScreen(onBack = { navController.safePopBackStack() }) }
                composable(Routes.SECURITY) { SecurityDashboardScreen(onBack = { navController.safePopBackStack() }) }
                composable(Routes.PASSKEYS) { com.vaultguard.app.ui.passkeys.PasskeysScreen(onBack = { navController.safePopBackStack() }) }
                composable(Routes.IMPORT) {
                    ImportScreen(
                        onBack = { navController.safePopBackStack() },
                        onOpenOnePassword = { navController.navigate(Routes.IMPORT_1PASSWORD) },
                    )
                }
                composable(Routes.IMPORT_1PASSWORD) {
                    OnePasswordImportScreen(onBack = { navController.safePopBackStack() })
                }
                composable(Routes.ABOUT) { AboutScreen(onBack = { navController.safePopBackStack() }) }
                composable(Routes.PROFILE) {
                    ProfileScreen(
                        onBack = { navController.safePopBackStack() },
                        onLocked = { navController.navigate(Routes.UNLOCK) { popUpTo(0) { inclusive = true } } },
                    )
                }
            }
        }
    }
}
