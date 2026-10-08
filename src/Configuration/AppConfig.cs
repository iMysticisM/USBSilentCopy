using System.Text.Json;
using System.Text.Json.Serialization;
using USBSilentCopy.Core;

namespace USBSilentCopy.Configuration;

/// <summary>
/// مدل جامع پیکربندی نرم‌افزار USBSilentCopy
/// </summary>
public class AppConfig
{
    // ۱. مسیرها و عمومی
    [JsonPropertyName("applicationName")]
    public string ApplicationName { get; set; } = Constants.ApplicationName;

    [JsonPropertyName("destinationDirectory")]
    public string DestinationDirectory { get; set; } = "Backup";

    [JsonPropertyName("secondaryDestination")]
    public string SecondaryDestination { get; set; } = string.Empty;

    [JsonPropertyName("enableSecondaryDestination")]
    public bool EnableSecondaryDestination { get; set; } = false;

    [JsonPropertyName("logsDirectory")]
    public string LogsDirectory { get; set; } = "logs";

    [JsonPropertyName("historyDirectory")]
    public string HistoryDirectory { get; set; } = "history";

    [JsonPropertyName("profilesDirectory")]
    public string ProfilesDirectory { get; set; } = "profiles";

    [JsonPropertyName("delayAfterUsbConnectionSeconds")]
    public int DelayAfterUsbConnectionSeconds { get; set; } = 3;

    // ۲. حالت اجرا و رابط کاربری
    [JsonPropertyName("silentMode")]
    public bool SilentMode { get; set; } = false;

    [JsonPropertyName("enableNotifications")]
    public bool EnableNotifications { get; set; } = true;

    [JsonPropertyName("startupCurrentUser")]
    public bool StartupCurrentUser { get; set; } = true;

    [JsonPropertyName("startupAllUsers")]
    public bool StartupAllUsers { get; set; } = false;

    // ۳. موتور پشتیبان‌گیری
    [JsonPropertyName("copyMode")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public BackupMode CopyMode { get; set; } = BackupMode.Copy;

    [JsonPropertyName("speedLimitMBps")]
    public int SpeedLimitMBps { get; set; } = 0; // 0 = نامحدود

    [JsonPropertyName("maxConcurrentTasks")]
    public int MaxConcurrentTasks { get; set; } = 1;

    [JsonPropertyName("retryCount")]
    public int RetryCount { get; set; } = 3;

    [JsonPropertyName("retryDelaySeconds")]
    public int RetryDelaySeconds { get; set; } = 5;

    [JsonPropertyName("skipIfFailed")]
    public bool SkipIfFailed { get; set; } = true;

    [JsonPropertyName("preventDiskFull")]
    public bool PreventDiskFull { get; set; } = true;

    [JsonPropertyName("minimumFreeSpaceMB")]
    public long MinimumFreeSpaceMB { get; set; } = 500;

    // ۴. استراتژی افزایشی و فایل‌های موجود
    [JsonPropertyName("skipExisting")]
    public bool SkipExisting { get; set; } = true;

    [JsonPropertyName("copyOnlyNew")]
    public bool CopyOnlyNew { get; set; } = false;

    [JsonPropertyName("compareHash")]
    public bool CompareHash { get; set; } = false;

    [JsonPropertyName("existingFilePolicy")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ExistingFilePolicy ExistingFilePolicy { get; set; } = ExistingFilePolicy.ReplaceIfNewer;

    [JsonPropertyName("overwriteExistingBackup")]
    public bool OverwriteExistingBackup { get; set; } = true;

    [JsonPropertyName("backupFolderTemplate")]
    public string BackupFolderTemplate { get; set; } = Constants.DefaultBackupFolderTemplate;

    [JsonPropertyName("copyEmptyDirectories")]
    public bool CopyEmptyDirectories { get; set; } = true;

    // ۵. فیلترها
    [JsonPropertyName("filters")]
    public FilterConfig Filters { get; set; } = new();

    // ۶. تنظیمات لاگ
    [JsonPropertyName("logging")]
    public LoggingConfig Logging { get; set; } = new();

    // ۷. کلید میانبر
    [JsonPropertyName("pauseHotkeyEnabled")]
    public bool PauseHotkeyEnabled { get; set; } = true;
}

public class FilterConfig
{
    [JsonPropertyName("includeExtensions")]
    public List<string> IncludeExtensions { get; set; } = new();

    [JsonPropertyName("excludeExtensions")]
    public List<string> ExcludeExtensions { get; set; } = new();

    [JsonPropertyName("includeCategories")]
    public List<string> IncludeCategories { get; set; } = new();

    [JsonPropertyName("excludeCategories")]
    public List<string> ExcludeCategories { get; set; } = new();

    [JsonPropertyName("includeFolders")]
    public List<string> IncludeFolders { get; set; } = new();

    [JsonPropertyName("excludeFolders")]
    public List<string> ExcludeFolders { get; set; } = new()
    {
        "$RECYCLE.BIN",
        "System Volume Information",
        ".Spotlight-V100",
        ".Trashes"
    };

    [JsonPropertyName("minSizeMB")]
    public double? MinSizeMB { get; set; }

    [JsonPropertyName("maxSizeMB")]
    public double? MaxSizeMB { get; set; }

    [JsonPropertyName("modifiedAfter")]
    public DateTime? ModifiedAfter { get; set; }

    [JsonPropertyName("modifiedBefore")]
    public DateTime? ModifiedBefore { get; set; }

    [JsonPropertyName("createdAfter")]
    public DateTime? CreatedAfter { get; set; }

    [JsonPropertyName("createdBefore")]
    public DateTime? CreatedBefore { get; set; }
}

public class LoggingConfig
{
    [JsonPropertyName("enableLogging")]
    public bool EnableLogging { get; set; } = true;

    [JsonPropertyName("maxLogSizeBytes")]
    public long MaxLogSizeBytes { get; set; } = 10 * 1024 * 1024; // 10 MB

    [JsonPropertyName("maxLogFiles")]
    public int MaxLogFiles { get; set; } = 10;

    [JsonPropertyName("minLogLevel")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public LogLevel MinLogLevel { get; set; } = LogLevel.Info;
}
