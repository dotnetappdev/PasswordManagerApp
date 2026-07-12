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

/**
 * A user-defined custom field on an item (1Password-style). `secret` fields are masked in the UI.
 * In LOCAL mode these are persisted as a JSON array on the item row.
 */
@Serializable
data class CustomFieldData(
    val name: String,
    val value: String,
    val secret: Boolean = false,
    /**
     * WPF `CustomFieldType` code (1=Text … 13=SignInWith). Stored as an Int so the JSON stays
     * forward/backward compatible with older items that predate typed custom fields.
     */
    val type: Int = CustomFieldType.Text.code,
) {
    val fieldType: CustomFieldType get() = CustomFieldType.fromCode(type)
    /** Password/OTP fields (or an explicit secret flag) are masked in the UI. */
    val isMasked: Boolean get() = secret || fieldType.isSecret
}

/**
 * Mirrors the desktop `VaultGuard.Models.CustomFieldType` so mobile custom fields offer the exact same
 * field types as the WPF and Blazor forms (full CRUD parity).
 */
enum class CustomFieldType(val code: Int, val label: String) {
    Text(1, "Text"),
    Password(2, "Password"),
    Date(3, "Date"),
    Number(4, "Number"),
    Email(5, "Email"),
    Url(6, "URL"),
    TextArea(7, "Text area"),
    Phone(8, "Phone"),
    File(9, "File"),
    Toggle(10, "Toggle (Yes/No)"),
    Address(11, "Address"),
    OneTimePassword(12, "One-Time Password"),
    SignInWith(13, "Sign in with");

    /** Password and OTP values are always masked regardless of the per-field secret flag. */
    val isSecret: Boolean get() = this == Password || this == OneTimePassword

    /** Multi-line fields (address / free-text notes). */
    val isMultiline: Boolean get() = this == TextArea || this == Address

    companion object {
        fun fromCode(code: Int): CustomFieldType = entries.firstOrNull { it.code == code } ?: Text
    }
}

@Serializable
data class VaultDto(
    val id: Int = 0,
    val name: String = "",
    val description: String? = null,
    val color: String? = null,
    val icon: String? = null,
    val itemCount: Int = 0,
    @SerialName("passwordItemsCount") val passwordItemsCount: Int = 0,
    val isDefault: Boolean = false,
) {
    val displayCount: Int get() = if (itemCount > 0) itemCount else passwordItemsCount
}

@Serializable
data class CreateVaultDto(
    val name: String,
    val description: String? = null,
    val isDefault: Boolean = false,
    val icon: String? = null,
    val color: String? = null,
)

@Serializable
data class UpdateVaultDto(
    val name: String,
    val description: String? = null,
    val isDefault: Boolean = false,
    val icon: String? = null,
    val color: String? = null,
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

// QR login handoff — phone scans the desktop's QR and signs the desktop in.
@Serializable
data class QrAuthenticateRequest(
    val token: String,
    val email: String,
    val password: String,
    val deviceName: String? = null,
    val deviceType: String? = null,
    val platform: String? = null,
)

@Serializable
data class QrAuthenticateResponse(
    val success: Boolean = false,
    val message: String = "",
    val deviceName: String? = null,
)

@Serializable
data class RegisterDeviceRequest(val token: String, val platform: String = "Android")

// GitHub-style number-matching approvals
@Serializable
data class PendingApprovalDto(
    val id: String = "",
    val action: String = "",
    val choices: List<Int> = emptyList(),
    val expiresAt: String? = null,
)

@Serializable
data class RespondApprovalRequest(val selectedNumber: Int, val approve: Boolean)

@Serializable
data class ApprovalStateResponse(val state: String = "")

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
    val customFields: List<CustomFieldData> = emptyList(),
    /** Which vault this item lives in (LOCAL mode). Null in API mode (vault↔collection bridge). */
    val vaultId: Int? = null,
    /** Epoch millis the item was created; used to group items by month in quick-access/search UI. */
    val createdAt: Long = 0L,
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
            createdAt = parseIsoMillis(dto.createdAt),
        )

        private fun parseIsoMillis(iso: String?): Long =
            if (iso.isNullOrBlank()) 0L
            else runCatching { java.time.Instant.parse(iso).toEpochMilli() }.getOrDefault(0L)
    }
}
