package com.vaultguard.app.ui.home

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.vaultguard.app.data.model.CategoryDto
import com.vaultguard.app.data.model.UserDto
import com.vaultguard.app.data.model.VaultDto
import com.vaultguard.app.data.model.VaultItem
import com.vaultguard.app.data.repo.SessionManager
import com.vaultguard.app.data.repo.VaultRepository
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import javax.inject.Inject

data class HomeState(
    val loading: Boolean = true,
    val all: List<VaultItem> = emptyList(),
    val categories: List<CategoryDto> = emptyList(),
    val categoryFilter: String? = null,
    val section: VaultSection = VaultSection.AllItems,
    val query: String = "",
    val user: UserDto? = null,
    val error: String? = null,
    val vaults: List<VaultDto> = emptyList(),
    /** The vault currently being viewed. Null = "All Vaults". */
    val currentVaultId: Int? = null,
) {
    /** The vault the user is viewing, or null when showing every vault. */
    val currentVault: VaultDto? get() = vaults.firstOrNull { it.id == currentVaultId }

    /** Display name for the vault context, shown on the list header ("All Vaults" when unscoped). */
    val vaultLabel: String
        get() = currentVault?.name ?: vaults.firstOrNull { it.isDefault }?.name ?: "All Vaults"

    /** Items scoped to the current vault (LOCAL mode); API-mode items have no vaultId so pass through. */
    private val inVault: List<VaultItem>
        get() = all.filter { currentVaultId == null || it.vaultId == null || it.vaultId == currentVaultId }

    val visible: List<VaultItem>
        get() = inVault.filter { section.matches(it) }
            .filter { categoryFilter == null || it.categoryName == categoryFilter }
            .filter {
                query.isBlank() ||
                    it.title.contains(query, true) ||
                    it.username?.contains(query, true) == true ||
                    it.website?.contains(query, true) == true
            }

    fun count(section: VaultSection): Int = inVault.count { section.matches(it) }
}

@HiltViewModel
class HomeViewModel @Inject constructor(
    private val repository: VaultRepository,
    private val session: SessionManager,
) : ViewModel() {

    private val _state = MutableStateFlow(HomeState(user = session.user.value))
    val state = _state.asStateFlow()

    init { refresh() }

    /** Lock the vault (used by "Switch account" — the unlock screen then shows the account picker). */
    fun lock() = session.lock()

    fun setSection(section: VaultSection) = _state.update { it.copy(section = section) }
    fun setQuery(q: String) = _state.update { it.copy(query = q) }
    fun setCategoryFilter(name: String?) = _state.update { it.copy(categoryFilter = name) }

    /** Scope the list to a single vault (or null for all vaults) and reset any category filter. */
    fun setVault(id: Int?) = _state.update { it.copy(currentVaultId = id, categoryFilter = null) }

    fun refresh() {
        viewModelScope.launch {
            _state.update { it.copy(loading = true, error = null) }
            try {
                runCatching { repository.seedLocalDemoIfEmpty() }
                val items = repository.list()
                val cats = runCatching { repository.categories() }.getOrDefault(emptyList())
                val vaults = runCatching { repository.vaults() }.getOrDefault(emptyList())
                _state.update { it.copy(loading = false, all = items, categories = cats, vaults = vaults) }
            } catch (e: Exception) {
                _state.update { it.copy(loading = false, error = e.message ?: "Could not load your vault.") }
            }
        }
    }

    fun toggleFavorite(id: Int) {
        viewModelScope.launch {
            runCatching { repository.toggleFavorite(id) }
            refresh()
        }
    }
}
