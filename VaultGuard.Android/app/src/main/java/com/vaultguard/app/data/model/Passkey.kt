package com.vaultguard.app.data.model

import kotlinx.serialization.Serializable

@Serializable
data class PasskeyDto(
    val id: Int = 0,
    val name: String = "",
    val deviceType: String? = null,
    val isBackedUp: Boolean = false,
    val requiresUserVerification: Boolean = false,
    val createdAt: String? = null,
    val lastUsedAt: String? = null,
    val isActive: Boolean = true,
    val storeInVault: Boolean = false,
)

@Serializable
data class PasskeyListResponse(
    val passkeys: List<PasskeyDto> = emptyList(),
    val passkeysEnabled: Boolean = false,
    val passkeysEnabledAt: String? = null,
)

@Serializable
data class PasskeyStatus(
    val isEnabled: Boolean = false,
    val enabledAt: String? = null,
    val passkeyCount: Int = 0,
    val storeInVault: Boolean = false,
)

@Serializable
data class PasskeyRegistrationStartRequest(
    val masterPassword: String,
    val passkeyName: String,
    val storeInVault: Boolean = true,
)

@Serializable
data class PasskeyRegistrationStartResponse(
    val challenge: String = "",
    val credentialCreationOptions: String = "",
)

@Serializable
data class PasskeyRegistrationComplete(
    val challenge: String,
    val credentialResponse: String,
    val originalOptionsJson: String,
    val passkeyName: String,
    val storeInVault: Boolean = true,
    val deviceType: String? = "Android",
)

@Serializable
data class PasskeyDeleteRequest(val masterPassword: String)
