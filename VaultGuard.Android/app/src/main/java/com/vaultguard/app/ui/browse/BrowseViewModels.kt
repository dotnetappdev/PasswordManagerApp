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
class VaultsViewModel @Inject constructor(private val repository: VaultRepository) : ViewModel() {
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

@HiltViewModel
class SecurityViewModel @Inject constructor(private val repository: VaultRepository) : ViewModel() {
    private val _stats = MutableStateFlow(SecurityStats())
    val stats = _stats.asStateFlow()

    init { load() }
    fun load() = viewModelScope.launch {
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
}
