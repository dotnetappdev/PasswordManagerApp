namespace PasswordManager.Services.Interfaces;

/// <summary>
/// Interface for secure storage operations
/// </summary>
public interface ISecureStorageService
{
    /// <summary>
    /// Stores a value securely
    /// </summary>
    /// <param name="key">The key to store the value under</param>
    /// <param name="value">The value to store</param>
    Task SetAsync(string key, string value);

    /// <summary>
    /// Retrieves a value from secure storage
    /// </summary>
    /// <param name="key">The key to retrieve the value for</param>
    /// <returns>The stored value or null if not found</returns>
    Task<string?> GetAsync(string key);

    /// <summary>
    /// Removes a value from secure storage
    /// </summary>
    /// <param name="key">The key to remove</param>
    /// <returns>True if the key was removed, false if it didn't exist</returns>
    bool Remove(string key);

    /// <summary>
    /// Removes all values from secure storage
    /// </summary>
    void RemoveAll();
}