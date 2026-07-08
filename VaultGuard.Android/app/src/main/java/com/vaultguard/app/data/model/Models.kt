package com.vaultguard.app.data.model

import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable

/**
 * Item types mirror VaultGuard.Models.ItemType (serialized as an integer over the wire by the API).
 */
enum class ItemType(val code: Int, val label: String) {
    Login(1, "Login"),
    CreditCard(2, "Credit Card"),
    SecureNote(3, "Secure Note"),
    WiFi(4, "Wi-Fi"),
    Password(5, "Password"),
    Passkey(6, "Passkey"),
    Identity(7, "Identity");

    companion object {
        fun fromCode(code: Int): ItemType = entries.firstOrNull { it.code == code } ?: Login
    }
}

// ---------------------------------------------------------------------------
// Auth DTOs — POST /api/auth/login/enhanced
// ---------------------------------------------------------------------------

@Serializable
data class EnhancedLoginRequest(
    val email: String,
    val password: String,
    val twoFactorCode: String? = null,
    val isTwoFactorBackupCode: Boolean = false,
)

@Serializable
data class LoginResponse(
    val requiresTwoFactor: Boolean = false,
    val supportsPasskey: Boolean = false,
    val twoFactorToken: String? = null,
    val authResponse: AuthResponse? = null,
)

@Serializable
data class AuthResponse(
    val token: String = "",
    val refreshToken: String = "",
    val expiresAt: String? = null,
    val user: UserDto? = null,
)

@Serializable
data class UserDto(
    val id: String = "",
    val email: String = "",
    val firstName: String? = null,
    val lastName: String? = null,
)

// ---------------------------------------------------------------------------
// Password item DTOs
// ---------------------------------------------------------------------------

@Serializable
data class PasswordItemDto(
    val id: Int = 0,
    val title: String = "",
    val description: String? = null,
    val type: Int = 1,
    val createdAt: String? = null,
    val lastModified: String? = null,
    val isFavorite: Boolean = false,
    val isArchived: Boolean = false,
    val isDeleted: Boolean = false,
    val userId: String? = null,
    val categoryId: Int? = null,
    val collectionId: Int? = null,
    val category: CategoryDto? = null,
    val loginItem: LoginItemDto? = null,
    val tags: List<TagDto> = emptyList(),
) {
    val itemType: ItemType get() = ItemType.fromCode(type)
}

@Serializable
data class LoginItemDto(
    val id: Int = 0,
    val website: String? = null,
    val username: String? = null,
    val email: String? = null,
    val phoneNumber: String? = null,
    val totpSecret: String? = null,
    val loginUrl: String? = null,
    val notes: String? = null,
    // Present only on the /decrypt response
    val password: String? = null,
)

@Serializable
data class CategoryDto(
    val id: Int = 0,
    val name: String = "",
    val color: String? = null,
)

@Serializable
data class TagDto(
    val id: Int = 0,
    val name: String = "",
    val color: String? = null,
)

@Serializable
data class VaultDto(
    val id: Int = 0,
    val name: String = "",
    val description: String? = null,
    val color: String? = null,
    val icon: String? = null,
    val itemCount: Int = 0,
    val isDefault: Boolean = false,
)

// Create (server-side encryption) — POST /api/passworditems/encrypted
@Serializable
data class CreateEncryptedPasswordItem(
    val title: String,
    val description: String? = null,
    val type: Int = 1,
    val isFavorite: Boolean = false,
    val isArchived: Boolean = false,
    val categoryId: Int? = null,
    val collectionId: Int? = null,
    val masterPassword: String,
    val loginItem: CreateLoginItem? = null,
    val tagIds: List<Int> = emptyList(),
)

@Serializable
data class CreateLoginItem(
    val website: String? = null,
    val username: String? = null,
    val email: String? = null,
    val password: String? = null,
    val phoneNumber: String? = null,
    val totpSecret: String? = null,
    val loginUrl: String? = null,
    val notes: String? = null,
)

@Serializable
data class UpdatePasswordItem(
    val title: String? = null,
    val description: String? = null,
    val isFavorite: Boolean? = null,
    val isArchived: Boolean = false,
    val categoryId: Int? = null,
    val collectionId: Int? = null,
    val loginItem: UpdateLoginItem? = null,
    val tagIds: List<Int> = emptyList(),
)

@Serializable
data class UpdateLoginItem(
    val website: String? = null,
    val username: String? = null,
    val email: String? = null,
    val password: String? = null,
    val phoneNumber: String? = null,
    val totpSecret: String? = null,
    val loginUrl: String? = null,
    val notes: String? = null,
)

// Session-based decrypt — POST /api/passworditems/{id}/decrypt (returns password + TOTP + fields)
@Serializable
data class DecryptedPasswordItemDto(
    val id: Int = 0,
    val title: String = "",
    val loginItem: DecryptedLoginItemDto? = null,
)

@Serializable
data class DecryptedLoginItemDto(
    val username: String? = null,
    val password: String? = null,
    val email: String? = null,
    val website: String? = null,
    val totpSecret: String? = null,
    val loginUrl: String? = null,
    val notes: String? = null,
)

@Serializable
data class RevealPasswordRequest(val masterPassword: String)

@Serializable
data class RevealPasswordResponse(
    val password: String = "",
    val itemId: Int = 0,
    @SerialName("revealedAt") val revealedAt: String? = null,
)

/**
 * A decrypted, presentation-ready view of a vault item used across both connection modes.
 * In API mode this is filled from PasswordItemDto + a reveal/decrypt call; in local mode it is
 * decrypted on-device.
 */
data class VaultItem(
    val id: Int,
    val title: String,
    val description: String?,
    val type: ItemType,
    val isFavorite: Boolean,
    val isArchived: Boolean,
    val isDeleted: Boolean,
    val username: String?,
    val email: String?,
    val website: String?,
    val loginUrl: String?,
    val totpSecret: String?,
    val notes: String?,
    val categoryName: String?,
    val tags: List<String> = emptyList(),
) {
    companion object {
        fun from(dto: PasswordItemDto) = VaultItem(
            id = dto.id,
            title = dto.title,
            description = dto.description,
            type = dto.itemType,
            isFavorite = dto.isFavorite,
            isArchived = dto.isArchived,
            isDeleted = dto.isDeleted,
            username = dto.loginItem?.username,
            email = dto.loginItem?.email,
            website = dto.loginItem?.website,
            loginUrl = dto.loginItem?.loginUrl,
            totpSecret = dto.loginItem?.totpSecret,
            notes = dto.loginItem?.notes,
            categoryName = dto.category?.name,
            tags = dto.tags.map { it.name },
        )
    }
}
