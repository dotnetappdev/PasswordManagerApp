package com.vaultguard.app.ui.home

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.vaultguard.app.data.model.UserDto
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
    val section: VaultSection = VaultSection.AllItems,
    val query: String = "",
    val user: UserDto? = null,
    val error: String? = null,
) {
    val visible: List<VaultItem>
        get() = all.filter { section.matches(it) }.filter {
            query.isBlank() ||
                it.title.contains(query, true) ||
                it.username?.contains(query, true) == true ||
                it.website?.contains(query, true) == true
        }

    fun count(section: VaultSection): Int = all.count { section.matches(it) }
}

@HiltViewModel
class HomeViewModel @Inject constructor(
    private val repository: VaultRepository,
    session: SessionManager,
) : ViewModel() {

    private val _state = MutableStateFlow(HomeState(user = session.user.value))
    val state = _state.asStateFlow()

    init { refresh() }

    fun setSection(section: VaultSection) = _state.update { it.copy(section = section) }
    fun setQuery(q: String) = _state.update { it.copy(query = q) }

    fun refresh() {
        viewModelScope.launch {
            _state.update { it.copy(loading = true, error = null) }
            try {
                runCatching { repository.seedLocalDemoIfEmpty() }
                val items = repository.list()
                _state.update { it.copy(loading = false, all = items) }
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
