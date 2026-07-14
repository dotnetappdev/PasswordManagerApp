using Allure.NUnit;
using NUnit.Framework;
using VaultGuard.Services.Helpers;

namespace VaultGuard.BackEnd.Tests.Helpers;

[TestFixture]
[AllureNUnit]
public class InputValidationHelperTests
{
    [Test]
    public void ValidateUsername_WithNullOrEmpty_ReturnsFalse()
    {
        // Arrange & Act
        var result1 = InputValidationHelper.ValidateUsername(null!);
        var result2 = InputValidationHelper.ValidateUsername("");
        var result3 = InputValidationHelper.ValidateUsername("   ");

        // Assert
        Assert.That(result1.IsValid, Is.False);
        Assert.That(result1.ErrorMessage, Is.EqualTo("Username is required"));
        Assert.That(result2.IsValid, Is.False);
        Assert.That(result3.IsValid, Is.False);
    }

    [Test]
    public void ValidateUsername_WithSingleCharacter_ReturnsFalse()
    {
        // Arrange & Act
        var result = InputValidationHelper.ValidateUsername("a");

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Contains.Substring("at least 2 characters"));
    }

    [Test]
    public void ValidateUsername_WithIllegalCharacters_ReturnsFalse()
    {
        // Arrange & Act
        var result1 = InputValidationHelper.ValidateUsername("user@test#com");
        var result2 = InputValidationHelper.ValidateUsername("user name");
        var result3 = InputValidationHelper.ValidateUsername("user!name");

        // Assert
        Assert.That(result1.IsValid, Is.False);
        Assert.That(result1.ErrorMessage, Contains.Substring("can only contain"));
        Assert.That(result2.IsValid, Is.False);
        Assert.That(result3.IsValid, Is.False);
    }

    [Test]
    public void ValidateUsername_WithLegalCharacters_ReturnsTrue()
    {
        // Arrange & Act
        var result1 = InputValidationHelper.ValidateUsername("user@test.com");
        var result2 = InputValidationHelper.ValidateUsername("user_name");
        var result3 = InputValidationHelper.ValidateUsername("user-name");
        var result4 = InputValidationHelper.ValidateUsername("username123");

        // Assert
        Assert.That(result1.IsValid, Is.True);
        Assert.That(result2.IsValid, Is.True);
        Assert.That(result3.IsValid, Is.True);
        Assert.That(result4.IsValid, Is.True);
    }

    [Test]
    public void ValidatePassword_WithNullOrEmpty_ReturnsFalse()
    {
        // Arrange & Act
        var result1 = InputValidationHelper.ValidatePassword(null!);
        var result2 = InputValidationHelper.ValidatePassword("");
        var result3 = InputValidationHelper.ValidatePassword("   ");

        // Assert
        Assert.That(result1.IsValid, Is.False);
        Assert.That(result1.ErrorMessage, Is.EqualTo("Password is required"));
        Assert.That(result2.IsValid, Is.False);
        Assert.That(result3.IsValid, Is.False);
    }

    [Test]
    public void ValidatePassword_WithSingleCharacter_ReturnsFalse()
    {
        // Arrange & Act
        var result = InputValidationHelper.ValidatePassword("a");

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Contains.Substring("at least 2 characters"));
    }

    [Test]
    public void ValidatePassword_WithValidLength_ReturnsTrue()
    {
        // Arrange & Act
        var result = InputValidationHelper.ValidatePassword("ab");

        // Assert
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void ValidateMasterPassword_WithShortPassword_ReturnsFalse()
    {
        // Arrange & Act
        var result1 = InputValidationHelper.ValidateMasterPassword("Pass1");
        var result2 = InputValidationHelper.ValidateMasterPassword("Pass12");
        var result3 = InputValidationHelper.ValidateMasterPassword("Pass123");

        // Assert
        Assert.That(result1.IsValid, Is.False);
        Assert.That(result1.ErrorMessage, Contains.Substring("at least 8 characters"));
        Assert.That(result2.IsValid, Is.False);
        Assert.That(result3.IsValid, Is.False);
    }

    [Test]
    public void ValidateMasterPassword_WithoutUppercase_ReturnsFalse()
    {
        // Arrange & Act
        var result = InputValidationHelper.ValidateMasterPassword("password123");

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Contains.Substring("uppercase letter"));
    }

    [Test]
    public void ValidateMasterPassword_WithoutLowercase_ReturnsFalse()
    {
        // Arrange & Act
        var result = InputValidationHelper.ValidateMasterPassword("PASSWORD123");

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Contains.Substring("lowercase letter"));
    }

    [Test]
    public void ValidateMasterPassword_WithoutDigit_ReturnsFalse()
    {
        // Arrange & Act
        var result = InputValidationHelper.ValidateMasterPassword("PasswordABC");

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Contains.Substring("number"));
    }

    [Test]
    public void ValidateMasterPassword_WithAllRequirements_ReturnsTrue()
    {
        // Arrange & Act
        var result = InputValidationHelper.ValidateMasterPassword("Password123");

        // Assert
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.ErrorMessage, Is.Empty);
    }

    [Test]
    public void ValidateName_WithNullOrEmpty_ReturnsFalse()
    {
        // Arrange & Act
        var result1 = InputValidationHelper.ValidateName(null!, "First name");
        var result2 = InputValidationHelper.ValidateName("", "Last name");
        var result3 = InputValidationHelper.ValidateName("   ", "Name");

        // Assert
        Assert.That(result1.IsValid, Is.False);
        Assert.That(result1.ErrorMessage, Contains.Substring("First name is required"));
        Assert.That(result2.IsValid, Is.False);
        Assert.That(result3.IsValid, Is.False);
    }

    [Test]
    public void ValidateName_WithSingleCharacter_ReturnsFalse()
    {
        // Arrange & Act
        var result = InputValidationHelper.ValidateName("J", "First name");

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Contains.Substring("at least 2 characters"));
    }

    [Test]
    public void ValidateName_WithIllegalCharacters_ReturnsFalse()
    {
        // Arrange & Act
        var result1 = InputValidationHelper.ValidateName("John123", "First name");
        var result2 = InputValidationHelper.ValidateName("John@Doe", "First name");
        var result3 = InputValidationHelper.ValidateName("John_Doe", "First name");

        // Assert
        Assert.That(result1.IsValid, Is.False);
        Assert.That(result1.ErrorMessage, Contains.Substring("can only contain"));
        Assert.That(result2.IsValid, Is.False);
        Assert.That(result3.IsValid, Is.False);
    }

    [Test]
    public void ValidateName_WithLegalCharacters_ReturnsTrue()
    {
        // Arrange & Act
        var result1 = InputValidationHelper.ValidateName("John", "First name");
        var result2 = InputValidationHelper.ValidateName("Mary-Jane", "First name");
        var result3 = InputValidationHelper.ValidateName("O'Brien", "Last name");
        var result4 = InputValidationHelper.ValidateName("Jean Pierre", "First name");

        // Assert
        Assert.That(result1.IsValid, Is.True);
        Assert.That(result2.IsValid, Is.True);
        Assert.That(result3.IsValid, Is.True);
        Assert.That(result4.IsValid, Is.True);
    }

    [Test]
    public void ValidateEmail_WithNullOrEmpty_ReturnsFalse()
    {
        // Arrange & Act
        var result1 = InputValidationHelper.ValidateEmail(null!);
        var result2 = InputValidationHelper.ValidateEmail("");
        var result3 = InputValidationHelper.ValidateEmail("   ");

        // Assert
        Assert.That(result1.IsValid, Is.False);
        Assert.That(result1.ErrorMessage, Is.EqualTo("Email address is required"));
        Assert.That(result2.IsValid, Is.False);
        Assert.That(result3.IsValid, Is.False);
    }

    [Test]
    public void ValidateEmail_WithTooShort_ReturnsFalse()
    {
        // Arrange & Act
        var result = InputValidationHelper.ValidateEmail("ab");

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Contains.Substring("at least 3 characters"));
    }

    [Test]
    public void ValidateEmail_WithInvalidFormat_ReturnsFalse()
    {
        // Arrange & Act
        var result1 = InputValidationHelper.ValidateEmail("notanemail");
        var result2 = InputValidationHelper.ValidateEmail("test@");
        var result3 = InputValidationHelper.ValidateEmail("@test.com");
        var result4 = InputValidationHelper.ValidateEmail("test@test");

        // Assert
        Assert.That(result1.IsValid, Is.False);
        Assert.That(result2.IsValid, Is.False);
        Assert.That(result3.IsValid, Is.False);
        Assert.That(result4.IsValid, Is.False);
    }

    [Test]
    public void ValidateEmail_WithValidFormat_ReturnsTrue()
    {
        // Arrange & Act
        var result1 = InputValidationHelper.ValidateEmail("test@example.com");
        var result2 = InputValidationHelper.ValidateEmail("user.name@example.co.uk");
        var result3 = InputValidationHelper.ValidateEmail("user-name@example-domain.com");

        // Assert
        Assert.That(result1.IsValid, Is.True);
        Assert.That(result2.IsValid, Is.True);
        Assert.That(result3.IsValid, Is.True);
    }

    [Test]
    public void ValidatePasswordMatch_WithMatchingPasswords_ReturnsTrue()
    {
        // Arrange & Act
        var result = InputValidationHelper.ValidatePasswordMatch("Password123", "Password123");

        // Assert
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.ErrorMessage, Is.Empty);
    }

    [Test]
    public void ValidatePasswordMatch_WithDifferentPasswords_ReturnsFalse()
    {
        // Arrange & Act
        var result = InputValidationHelper.ValidatePasswordMatch("Password123", "Password456");

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo("Passwords do not match"));
    }

    [Test]
    public void ValidatePasswordMatch_CaseSensitive_ReturnsFalse()
    {
        // Arrange & Act
        var result = InputValidationHelper.ValidatePasswordMatch("Password123", "password123");

        // Assert
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo("Passwords do not match"));
    }
}
