package com.vaultguard.app.ui.edit

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.vaultguard.app.config.SettingsStore
import com.vaultguard.app.data.model.ItemType
import com.vaultguard.app.data.repo.LoginItemInput
import com.vaultguard.app.data.repo.VaultRepository
import com.vaultguard.app.domain.PasswordGenerator
import com.vaultguard.app.domain.PasswordOptions
import com.vaultguard.app.domain.Totp
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import javax.inject.Inject

data class ItemEditState(
    val isNew: Boolean = true,
    val loading: Boolean = false,
    val saving: Boolean = false,
    val saved: Boolean = false,
    val error: String? = null,
    val title: String = "",
    val description: String = "",
    val username: String = "",
    val email: String = "",
    val website: String = "",
    val loginUrl: String = "",
    val password: String = "",
    val totpSecret: String = "",
    val notes: String = "",
    val isFavorite: Boolean = false,
    val passwordStrength: Int = 0,
    val categories: List<com.vaultguard.app.data.model.CategoryDto> = emptyList(),
    val selectedCategory: String? = null,
    val customFields: List<com.vaultguard.app.data.model.CustomFieldData> = emptyList(),
)

@HiltViewModel
class ItemEditViewModel @Inject constructor(
    private val repository: VaultRepository,
    private val generator: PasswordGenerator,
    private val settingsStore: SettingsStore,
) : ViewModel() {

    private val _state = MutableStateFlow(ItemEditState())
    val state = _state.asStateFlow()

    private var editingId: Int = -1

    fun load(id: Int) {
        editingId = id
        viewModelScope.launch {
            val cats = runCatching { repository.categories() }.getOrDefault(emptyList())
            if (id == -1) {
                _state.update { ItemEditState(isNew = true, categories = cats) }
                return@launch
            }
            _state.update { it.copy(isNew = false, loading = true, categories = cats) }
            try {
                val item = repository.get(id)
                _state.update {
                    it.copy(
                        loading = false,
                        title = item?.title ?: "",
                        description = item?.description ?: "",
                        username = item?.username ?: "",
                        email = item?.email ?: "",
                        website = item?.website ?: "",
                        loginUrl = item?.loginUrl ?: "",
                        notes = item?.notes ?: "",
                        isFavorite = item?.isFavorite ?: false,
                        selectedCategory = item?.categoryName,
                        customFields = item?.customFields ?: emptyList(),
                    )
                }
            } catch (e: Exception) {
                _state.update { it.copy(loading = false, error = e.message) }
            }
        }
    }

    fun setCategory(name: String?) = _state.update { it.copy(selectedCategory = name) }
    fun setTitle(v: String) = _state.update { it.copy(title = v) }
    fun setDescription(v: String) = _state.update { it.copy(description = v) }
    fun setUsername(v: String) = _state.update { it.copy(username = v) }
    fun setEmail(v: String) = _state.update { it.copy(email = v) }
    fun setWebsite(v: String) = _state.update { it.copy(website = v) }
    fun setLoginUrl(v: String) = _state.update { it.copy(loginUrl = v) }
    fun setPassword(v: String) = _state.update { it.copy(password = v, passwordStrength = generator.strength(v)) }

    fun generatePassword() {
        viewModelScope.launch {
            val s = settingsStore.settings.first()
            val pwd = generator.generate(
                PasswordOptions(
                    length = s.pwLength,
                    upper = s.pwUpper,
                    lower = s.pwLower,
                    digits = s.pwDigits,
                    symbols = s.pwSymbols,
                    avoidAmbiguous = s.pwAvoidAmbiguous,
                )
            )
            _state.update { it.copy(password = pwd, passwordStrength = generator.strength(pwd)) }
        }
    }
    fun setTotp(v: String) = _state.update { it.copy(totpSecret = v) }
    fun setNotes(v: String) = _state.update { it.copy(notes = v) }
    fun setFavorite(v: Boolean) = _state.update { it.copy(isFavorite = v) }

    // ---- Custom fields -----------------------------------------------------

    fun addCustomField() = _state.update {
        it.copy(customFields = it.customFields + com.vaultguard.app.data.model.CustomFieldData(name = "", value = ""))
    }

    fun setCustomFieldName(index: Int, name: String) = _state.update { s ->
        s.copy(customFields = s.customFields.mapIndexed { i, f -> if (i == index) f.copy(name = name) else f })
    }

    fun setCustomFieldValue(index: Int, value: String) = _state.update { s ->
        s.copy(customFields = s.customFields.mapIndexed { i, f -> if (i == index) f.copy(value = value) else f })
    }

    fun toggleCustomFieldSecret(index: Int) = _state.update { s ->
        s.copy(customFields = s.customFields.mapIndexed { i, f -> if (i == index) f.copy(secret = !f.secret) else f })
    }

    fun setCustomFieldType(index: Int, type: com.vaultguard.app.data.model.CustomFieldType) = _state.update { s ->
        s.copy(customFields = s.customFields.mapIndexed { i, f ->
            // Password/OTP types imply a masked value; keep the explicit secret flag for other types.
            if (i == index) f.copy(type = type.code, secret = f.secret || type.isSecret) else f
        })
    }

    fun removeCustomField(index: Int) = _state.update { s ->
        s.copy(customFields = s.customFields.filterIndexed { i, _ -> i != index })
    }

    /** Called when the QR scanner returns; accepts a raw secret or an otpauth:// URI. */
    fun applyScannedTotp(scanned: String) =
        _state.update { it.copy(totpSecret = Totp.secretFromUri(scanned)) }

    fun save() {
        val s = _state.value
        if (s.title.isBlank()) {
            _state.update { it.copy(error = "Title is required.") }
            return
        }
        viewModelScope.launch {
            _state.update { it.copy(saving = true, error = null) }
            val input = LoginItemInput(
                title = s.title.trim(),
                description = s.description.ifBlank { null },
                type = ItemType.Login,
                isFavorite = s.isFavorite,
                username = s.username.ifBlank { null },
                email = s.email.ifBlank { null },
                website = s.website.ifBlank { null },
                loginUrl = s.loginUrl.ifBlank { null },
                password = s.password.ifBlank { null },
                totpSecret = s.totpSecret.ifBlank { null },
                notes = s.notes.ifBlank { null },
                categoryId = s.categories.firstOrNull { it.name == s.selectedCategory }?.id,
                categoryName = s.selectedCategory,
                customFields = s.customFields
                    .map { it.copy(name = it.name.trim(), value = it.value.trim()) }
                    .filter { it.name.isNotEmpty() || it.value.isNotEmpty() },
            )
            try {
                if (s.isNew) repository.create(input) else repository.update(editingId, input)
                _state.update { it.copy(saving = false, saved = true) }
            } catch (e: Exception) {
                _state.update { it.copy(saving = false, error = e.message ?: "Could not save.") }
            }
        }
    }
}
