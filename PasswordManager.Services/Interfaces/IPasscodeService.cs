using PasswordManager.Models;

namespace PasswordManager.Services.Interfaces;

/// <summary>
/// Service for managing app passcode authentication (8-digit PIN)
/// </summary>
public interface IPasscodeService
{
    /// <summary>
    /// Checks if a passcode has been set up
    /// </summary>
    /// <returns>True if passcode is configured</returns>
    Task<bool> IsPasscodeSetAsync();

    /// <summary>
    /// Sets up a new 8-digit passcode
    /// </summary>
    /// <param name="passcode">8-digit passcode</param>
    /// <returns>True if setup was successful</returns>
    Task<bool> SetPasscodeAsync(string passcode);

    /// <summary>
    /// Verifies the provided passcode
    /// </summary>
    /// <param name="passcode">8-digit passcode to verify</param>
    /// <returns>True if passcode is correct</returns>
    Task<bool> VerifyPasscodeAsync(string passcode);

    /// <summary>
    /// Removes the configured passcode
    /// </summary>
    /// <returns>True if removal was successful</returns>
    Task<bool> RemovePasscodeAsync();

    /// <summary>
    /// Changes the existing passcode
    /// </summary>
    /// <param name="currentPasscode">Current passcode</param>
    /// <param name="newPasscode">New 8-digit passcode</param>
    /// <returns>True if change was successful</returns>
    Task<bool> ChangePasscodeAsync(string currentPasscode, string newPasscode);

    /// <summary>
    /// Gets the number of failed passcode attempts
    /// </summary>
    /// <returns>Number of failed attempts</returns>
    Task<int> GetFailedAttemptsAsync();

    /// <summary>
    /// Increments the failed attempts counter
    /// </summary>
    Task IncrementFailedAttemptsAsync();

    /// <summary>
    /// Resets the failed attempts counter
    /// </summary>
    Task ResetFailedAttemptsAsync();

    /// <summary>
    /// Checks if the passcode is temporarily locked due to too many failed attempts
    /// </summary>
    /// <returns>True if locked, false if available</returns>
    Task<bool> IsPasscodeLockedAsync();

    /// <summary>
    /// Gets the time remaining until passcode is unlocked (in seconds)
    /// </summary>
    /// <returns>Seconds until unlock, 0 if not locked</returns>
    Task<int> GetLockoutTimeRemainingAsync();
}