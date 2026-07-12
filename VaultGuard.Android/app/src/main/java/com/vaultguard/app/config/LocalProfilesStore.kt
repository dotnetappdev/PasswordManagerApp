package com.vaultguard.app.config

import android.content.Context
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.stringPreferencesKey
import androidx.datastore.preferences.preferencesDataStore
import dagger.hilt.android.qualifiers.ApplicationContext
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.flow.map
import kotlinx.serialization.Serializable
import kotlinx.serialization.decodeFromString
import kotlinx.serialization.encodeToString
import kotlinx.serialization.json.Json
import javax.inject.Inject
import javax.inject.Singleton

private val Context.localProfilesDataStore by preferencesDataStore(name = "vaultguard_local_profiles")

/**
 * A local (on-device) vault profile the user can pick from on the unlock screen — the LOCAL-mode
 * equivalent of the WPF app's user accounts. Each profile has its own encryption key + verifier (keyed by
 * [id] in [SecureStore]) and its own items (namespaced by [id] in the Room DB), so profiles are isolated.
 */
@Serializable
data class LocalProfile(
    val id: String,
    val name: String,
    val role: String = "User",
    val email: String? = null,
    val colorArgb: Long = 0xFF2563EBL,
) {
    val initials: String get() = name.trim().ifEmpty { role }.take(1).uppercase()
}

/**
 * Stores the list of local vault profiles + which one is active. Seeds the same four demo accounts the WPF
 * app shows (admin / parent / user / child) so a fresh install presents a familiar account picker; each is
 * created on first unlock with whatever master password is entered (the demo key 7hm3Z!Csu:Y64nm works).
 */
@Singleton
class LocalProfilesStore @Inject constructor(
    @ApplicationContext private val context: Context,
) {
    private object Keys {
        val LIST = stringPreferencesKey("profiles_json")
        val ACTIVE = stringPreferencesKey("active_profile_id")
    }
    private val json = Json { ignoreUnknownKeys = true }

    val profiles: Flow<List<LocalProfile>> = context.localProfilesDataStore.data.map { prefs ->
        prefs[Keys.LIST]?.let { runCatching { json.decodeFromString<List<LocalProfile>>(it) }.getOrNull() }
            ?: DEFAULTS
    }

    val activeId: Flow<String?> = context.localProfilesDataStore.data.map { it[Keys.ACTIVE] }

    suspend fun list(): List<LocalProfile> = profiles.first()

    /** Ensure the default demo profiles exist on first run. */
    suspend fun seedDefaultsIfEmpty() {
        context.localProfilesDataStore.edit { prefs ->
            if (prefs[Keys.LIST].isNullOrBlank()) prefs[Keys.LIST] = json.encodeToString(DEFAULTS)
        }
    }

    suspend fun select(id: String) {
        context.localProfilesDataStore.edit { it[Keys.ACTIVE] = id }
    }

    /** Add or update a profile (matched by id) and mark it active. */
    suspend fun upsert(profile: LocalProfile) {
        val current = list().filterNot { it.id == profile.id } + profile
        context.localProfilesDataStore.edit { prefs ->
            prefs[Keys.LIST] = json.encodeToString(current)
            prefs[Keys.ACTIVE] = profile.id
        }
    }

    suspend fun remove(id: String) {
        val remaining = list().filterNot { it.id == id }
        context.localProfilesDataStore.edit { prefs ->
            prefs[Keys.LIST] = json.encodeToString(remaining)
            if (prefs[Keys.ACTIVE] == id) prefs.remove(Keys.ACTIVE)
        }
    }

    /** Wipe every local profile and the active selection, so the picker re-seeds the demo accounts. */
    suspend fun clearAll() {
        context.localProfilesDataStore.edit { prefs ->
            prefs.remove(Keys.LIST)
            prefs.remove(Keys.ACTIVE)
        }
    }

    companion object {
        val DEFAULTS = listOf(
            LocalProfile("admin", "Administrator", "Admin", "admin@passwordmanager.local", 0xFF7C3AEDL),
            LocalProfile("parent", "Parent", "Parent", "parent@passwordmanager.local", 0xFF2563EBL),
            LocalProfile("user", "Regular User", "User", "user@passwordmanager.local", 0xFF059669L),
            LocalProfile("child", "Child", "Child", "child@passwordmanager.local", 0xFFEC4899L),
        )
    }
}
