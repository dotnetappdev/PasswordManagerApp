using System.Linq;
using System.Text.RegularExpressions;

namespace VaultGuard.Services.Helpers;

/// <summary>
/// Provides validation helpers for user input across all platforms
/// </summary>
public static class InputValidationHelper
{
    private const int MinimumUsernameLength = 2;
    private const int MinimumPasswordLength = 2;
    private const int MinimumNameLength = 2;
    
    /// <summary>
    /// Validates that a username meets minimum requirements
    /// </summary>
    public static (bool IsValid, string ErrorMessage) ValidateUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return (false, "Username is required");
        }

        if (username.Length < MinimumUsernameLength)
        {
            return (false, $"Username must be at least {MinimumUsernameLength} characters long");
        }

        // Allow alphanumeric, @, ., -, _ (common for email-based usernames)
        if (!Regex.IsMatch(username, @"^[a-zA-Z0-9@.\-_]+$"))
        {
            return (false, "Username can only contain letters, numbers, @, ., -, and _");
        }

        return (true, string.Empty);
    }

    /// <summary>
    /// Validates that a password meets minimum requirements
    /// </summary>
    public static (bool IsValid, string ErrorMessage) ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return (false, "Password is required");
        }

        if (password.Length < MinimumPasswordLength)
        {
            return (false, $"Password must be at least {MinimumPasswordLength} characters long");
        }

        return (true, string.Empty);
    }

    /// <summary>
    /// Validates that a master password meets security requirements
    /// </summary>
    public static (bool IsValid, string ErrorMessage) ValidateMasterPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return (false, "Master password is required");
        }

        if (password.Length < 8)
        {
            return (false, "Master password must be at least 8 characters long");
        }

        if (!password.Any(char.IsUpper))
        {
            return (false, "Master password must contain at least one uppercase letter");
        }

        if (!password.Any(char.IsLower))
        {
            return (false, "Master password must contain at least one lowercase letter");
        }

        if (!password.Any(char.IsDigit))
        {
            return (false, "Master password must contain at least one number");
        }

        return (true, string.Empty);
    }

    /// <summary>
    /// Validates that a name (first or last) meets requirements
    /// </summary>
    public static (bool IsValid, string ErrorMessage) ValidateName(string name, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return (false, $"{fieldName} is required");
        }

        if (name.Length < MinimumNameLength)
        {
            return (false, $"{fieldName} must be at least {MinimumNameLength} characters long");
        }

        // Names should only contain letters, spaces, hyphens, and apostrophes
        if (!Regex.IsMatch(name, @"^[a-zA-Z\s'\-]+$"))
        {
            return (false, $"{fieldName} can only contain letters, spaces, hyphens, and apostrophes");
        }

        return (true, string.Empty);
    }

    /// <summary>
    /// Validates that an email address is in a valid format
    /// </summary>
    public static (bool IsValid, string ErrorMessage) ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return (false, "Email address is required");
        }

        if (email.Length < 3)
        {
            return (false, "Email address must be at least 3 characters long");
        }

        // Comprehensive email validation with legal characters only
        if (!Regex.IsMatch(email, @"^[a-zA-Z0-9@.\-_]+@[a-zA-Z0-9.\-_]+\.[a-zA-Z]{2,}$"))
        {
            return (false, "Please enter a valid email address with only legal characters (letters, numbers, @, ., -, _)");
        }

        return (true, string.Empty);
    }

    /// <summary>
    /// Validates that passwords match
    /// </summary>
    public static (bool IsValid, string ErrorMessage) ValidatePasswordMatch(string password, string confirmPassword)
    {
        if (password != confirmPassword)
        {
            return (false, "Passwords do not match");
        }

        return (true, string.Empty);
    }
}
