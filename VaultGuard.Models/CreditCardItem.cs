using System.ComponentModel.DataAnnotations;

namespace PasswordManager.Models;

public class CreditCardItem
{
    public int Id { get; set; }
    public int PasswordItemId { get; set; }

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // User relationship
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }
    
    // Card Details
    [MaxLength(100)]
    public string? CardholderName { get; set; }
    
    [MaxLength(19)] // Maximum for credit card numbers with spaces
    public string? CardNumber { get; set; }
    
    [MaxLength(7)] // MM/YYYY format
    public string? ExpiryDate { get; set; }
    
    [MaxLength(4)]
    public string? CVV { get; set; }
    
    [MaxLength(6)]
    public string? PIN { get; set; }
    
    public CardType CardType { get; set; }
    
    [MaxLength(100)]
    public string? IssuingBank { get; set; }
    
    [MaxLength(7)] // MM/YYYY format
    public string? ValidFrom { get; set; }
    
    // Contact Information
    [MaxLength(200)]
    public string? BankWebsite { get; set; }
    
    [MaxLength(20)]
    public string? BankPhoneNumber { get; set; }
    
    [MaxLength(20)]
    public string? CustomerServicePhone { get; set; }
    
    // Online Banking
    [MaxLength(100)]
    public string? OnlineBankingUsername { get; set; }
    
    [MaxLength(500)]
    public string? OnlineBankingPassword { get; set; }
    
    [MaxLength(200)]
    public string? OnlineBankingUrl { get; set; }
    
    // Financial Details
    [MaxLength(20)]
    public string? CreditLimit { get; set; }
    
    [MaxLength(10)]
    public string? InterestRate { get; set; }
    
    [MaxLength(20)]
    public string? CashAdvanceLimit { get; set; }
    
    [MaxLength(20)]
    public string? AvailableCredit { get; set; }
    
    // Billing Address
    [MaxLength(200)]
    public string? BillingAddressLine1 { get; set; }
    
    [MaxLength(200)]
    public string? BillingAddressLine2 { get; set; }
    
    [MaxLength(100)]
    public string? BillingCity { get; set; }
    
    [MaxLength(50)]
    public string? BillingState { get; set; }
    
    [MaxLength(20)]
    public string? BillingZipCode { get; set; }
    
    [MaxLength(50)]
    public string? BillingCountry { get; set; }
    
    // Rewards and Benefits
    [MaxLength(100)]
    public string? RewardsProgram { get; set; }
    
    [MaxLength(50)]
    public string? RewardsNumber { get; set; }
    
    [MaxLength(500)]
    public string? BenefitsDescription { get; set; }
    
    // Travel Benefits
    [MaxLength(100)]
    public string? TravelInsurance { get; set; }
    
    [MaxLength(100)]
    public string? AirportLoungeAccess { get; set; }
    
    // Security
    [MaxLength(20)]
    public string? FraudAlertPhone { get; set; }
    
    [MaxLength(200)]
    public string? FraudAlertEmail { get; set; }
    
    // Usage Tracking
    public DateTime? LastUsed { get; set; }
    
    public int UsageCount { get; set; }
    
    [MaxLength(2000)]
    public string? Notes { get; set; }
    
    // Navigation property
    public PasswordItem PasswordItem { get; set; } = null!;

    // Additional fields for mapping and compatibility
    public string? ExpirationMonth { get; set; } // For mapping
    public string? ExpirationYear { get; set; } // For mapping
    public string? SecurityCode { get; set; } // For mapping
    public bool RequiresMasterPassword { get; set; } // For mapping
    public int? PasswordId { get; set; } // For mapping

    [MaxLength(1000)]
    public string? EncryptedCardNumber { get; set; }
    [MaxLength(200)]
    public string? CardNumberNonce { get; set; }
    [MaxLength(200)]
    public string? CardNumberAuthTag { get; set; }
    [MaxLength(1000)]
    public string? EncryptedCvv { get; set; }
    [MaxLength(200)]
    public string? CvvNonce { get; set; }
    [MaxLength(200)]
    public string? CvvAuthTag { get; set; }
}
