package com.vaultguard.app.data.repo

import com.vaultguard.app.data.model.ItemType

/** Editable fields for creating/updating a login item across both connection modes. */
data class LoginItemInput(
    val title: String,
    val description: String? = null,
    val type: ItemType = ItemType.Login,
    val isFavorite: Boolean = false,
    val username: String? = null,
    val email: String? = null,
    val website: String? = null,
    val loginUrl: String? = null,
    val password: String? = null,
    val totpSecret: String? = null,
    val notes: String? = null,
)

/** Sensitive fields decrypted on demand. */
data class ItemSecret(
    val password: String? = null,
    val totpSecret: String? = null,
)

sealed interface LoginResult {
    data object Success : LoginResult
    data class NeedsTwoFactor(val token: String?) : LoginResult
    data class Error(val message: String) : LoginResult
}
