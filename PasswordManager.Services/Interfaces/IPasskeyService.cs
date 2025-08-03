using PasswordManager.Models;
using PasswordManager.Models.DTOs.Auth;

namespace PasswordManager.Services.Interfaces;

/// <summary>
/// Service for managing WebAuthn Passkey operations
/// </summary>
public interface IPasskeyService
{
    /// <summary>
    /// Starts the passkey registration process
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="startDto">Registration start request</param>
    /// <returns>Challenge and credential creation options</returns>
    Task<PasskeyRegistrationStartResponseDto?> StartPasskeyRegistrationAsync(string userId, PasskeyRegistrationStartDto startDto);

    /// <summary>
    /// Completes the passkey registration process
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="completeDto">Registration completion data</param>
    /// <returns>True if registration was successful</returns>
    Task<bool> CompletePasskeyRegistrationAsync(string userId, PasskeyRegistrationCompleteDto completeDto);

    /// <summary>
    /// Starts the passkey authentication process
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <returns>Challenge and credential request options</returns>
    Task<PasskeyAuthenticationStartResponseDto?> StartPasskeyAuthenticationAsync(string email);

    /// <summary>
    /// Completes the passkey authentication process
    /// </summary>
    /// <param name="completeDto">Authentication completion data</param>
    /// <returns>Authentication response with tokens</returns>
    Task<AuthResponseDto?> CompletePasskeyAuthenticationAsync(PasskeyAuthenticationCompleteDto completeDto);

    /// <summary>
    /// Gets all passkeys for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>List of user's passkeys</returns>
    Task<PasskeyListResponseDto> GetUserPasskeysAsync(string userId);

    /// <summary>
    /// Deletes a passkey for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="passkeyId">The passkey ID to delete</param>
    /// <param name="deleteDto">Delete request with master password</param>
    /// <returns>True if deletion was successful</returns>
    Task<bool> DeletePasskeyAsync(string userId, int passkeyId, PasskeyDeleteDto deleteDto);

    /// <summary>
    /// Gets the passkey status for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>Passkey status information</returns>
    Task<PasskeyStatusDto> GetPasskeyStatusAsync(string userId);

    /// <summary>
    /// Updates passkey settings for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="storeInVault">Whether to store passkeys in vault</param>
    /// <param name="masterPassword">Master password for verification</param>
    /// <returns>True if update was successful</returns>
    Task<bool> UpdatePasskeySettingsAsync(string userId, bool storeInVault, string masterPassword);

    /// <summary>
    /// Stores a passkey in the encrypted password vault
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="passkeyId">The passkey ID</param>
    /// <param name="passkeyData">The passkey data to encrypt and store</param>
    /// <returns>True if storage was successful</returns>
    Task<bool> StorePasskeyInVaultAsync(string userId, int passkeyId, object passkeyData);

    /// <summary>
    /// Retrieves a passkey from the encrypted password vault
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="passkeyId">The passkey ID</param>
    /// <returns>Decrypted passkey data</returns>
    Task<object?> RetrievePasskeyFromVaultAsync(string userId, int passkeyId);

    /// <summary>
    /// Verifies a WebAuthn assertion
    /// </summary>
    /// <param name="credentialId">The credential ID</param>
    /// <param name="clientDataJson">Client data JSON</param>
    /// <param name="authenticatorData">Authenticator data</param>
    /// <param name="signature">Assertion signature</param>
    /// <param name="challenge">Original challenge</param>
    /// <returns>True if assertion is valid</returns>
    Task<bool> VerifyPasskeyAssertionAsync(string credentialId, string clientDataJson, string authenticatorData, string signature, string challenge);

    /// <summary>
    /// Generates a new WebAuthn challenge
    /// </summary>
    /// <returns>Base64-encoded challenge</returns>
    string GenerateChallenge();

    /// <summary>
    /// Stores a challenge temporarily for verification
    /// </summary>
    /// <param name="challenge">The challenge to store</param>
    /// <param name="userId">Associated user ID</param>
    /// <param name="expiryMinutes">Challenge expiry time in minutes</param>
    /// <returns>True if storage was successful</returns>
    Task<bool> StoreChallengeAsync(string challenge, string userId, int expiryMinutes = 5);

    /// <summary>
    /// Verifies and removes a stored challenge
    /// </summary>
    /// <param name="challenge">The challenge to verify</param>
    /// <param name="userId">Associated user ID</param>
    /// <returns>True if challenge is valid</returns>
    Task<bool> VerifyAndRemoveChallengeAsync(string challenge, string userId);
}