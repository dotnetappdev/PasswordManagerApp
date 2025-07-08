using PasswordManager.Models;

namespace PasswordManager.Models.DTOs;

public class CreditCardItemDto
{
    public int Id { get; set; }
    public int PasswordItemId { get; set; }
    public string? CardholderName { get; set; }
    public string? CardNumber { get; set; }
    public string? ExpiryDate { get; set; }
    public string? CVV { get; set; }
    public string? PIN { get; set; }
    public CardType CardType { get; set; }
    public string? IssuingBank { get; set; }
    public string? ValidFrom { get; set; }
    public string? BankWebsite { get; set; }
    public string? BankPhoneNumber { get; set; }
    public string? CustomerServicePhone { get; set; }
    public string? OnlineBankingUsername { get; set; }
    public string? OnlineBankingPassword { get; set; }
    public string? OnlineBankingUrl { get; set; }
    public string? CreditLimit { get; set; }
    public string? InterestRate { get; set; }
    public string? CashAdvanceLimit { get; set; }
    public string? AvailableCredit { get; set; }
    public string? BillingAddressLine1 { get; set; }
    public string? BillingAddressLine2 { get; set; }
    public string? BillingCity { get; set; }
    public string? BillingState { get; set; }
    public string? BillingZipCode { get; set; }
    public string? BillingCountry { get; set; }
    public string? RewardsProgram { get; set; }
    public string? RewardsNumber { get; set; }
    public string? BenefitsDescription { get; set; }
    public string? TravelInsurance { get; set; }
    public string? AirportLoungeAccess { get; set; }
    public string? FraudAlertPhone { get; set; }
    public string? FraudAlertEmail { get; set; }
    public DateTime? LastUsed { get; set; }
    public int UsageCount { get; set; }
    public string? Notes { get; set; }
}

public class CreateCreditCardItemDto
{
    public string? CardholderName { get; set; }
    public string? CardNumber { get; set; }
    public string? ExpiryDate { get; set; }
    public string? CVV { get; set; }
    public string? PIN { get; set; }
    public CardType CardType { get; set; }
    public string? IssuingBank { get; set; }
    public string? ValidFrom { get; set; }
    public string? BankWebsite { get; set; }
    public string? BankPhoneNumber { get; set; }
    public string? CustomerServicePhone { get; set; }
    public string? OnlineBankingUsername { get; set; }
    public string? OnlineBankingPassword { get; set; }
    public string? OnlineBankingUrl { get; set; }
    public string? CreditLimit { get; set; }
    public string? InterestRate { get; set; }
    public string? CashAdvanceLimit { get; set; }
    public string? AvailableCredit { get; set; }
    public string? BillingAddressLine1 { get; set; }
    public string? BillingAddressLine2 { get; set; }
    public string? BillingCity { get; set; }
    public string? BillingState { get; set; }
    public string? BillingZipCode { get; set; }
    public string? BillingCountry { get; set; }
    public string? RewardsProgram { get; set; }
    public string? RewardsNumber { get; set; }
    public string? BenefitsDescription { get; set; }
    public string? TravelInsurance { get; set; }
    public string? AirportLoungeAccess { get; set; }
    public string? FraudAlertPhone { get; set; }
    public string? FraudAlertEmail { get; set; }
    public string? Notes { get; set; }
}

public class UpdateCreditCardItemDto
{
    public string? CardholderName { get; set; }
    public string? CardNumber { get; set; }
    public string? ExpiryDate { get; set; }
    public string? CVV { get; set; }
    public string? PIN { get; set; }
    public CardType CardType { get; set; }
    public string? IssuingBank { get; set; }
    public string? ValidFrom { get; set; }
    public string? BankWebsite { get; set; }
    public string? BankPhoneNumber { get; set; }
    public string? CustomerServicePhone { get; set; }
    public string? OnlineBankingUsername { get; set; }
    public string? OnlineBankingPassword { get; set; }
    public string? OnlineBankingUrl { get; set; }
    public string? CreditLimit { get; set; }
    public string? InterestRate { get; set; }
    public string? CashAdvanceLimit { get; set; }
    public string? AvailableCredit { get; set; }
    public string? BillingAddressLine1 { get; set; }
    public string? BillingAddressLine2 { get; set; }
    public string? BillingCity { get; set; }
    public string? BillingState { get; set; }
    public string? BillingZipCode { get; set; }
    public string? BillingCountry { get; set; }
    public string? RewardsProgram { get; set; }
    public string? RewardsNumber { get; set; }
    public string? BenefitsDescription { get; set; }
    public string? TravelInsurance { get; set; }
    public string? AirportLoungeAccess { get; set; }
    public string? FraudAlertPhone { get; set; }
    public string? FraudAlertEmail { get; set; }
    public string? Notes { get; set; }
}
