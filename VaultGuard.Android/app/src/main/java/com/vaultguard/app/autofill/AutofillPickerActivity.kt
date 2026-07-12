package com.vaultguard.app.autofill

import android.app.Activity
import android.content.Intent
import android.os.Bundle
import android.service.autofill.Dataset
import android.view.autofill.AutofillId
import android.view.autofill.AutofillManager
import android.view.autofill.AutofillValue
import android.widget.RemoteViews
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Close
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.ViewModel
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewModelScope
import com.vaultguard.app.R
import com.vaultguard.app.data.model.CategoryDto
import com.vaultguard.app.data.model.VaultItem
import com.vaultguard.app.data.repo.VaultRepository
import com.vaultguard.app.ui.home.CategoryCombobox
import com.vaultguard.app.ui.home.CenterText
import com.vaultguard.app.ui.home.MonthGroupedList
import com.vaultguard.app.ui.home.SearchField
import com.vaultguard.app.ui.theme.VaultGuardTheme
import dagger.hilt.android.AndroidEntryPoint
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import javax.inject.Inject

/**
 * The Autofill "Search VaultGuard…" entry opens this: a 1Password-style quick-access picker
 * (category filter, search field, items grouped by month) reused from the Home search tab.
 * Picking an item hands the credential back to the Autofill framework via
 * [AutofillManager.EXTRA_AUTHENTICATION_RESULT] instead of just opening the main app.
 */
@AndroidEntryPoint
class AutofillPickerActivity : ComponentActivity() {

    companion object {
        const val EXTRA_USERNAME_ID = "username_id"
        const val EXTRA_PASSWORD_ID = "password_id"
        const val EXTRA_DOMAIN = "domain"
    }

    @OptIn(ExperimentalMaterial3Api::class)
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        val usernameId = intent.getParcelableExtra(EXTRA_USERNAME_ID, AutofillId::class.java)
        val passwordId = intent.getParcelableExtra(EXTRA_PASSWORD_ID, AutofillId::class.java)

        setContent {
            val viewModel: AutofillPickerViewModel = hiltViewModel()
            val state by viewModel.state.collectAsStateWithLifecycle()
            val scope = rememberCoroutineScope()

            VaultGuardTheme {
                Surface(Modifier.fillMaxSize(), color = MaterialTheme.colorScheme.background) {
                    Column(Modifier.fillMaxSize()) {
                        TopAppBar(
                            title = { Text("Quick Access", fontWeight = FontWeight.SemiBold) },
                            navigationIcon = {
                                IconButton(onClick = { setResult(Activity.RESULT_CANCELED); finish() }) {
                                    Icon(Icons.Filled.Close, "Close")
                                }
                            },
                        )
                        if (state.categories.isNotEmpty()) {
                            CategoryCombobox(state.categories, state.categoryFilter, viewModel::setCategory)
                        }
                        SearchField(state.query, viewModel::setQuery)
                        val results = state.all
                            .filter { state.categoryFilter == null || it.categoryName == state.categoryFilter }
                            .filter {
                                state.query.isBlank() ||
                                    it.title.contains(state.query, true) ||
                                    it.username?.contains(state.query, true) == true ||
                                    it.website?.contains(state.query, true) == true
                            }
                        when {
                            state.loading -> Box(Modifier.fillMaxSize(), Alignment.Center) { CircularProgressIndicator() }
                            results.isEmpty() -> CenterText("No matching items.", MaterialTheme.colorScheme.onSurfaceVariant)
                            else -> MonthGroupedList(
                                items = results,
                                onOpenItem = { id -> scope.launch { fillAndFinish(viewModel, id, usernameId, passwordId) } },
                                onToggleFavorite = {},
                            )
                        }
                    }
                }
            }
        }
    }

    private suspend fun fillAndFinish(
        viewModel: AutofillPickerViewModel,
        itemId: Int,
        usernameId: AutofillId?,
        passwordId: AutofillId?,
    ) {
        val item = viewModel.state.value.all.firstOrNull { it.id == itemId }
        val secret = runCatching { viewModel.secretFor(itemId) }.getOrNull()
        val username = item?.username ?: item?.email
        val label = item?.title?.ifBlank { username ?: "VaultGuard" } ?: "VaultGuard"
        val row = RemoteViews(packageName, R.layout.autofill_item).apply {
            setTextViewText(R.id.autofill_title, label)
            setImageViewResource(R.id.autofill_icon, android.R.drawable.ic_lock_idle_lock)
            if (username.isNullOrBlank()) {
                setViewVisibility(R.id.autofill_subtitle, android.view.View.GONE)
            } else {
                setTextViewText(R.id.autofill_subtitle, username)
                setViewVisibility(R.id.autofill_subtitle, android.view.View.VISIBLE)
            }
        }

        val dataset = Dataset.Builder()
        var any = false
        usernameId?.let { if (!username.isNullOrBlank()) { dataset.setValue(it, AutofillValue.forText(username), row); any = true } }
        passwordId?.let { val pw = secret?.password; if (!pw.isNullOrBlank()) { dataset.setValue(it, AutofillValue.forText(pw), row); any = true } }

        if (any) {
            val replyIntent = Intent().putExtra(AutofillManager.EXTRA_AUTHENTICATION_RESULT, dataset.build())
            setResult(Activity.RESULT_OK, replyIntent)
        } else {
            setResult(Activity.RESULT_CANCELED)
        }
        finish()
    }
}

data class PickerState(
    val loading: Boolean = true,
    val all: List<VaultItem> = emptyList(),
    val categories: List<CategoryDto> = emptyList(),
    val categoryFilter: String? = null,
    val query: String = "",
)

@HiltViewModel
class AutofillPickerViewModel @Inject constructor(
    private val repository: VaultRepository,
) : ViewModel() {

    private val _state = MutableStateFlow(PickerState())
    val state = _state.asStateFlow()

    init {
        viewModelScope.launch {
            val items = runCatching { repository.list() }.getOrDefault(emptyList()).filter { !it.isArchived && !it.isDeleted }
            val cats = runCatching { repository.categories() }.getOrDefault(emptyList())
            _state.update { it.copy(loading = false, all = items, categories = cats) }
        }
    }

    fun setQuery(q: String) = _state.update { it.copy(query = q) }
    fun setCategory(c: String?) = _state.update { it.copy(categoryFilter = c) }

    suspend fun secretFor(id: Int) = repository.secret(id)
}
