package com.vaultguard.app.ui.login

import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.AdminPanelSettings
import androidx.compose.material.icons.filled.ChildCare
import androidx.compose.material.icons.filled.Person
import androidx.compose.material.icons.filled.SupervisorAccount
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector

data class SeededUser(
    val email: String,
    val name: String,
    val role: String,
    val icon: ImageVector,
    /** Accent used for the role avatar so the switcher reads like the WPF profile picker. */
    val color: Color,
)

/**
 * The built-in demo accounts seeded by VaultGuard.DAL.Seed.IdentityDataSeeder, shown on the login screen
 * exactly like the WPF app (admin / parent / user / child), with role icons + colors. Common master key below.
 */
object DefaultAccounts {
    const val commonMasterKey = "7hm3Z!Csu:Y64nm"

    val users = listOf(
        SeededUser("admin@passwordmanager.local", "Administrator", "Admin", Icons.Filled.AdminPanelSettings, Color(0xFF7C3AED)),
        SeededUser("parent@passwordmanager.local", "Parent", "Parent", Icons.Filled.SupervisorAccount, Color(0xFF2563EB)),
        SeededUser("user@passwordmanager.local", "Regular User", "User", Icons.Filled.Person, Color(0xFF059669)),
        SeededUser("child@passwordmanager.local", "Child", "Child", Icons.Filled.ChildCare, Color(0xFFEC4899)),
    )
}
