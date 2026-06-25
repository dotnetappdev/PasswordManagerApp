namespace VaultGuard.Models.DTOs;

/// <summary>
/// Backup schedule intervals
/// </summary>
public enum BackupScheduleInterval
{
    Manual = 0,
    Daily = 1,
    Weekly = 7,
    BiWeekly = 14,
    Monthly = 30,
    BiMonthly = 60,
    Quarterly = 90
}

/// <summary>
/// Backup schedule configuration
/// </summary>
public class BackupScheduleInfo
{
    public BackupScheduleInterval Interval { get; set; } = BackupScheduleInterval.Manual;
    public TimeSpan PreferredTime { get; set; } = new TimeSpan(2, 0, 0); // 2:00 AM default
    public DateTime? LastScheduledBackup { get; set; }
    public DateTime? NextScheduledBackup { get; set; }
    public bool IsEnabled { get; set; } = false;

    /// <summary>
    /// Calculate next backup time based on interval and preferred time
    /// </summary>
    public DateTime CalculateNextBackupTime()
    {
        if (Interval == BackupScheduleInterval.Manual || !IsEnabled)
            return DateTime.MaxValue;

        var now = DateTime.UtcNow;
        var today = now.Date.Add(PreferredTime);
        
        // If today's scheduled time hasn't passed, schedule for today
        if (today > now && LastScheduledBackup?.Date != now.Date)
            return today;
        
        // Otherwise, schedule for next interval
        return Interval switch
        {
            BackupScheduleInterval.Daily => today.AddDays(1),
            BackupScheduleInterval.Weekly => today.AddDays(7),
            BackupScheduleInterval.BiWeekly => today.AddDays(14),
            BackupScheduleInterval.Monthly => today.AddMonths(1),
            BackupScheduleInterval.BiMonthly => today.AddMonths(2),
            BackupScheduleInterval.Quarterly => today.AddMonths(3),
            _ => DateTime.MaxValue
        };
    }

    /// <summary>
    /// Check if a backup is due now
    /// </summary>
    public bool IsBackupDue()
    {
        if (!IsEnabled || Interval == BackupScheduleInterval.Manual)
            return false;

        return NextScheduledBackup.HasValue && DateTime.UtcNow >= NextScheduledBackup.Value;
    }

    /// <summary>
    /// Get display name for the schedule interval
    /// </summary>
    public string GetDisplayName() => Interval switch
    {
        BackupScheduleInterval.Manual => "Manual Only",
        BackupScheduleInterval.Daily => "Daily",
        BackupScheduleInterval.Weekly => "Weekly",
        BackupScheduleInterval.BiWeekly => "Every 2 Weeks",
        BackupScheduleInterval.Monthly => "Monthly",
        BackupScheduleInterval.BiMonthly => "Every 2 Months",
        BackupScheduleInterval.Quarterly => "Every 3 Months",
        _ => "Manual Only"
    };
}

/// <summary>
/// Backup schedule option for UI display
/// </summary>
public class BackupScheduleOption
{
    public BackupScheduleInterval Interval { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public static List<BackupScheduleOption> GetAllOptions() => new()
    {
        new BackupScheduleOption 
        { 
            Interval = BackupScheduleInterval.Manual, 
            DisplayName = "Manual Only", 
            Description = "Backups only when manually triggered" 
        },
        new BackupScheduleOption 
        { 
            Interval = BackupScheduleInterval.Daily, 
            DisplayName = "Daily", 
            Description = "Backup every day at the specified time" 
        },
        new BackupScheduleOption 
        { 
            Interval = BackupScheduleInterval.Weekly, 
            DisplayName = "Weekly", 
            Description = "Backup once per week" 
        },
        new BackupScheduleOption 
        { 
            Interval = BackupScheduleInterval.BiWeekly, 
            DisplayName = "Every 2 Weeks", 
            Description = "Backup every two weeks" 
        },
        new BackupScheduleOption 
        { 
            Interval = BackupScheduleInterval.Monthly, 
            DisplayName = "Monthly", 
            Description = "Backup once per month" 
        },
        new BackupScheduleOption 
        { 
            Interval = BackupScheduleInterval.BiMonthly, 
            DisplayName = "Every 2 Months", 
            Description = "Backup every two months" 
        },
        new BackupScheduleOption 
        { 
            Interval = BackupScheduleInterval.Quarterly, 
            DisplayName = "Every 3 Months", 
            Description = "Backup every three months" 
        }
    };
}