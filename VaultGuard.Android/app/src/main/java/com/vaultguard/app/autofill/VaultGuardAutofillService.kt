package com.vaultguard.app.autofill

import android.app.PendingIntent
import android.app.assist.AssistStructure
import android.content.Intent
import android.os.CancellationSignal
import android.service.autofill.AutofillService
import android.service.autofill.Dataset
import android.service.autofill.FillCallback
import android.service.autofill.FillRequest
import android.service.autofill.FillResponse
import android.service.autofill.SaveCallback
import android.service.autofill.SaveInfo
import android.service.autofill.SaveRequest
import android.text.InputType
import android.view.autofill.AutofillId
import android.view.autofill.AutofillValue
import android.widget.RemoteViews
import com.vaultguard.app.MainActivity
import com.vaultguard.app.R
import com.vaultguard.app.data.model.VaultItem
import com.vaultguard.app.data.repo.LoginItemInput
import com.vaultguard.app.data.repo.SessionManager
import com.vaultguard.app.data.repo.VaultRepository
import dagger.hilt.android.AndroidEntryPoint
import kotlinx.coroutines.runBlocking
import kotlinx.coroutines.withTimeoutOrNull
import javax.inject.Inject

/**
 * System Autofill provider — lets VaultGuard fill usernames/passwords in other apps and browsers, so it
 * can be selected as the device's password manager (Settings → Passwords & autofill).
 *
 * When the vault is unlocked it offers matching items directly; when locked it offers a single
 * "Unlock VaultGuard" entry that opens the app.
 */
@AndroidEntryPoint
class VaultGuardAutofillService : AutofillService() {

    @Inject lateinit var repository: VaultRepository
    @Inject lateinit var session: SessionManager

    override fun onFillRequest(request: FillRequest, cancellationSignal: CancellationSignal, callback: FillCallback) {
        val structure = request.fillContexts.lastOrNull()?.structure
        if (structure == null) { callback.onSuccess(null); return }

        val parsed = parse(structure)
        if (parsed.usernameId == null && parsed.passwordId == null) { callback.onSuccess(null); return }

        val autofillIds = listOfNotNull(parsed.usernameId, parsed.passwordId).toTypedArray()
        val response = FillResponse.Builder()

        if (session.masterPassword == null) {
            // Locked → single authentication entry that opens the app to unlock.
            val intent = Intent(this, MainActivity::class.java)
            val pending = PendingIntent.getActivity(
                this, 1001, intent,
                PendingIntent.FLAG_IMMUTABLE or PendingIntent.FLAG_CANCEL_CURRENT,
            )
            response.setAuthentication(autofillIds, pending.intentSender, presentation("Unlock VaultGuard to autofill"))
            callback.onSuccess(response.build())
            return
        }

        val matches = runBlocking {
            withTimeoutOrNull(2500) { matchingItems(parsed.domain) } ?: emptyList()
        }

        var added = 0
        for (item in matches.take(8)) {
            val secret = runBlocking { withTimeoutOrNull(2500) { repository.secret(item.id) } }
            val username = item.username ?: item.email
            val label = item.title.ifBlank { username ?: "VaultGuard" }
            val row = presentation(label, username)
            val dataset = Dataset.Builder()
            var any = false
            // Fill the username field only when we actually have one — but never skip the whole item
            // just because a password-only field (e.g. a two-step login) has no username.
            parsed.usernameId?.let { uid ->
                if (!username.isNullOrBlank()) {
                    dataset.setValue(uid, AutofillValue.forText(username), row); any = true
                }
            }
            parsed.passwordId?.let { pid ->
                val pw = secret?.password
                if (!pw.isNullOrBlank()) {
                    dataset.setValue(pid, AutofillValue.forText(pw), row); any = true
                }
            }
            if (any) { response.addDataset(dataset.build()); added++ }
        }

        // "Quick Access" entry — opens a rich in-app picker (search + category filter, items
        // grouped by month, 1Password-style) for cases where the ranked matches above aren't it.
        val pickerIntent = Intent(this, AutofillPickerActivity::class.java).apply {
            putExtra(AutofillPickerActivity.EXTRA_USERNAME_ID, parsed.usernameId)
            putExtra(AutofillPickerActivity.EXTRA_PASSWORD_ID, parsed.passwordId)
            putExtra(AutofillPickerActivity.EXTRA_DOMAIN, parsed.domain)
        }
        val pickerPending = PendingIntent.getActivity(
            this, 1002, pickerIntent,
            PendingIntent.FLAG_MUTABLE or PendingIntent.FLAG_UPDATE_CURRENT,
        )
        val searchDataset = Dataset.Builder()
        parsed.usernameId?.let { searchDataset.setValue(it, null, presentation("Search VaultGuard…", null, android.R.drawable.ic_menu_search)) }
        parsed.passwordId?.let { searchDataset.setValue(it, null, presentation("Search VaultGuard…", null, android.R.drawable.ic_menu_search)) }
        searchDataset.setAuthentication(pickerPending.intentSender)
        response.addDataset(searchDataset.build())
        added++

        // Offer to save new credentials the user types.
        val saveIds = autofillIds
        if (saveIds.isNotEmpty()) {
            response.setSaveInfo(
                SaveInfo.Builder(SaveInfo.SAVE_DATA_TYPE_USERNAME or SaveInfo.SAVE_DATA_TYPE_PASSWORD, saveIds).build()
            )
        }

        callback.onSuccess(if (added > 0) response.build() else null)
    }

