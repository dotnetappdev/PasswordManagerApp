using PasswordManager.Models;

namespace PasswordManager.Models.DTOs;

public class WiFiItemDto
{
    public int Id { get; set; }
    public int PasswordItemId { get; set; }
    public string? NetworkName { get; set; }
    public string? Password { get; set; }
    public SecurityType SecurityType { get; set; }
    public bool IsHidden { get; set; }
    public string? IPAddress { get; set; }
    public string? SubnetMask { get; set; }
    public string? DefaultGateway { get; set; }
    public string? PrimaryDNS { get; set; }
    public string? SecondaryDNS { get; set; }
    public bool UseStaticIP { get; set; }
    public string? RouterBrand { get; set; }
    public string? RouterModel { get; set; }
    public string? RouterIP { get; set; }
    public string? RouterUsername { get; set; }
    public string? RouterPassword { get; set; }
    public string? RouterAdminUrl { get; set; }
    public FrequencyType Frequency { get; set; }
    public string? Channel { get; set; }
    public string? Bandwidth { get; set; }
    public string? WirelessStandard { get; set; }
    public string? SignalStrength { get; set; }
    public string? MacAddress { get; set; }
    public string? BSSID { get; set; }
    public string? ISPName { get; set; }
    public string? PlanType { get; set; }
    public string? DownloadSpeed { get; set; }
    public string? UploadSpeed { get; set; }
    public string? DataLimit { get; set; }
    public string? ISPPhoneNumber { get; set; }
    public string? AccountNumber { get; set; }
    public string? Location { get; set; }
    public string? InstallationAddress { get; set; }
    public string? Building { get; set; }
    public string? Floor { get; set; }
    public string? Room { get; set; }
    public string? GuestNetworkName { get; set; }
    public string? GuestNetworkPassword { get; set; }
    public bool HasGuestNetwork { get; set; }
    public string? QRCodeData { get; set; }
    public DateTime? LastUsed { get; set; }
    public int UsageCount { get; set; }
    public string? Notes { get; set; }
}

public class CreateWiFiItemDto
{
    public string? NetworkName { get; set; }
    public string? Password { get; set; }
    public SecurityType SecurityType { get; set; }
    public bool IsHidden { get; set; }
    public string? IPAddress { get; set; }
    public string? SubnetMask { get; set; }
    public string? DefaultGateway { get; set; }
    public string? PrimaryDNS { get; set; }
    public string? SecondaryDNS { get; set; }
    public bool UseStaticIP { get; set; }
    public string? RouterBrand { get; set; }
    public string? RouterModel { get; set; }
    public string? RouterIP { get; set; }
    public string? RouterUsername { get; set; }
    public string? RouterPassword { get; set; }
    public string? RouterAdminUrl { get; set; }
    public FrequencyType Frequency { get; set; }
    public string? Channel { get; set; }
    public string? Bandwidth { get; set; }
    public string? WirelessStandard { get; set; }
    public string? SignalStrength { get; set; }
    public string? MacAddress { get; set; }
    public string? BSSID { get; set; }
    public string? ISPName { get; set; }
    public string? PlanType { get; set; }
    public string? DownloadSpeed { get; set; }
    public string? UploadSpeed { get; set; }
    public string? DataLimit { get; set; }
    public string? ISPPhoneNumber { get; set; }
    public string? AccountNumber { get; set; }
    public string? Location { get; set; }
    public string? InstallationAddress { get; set; }
    public string? Building { get; set; }
    public string? Floor { get; set; }
    public string? Room { get; set; }
    public string? GuestNetworkName { get; set; }
    public string? GuestNetworkPassword { get; set; }
    public bool HasGuestNetwork { get; set; }
    public string? QRCodeData { get; set; }
    public string? Notes { get; set; }
}

public class UpdateWiFiItemDto
{
    public string? NetworkName { get; set; }
    public string? Password { get; set; }
    public SecurityType SecurityType { get; set; }
    public bool IsHidden { get; set; }
    public string? IPAddress { get; set; }
    public string? SubnetMask { get; set; }
    public string? DefaultGateway { get; set; }
    public string? PrimaryDNS { get; set; }
    public string? SecondaryDNS { get; set; }
    public bool UseStaticIP { get; set; }
    public string? RouterBrand { get; set; }
    public string? RouterModel { get; set; }
    public string? RouterIP { get; set; }
    public string? RouterUsername { get; set; }
    public string? RouterPassword { get; set; }
    public string? RouterAdminUrl { get; set; }
    public FrequencyType Frequency { get; set; }
    public string? Channel { get; set; }
    public string? Bandwidth { get; set; }
    public string? WirelessStandard { get; set; }
    public string? SignalStrength { get; set; }
    public string? MacAddress { get; set; }
    public string? BSSID { get; set; }
    public string? ISPName { get; set; }
    public string? PlanType { get; set; }
    public string? DownloadSpeed { get; set; }
    public string? UploadSpeed { get; set; }
    public string? DataLimit { get; set; }
    public string? ISPPhoneNumber { get; set; }
    public string? AccountNumber { get; set; }
    public string? Location { get; set; }
    public string? InstallationAddress { get; set; }
    public string? Building { get; set; }
    public string? Floor { get; set; }
    public string? Room { get; set; }
    public string? GuestNetworkName { get; set; }
    public string? GuestNetworkPassword { get; set; }
    public bool HasGuestNetwork { get; set; }
    public string? QRCodeData { get; set; }
    public string? Notes { get; set; }
}
