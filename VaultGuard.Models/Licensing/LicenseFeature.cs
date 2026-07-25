namespace VaultGuard.Models.Licensing;

/// <summary>
/// Individually unlockable Pro features. Bitwise flags so a single license can grant any combination.
/// </summary>
[Flags]
public enum LicenseFeature
{
    None = 0,
    ApiAccess = 1 << 0,
    CloudBackup = 1 << 1,
    UnlimitedDevices = 1 << 2,
    AdvancedSharing = 1 << 3,
    PrioritySupport = 1 << 4,
    Sso = 1 << 5,
    MultiTenancy = 1 << 6,

    All = ApiAccess | CloudBackup | UnlimitedDevices | AdvancedSharing | PrioritySupport | Sso | MultiTenancy
}

/// <summary>
/// Commercial plan tiers. A plan implies a default feature set (see <see cref="LicensePlans"/>);
/// individual <see cref="LicenseFeature"/> flags on a license can still narrow or extend that.
/// </summary>
public enum LicensePlan
{
    Free = 0,
    Pro = 1,
    Business = 2,
    Enterprise = 3
}

public static class LicensePlans
{
    public static LicenseFeature DefaultFeatures(LicensePlan plan) => plan switch
    {
        LicensePlan.Free => LicenseFeature.None,
        LicensePlan.Pro => LicenseFeature.ApiAccess | LicenseFeature.CloudBackup | LicenseFeature.UnlimitedDevices,
        LicensePlan.Business => LicenseFeature.ApiAccess | LicenseFeature.CloudBackup | LicenseFeature.UnlimitedDevices
            | LicenseFeature.AdvancedSharing | LicenseFeature.PrioritySupport,
        LicensePlan.Enterprise => LicenseFeature.All,
        _ => LicenseFeature.None
    };
}
