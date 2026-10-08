namespace USBSilentCopy.Core;

/// <summary>
/// حالت انجام عملیات پشتیبان‌گیری
/// </summary>
public enum BackupMode
{
    Copy,
    Move
}

/// <summary>
/// سیاست مدیریت فایل‌های تکراری در مقصد
/// </summary>
public enum ExistingFilePolicy
{
    Skip,
    Overwrite,
    ReplaceIfNewer,
    CreateNewBackupFolder
}

/// <summary>
/// وضعیت سخت‌افزاری و پردازشی دستگاه USB
/// </summary>
public enum DeviceStatus
{
    Disconnected,
    Detected,
    Queued,
    InProgress,
    Paused,
    Completed,
    Interrupted,
    Failed
}

/// <summary>
/// وضعیت اجرای وظیفه پشتیبان‌گیری
/// </summary>
public enum BackupStatus
{
    Queued,
    Scanning,
    Copying,
    Paused,
    Completed,
    Interrupted,
    Failed,
    Cancelled
}

/// <summary>
/// دسته‌بندی فایل‌ها جهت فیلترسازی
/// </summary>
public enum FileCategory
{
    Video,
    Audio,
    Images,
    Documents,
    Archives,
    Executables,
    Other
}

/// <summary>
/// سطح پیام‌های ثبت لاگ
/// </summary>
public enum LogLevel
{
    Trace,
    Debug,
    Info,
    Warning,
    Error,
    Fatal
}
