package com.vaultguard.app.ui.browse

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.vaultguard.app.data.model.CategoryDto
import com.vaultguard.app.data.model.ItemType
import com.vaultguard.app.data.model.VaultDto
import com.vaultguard.app.data.repo.VaultRepository
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltViewModel
class VaultsViewModel @Inject constructor(
    private val repository: VaultRepository,
    private val toaster: com.vaultguard.app.ui.common.Toaster,
) : ViewModel() {
    private val _vaults = MutableStateFlow<List<VaultDto>>(emptyList())
    val vaults = _vaults.asStateFlow()
    private val _loading = MutableStateFlow(true)
    val loading = _loading.asStateFlow()

    init { load() }
    fun load() = viewModelScope.launch {
        _loading.value = true
        _vaults.value = runCatching { repository.vaults() }.getOrDefault(emptyList())
        _loading.value = false
    }

    fun create(name: String, description: String?) = mutate { repository.createVault(name, description) }
    fun update(id: Int, name: String, description: String?) = mutate { repository.updateVault(id, name, description) }
    fun delete(id: Int) = mutate { repository.deleteVault(id) }

    private fun mutate(block: suspend () -> Result<Unit>) = viewModelScope.launch {
        block()
            .onSuccess { toaster.show("Vault saved."); load() }
            .onFailure { toaster.show(it.message ?: "Could not update vault.") }
    }
}

@HiltViewModel
class CategoriesViewModel @Inject constructor(private val repository: VaultRepository) : ViewModel() {
    private val _categories = MutableStateFlow<List<CategoryDto>>(emptyList())
    val categories = _categories.asStateFlow()
    private val _loading = MutableStateFlow(true)
    val loading = _loading.asStateFlow()

    init { load() }
    fun load() = viewModelScope.launch {
        _loading.value = true
        _categories.value = runCatching { repository.categories() }.getOrDefault(emptyList())
        _loading.value = false
    }
}

data class SecurityStats(
    val total: Int = 0,
    val perType: Map<ItemType, Int> = emptyMap(),
    val favorites: Int = 0,
    val archived: Int = 0,
    val deleted: Int = 0,
    val loading: Boolean = true,
)

/** A password found in a known breach. */
data class BreachHit(val id: Int, val title: String, val timesSeen: Int)

/** State of the on-demand Have I Been Pwned scan. */
sealed interface BreachScan {
    data object Idle : BreachScan
    data object Scanning : BreachScan
    data class Done(val compromised: List<BreachHit>, val someFailed: Boolean) : BreachScan
}

@HiltViewModel
class SecurityViewModel @Inject constructor(
    private val repository: VaultRepository,
    private val breachChecker: com.vaultguard.app.domain.BreachChecker,
) : ViewModel() {
    private val _stats = MutableStateFlow(SecurityStats())
    val stats = _stats.asStateFlow()

    private val _breach = MutableStateFlow<BreachScan>(BreachScan.Idle)
    val breach = _breach.asStateFlow()

    init { load() }
    fun load() = viewModelScope.launch {
        _breach.value = BreachScan.Idle
        val items = runCatching { repository.list() }.getOrDefault(emptyList())
        val active = items.filter { !it.isDeleted }
        _stats.update {
            SecurityStats(
                total = active.count { !it.isArchived },
                perType = active.filter { !it.isArchived }.groupingBy { it.type }.eachCount(),
                favorites = active.count { it.isFavorite && !it.isArchived },
                archived = active.count { it.isArchived },
                deleted = items.count { it.isDeleted },
                loading = false,
            )
        }
    }

    /** Check every stored password against known breaches (k-anonymity). Network call, hence on-demand. */
    fun scanBreaches() = viewModelScope.launch {
        _breach.value = BreachScan.Scanning
        val items = runCatching { repository.list() }.getOrDefault(emptyList())
            .filter { !it.isDeleted && !it.isArchived }
        val hits = mutableListOf<BreachHit>()
        var someFailed = false
        val cache = HashMap<String, Int?>()
        for (item in items) {
            val pwd = runCatching { repository.secret(item.id).password }.getOrNull()
            if (pwd.isNullOrEmpty()) continue
            val count = if (cache.containsKey(pwd)) cache[pwd] else breachChecker.timesSeen(pwd).also { cache[pwd] = it }
            when {
                count == null -> someFailed = true
                count > 0 -> hits.add(BreachHit(item.id, item.title, count))
            }
        }
        _breach.value = BreachScan.Done(hits.sortedByDescending { it.timesSeen }, someFailed)
    }
}