    override fun onSaveRequest(request: SaveRequest, callback: SaveCallback) {
        val structure = request.fillContexts.lastOrNull()?.structure
        if (structure == null || session.masterPassword == null) { callback.onSuccess(); return }

        val values = extractValues(structure)
        val username = values.username
        val password = values.password
        if (!username.isNullOrBlank() && !password.isNullOrBlank()) {
            runBlocking {
                runCatching {
                    repository.create(
                        LoginItemInput(
                            title = values.domain ?: username,
                            username = username,
                            password = password,
                            website = values.domain,
                        )
                    )
                }
            }
        }
        callback.onSuccess()
    }

    // ---- Structure parsing --------------------------------------------------

    private data class ParsedFields(val usernameId: AutofillId?, val passwordId: AutofillId?, val domain: String?)
    private data class SavedValues(val username: String?, val password: String?, val domain: String?)

    private fun parse(structure: AssistStructure): ParsedFields {
        var usernameId: AutofillId? = null
        var passwordId: AutofillId? = null
        var domain: String? = null
        for (i in 0 until structure.windowNodeCount) {
            traverse(structure.getWindowNodeAt(i).rootViewNode) { node ->
                node.webDomain?.takeIf { it.isNotBlank() }?.let { domain = it }
                val id = node.autofillId ?: return@traverse
                val hintStr = "${node.hint.orEmpty()} ${node.idEntry.orEmpty()}".lowercase()
                val hints = node.autofillHints?.map { it.lowercase() } ?: emptyList()
                val isPassword = hints.any { it.contains("password") } || isPasswordInput(node.inputType) || hintStr.contains("password")
                val isUsername = hints.any { it.contains("username") || it.contains("email") } ||
                    hintStr.contains("user") || hintStr.contains("email")
                if (isPassword && passwordId == null) passwordId = id
                else if (isUsername && usernameId == null) usernameId = id
            }
        }
        return ParsedFields(usernameId, passwordId, domain)
    }

    private fun extractValues(structure: AssistStructure): SavedValues {
        var username: String? = null
        var password: String? = null
        var domain: String? = null
        for (i in 0 until structure.windowNodeCount) {
            traverse(structure.getWindowNodeAt(i).rootViewNode) { node ->
                node.webDomain?.takeIf { it.isNotBlank() }?.let { domain = it }
                val text = node.autofillValue?.let { if (it.isText) it.textValue?.toString() else null } ?: node.text?.toString()
                if (text.isNullOrBlank()) return@traverse
                val hintStr = "${node.hint.orEmpty()} ${node.idEntry.orEmpty()}".lowercase()
                if (isPasswordInput(node.inputType) || hintStr.contains("password")) password = text
                else if (hintStr.contains("user") || hintStr.contains("email")) username = text
            }
        }
        return SavedValues(username, password, domain)
    }

    private fun traverse(node: AssistStructure.ViewNode, visit: (AssistStructure.ViewNode) -> Unit) {
        visit(node)
        for (i in 0 until node.childCount) traverse(node.getChildAt(i), visit)
    }

    private fun isPasswordInput(inputType: Int): Boolean {
        val variation = inputType and InputType.TYPE_MASK_VARIATION
        return variation == InputType.TYPE_TEXT_VARIATION_PASSWORD ||
            variation == InputType.TYPE_TEXT_VARIATION_VISIBLE_PASSWORD ||
            variation == InputType.TYPE_TEXT_VARIATION_WEB_PASSWORD
    }

    private suspend fun matchingItems(domain: String?): List<VaultItem> {
        val all = repository.list().filter { !it.isDeleted && !it.isArchived }
        val key = domain?.lowercase()?.removePrefix("www.") ?: return all.take(6)
        val core = key.substringBefore('.') // e.g. github.com -> github
        val matched = all.filter { item ->
            listOfNotNull(item.website, item.loginUrl, item.title)
                .any { it.lowercase().contains(core) || it.lowercase().contains(key) }
        }
        return matched.ifEmpty { all }
    }

    private fun presentation(
        title: String,
        subtitle: String? = null,
        iconRes: Int = android.R.drawable.ic_lock_idle_lock,
    ): RemoteViews =
        RemoteViews(packageName, R.layout.autofill_item).apply {
            setTextViewText(R.id.autofill_title, title)
            setImageViewResource(R.id.autofill_icon, iconRes)
            if (subtitle.isNullOrBlank()) {
                setViewVisibility(R.id.autofill_subtitle, android.view.View.GONE)
            } else {
                setTextViewText(R.id.autofill_subtitle, subtitle)
                setViewVisibility(R.id.autofill_subtitle, android.view.View.VISIBLE)
            }
        }
}
