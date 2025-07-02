using PasswordManager.Models;

namespace PasswordManager.Models.DTOs;

public class WiFiItemDto
{
    public int Id { get; set; }
    public string? SSID { get; set; }
    public string? Password { get; set; }
    public SecurityType SecurityType { get; set; }
    public FrequencyType Frequency { get; set; }
    public string? RouterPassword { get; set; }
    public string? Notes { get; set; }
    public int PasswordItemId { get; set; }
}

public class CreateWiFiItemDto
{
    public string? SSID { get; set; }
    public string? Password { get; set; }
    public SecurityType SecurityType { get; set; }
    public FrequencyType Frequency { get; set; }
    public string? RouterPassword { get; set; }
    public string? Notes { get; set; }
}

public class UpdateWiFiItemDto
{
    public string? SSID { get; set; }
    public string? Password { get; set; }
    public SecurityType SecurityType { get; set; }
    public FrequencyType Frequency { get; set; }
    public string? RouterPassword { get; set; }
    public string? Notes { get; set; }
}
