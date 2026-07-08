package com.vaultguard.app.ui.navigation

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.navigation.NavType
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import androidx.navigation.navArgument
import com.vaultguard.app.ui.browse.AboutScreen
import com.vaultguard.app.ui.browse.CategoriesScreen
import com.vaultguard.app.ui.browse.ImportScreen
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
                        onOpenSettings = { navController.navigate(Routes.SETTINGS) },
                        onOpenProfile = { navController.navigate(Routes.PROFILE) },
                        onOpenVaults = { navController.navigate(Routes.VAULTS) },
                        onOpenCategories = { navController.navigate(Routes.CATEGORIES) },
                        onOpenSecurity = { navController.navigate(Routes.SECURITY) },
                        onOpenImport = { navController.navigate(Routes.IMPORT) },
                        onOpenAbout = { navController.navigate(Routes.ABOUT) },
                    )
                }

                composable(
                    route = Routes.DETAIL,
                    arguments = listOf(navArgument("id") { type = NavType.IntType }),
                ) { entry ->
                    val id = entry.arguments?.getInt("id") ?: return@composable
                    ItemDetailScreen(
                        itemId = id,
                        onBack = { navController.popBackStack() },
                        onEdit = { navController.navigate(Routes.edit(id)) },
                    )
                }

                composable(
                    route = Routes.EDIT,
                    arguments = listOf(navArgument("id") { type = NavType.IntType; defaultValue = -1 }),
                ) { entry ->
                    val id = entry.arguments?.getInt("id") ?: -1
                    ItemEditScreen(
                        itemId = id,
                        navController = navController,
                        onDone = { navController.popBackStack() },
                        onScanTotp = { navController.navigate(Routes.SCAN) },
                    )
                }

                composable(Routes.SCAN) {
                    QrScannerScreen(
                        onResult = { value ->
                            navController.previousBackStackEntry?.savedStateHandle?.set(Routes.SCAN_RESULT, value)
                            navController.popBackStack()
                        },
                        onCancel = { navController.popBackStack() },
                    )
                }

                composable(Routes.SETTINGS) {
                    SettingsScreen(
                        onBack = { navController.popBackStack() },
                        onEditConnection = { navController.navigate(Routes.SETUP) },
                        onLocked = { navController.navigate(Routes.UNLOCK) { popUpTo(0) { inclusive = true } } },
                        onOpenImport = { navController.navigate(Routes.IMPORT) },
                    )
                }

                composable(Routes.VAULTS) { VaultsScreen(onBack = { navController.popBackStack() }) }
                composable(Routes.CATEGORIES) { CategoriesScreen(onBack = { navController.popBackStack() }) }
                composable(Routes.SECURITY) { SecurityDashboardScreen(onBack = { navController.popBackStack() }) }
                composable(Routes.IMPORT) { ImportScreen(onBack = { navController.popBackStack() }) }
                composable(Routes.ABOUT) { AboutScreen(onBack = { navController.popBackStack() }) }
                composable(Routes.PROFILE) {
                    ProfileScreen(
                        onBack = { navController.popBackStack() },
                        onLocked = { navController.navigate(Routes.UNLOCK) { popUpTo(0) { inclusive = true } } },
                    )
                }
            }
        }
    }
}
