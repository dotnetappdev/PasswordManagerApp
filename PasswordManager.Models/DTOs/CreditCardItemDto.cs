using PasswordManager.Models;

namespace PasswordManager.Models.DTOs;

public class CreditCardItemDto
{
    public int Id { get; set; }
    public string? CardholderName { get; set; }
    public string? CardNumber { get; set; }
    public string? CVV { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? PIN { get; set; }
    public CardType CardType { get; set; }
    public string? Notes { get; set; }
    public int PasswordItemId { get; set; }
}

public class CreateCreditCardItemDto
{
    public string? CardholderName { get; set; }
    public string? CardNumber { get; set; }
    public string? CVV { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? PIN { get; set; }
    public CardType CardType { get; set; }
    public string? Notes { get; set; }
}

public class UpdateCreditCardItemDto
{
    public string? CardholderName { get; set; }
    public string? CardNumber { get; set; }
    public string? CVV { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? PIN { get; set; }
    public CardType CardType { get; set; }
    public string? Notes { get; set; }
}
