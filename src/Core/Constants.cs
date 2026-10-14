namespace USBSilentCopy.Core;

public static class Constants
{
    public const string ApplicationName = "USBSilentCopy";
    public const string ApplicationVersion = "1.0.0";
    public const string SingleInstanceMutexId = @"Local\USBSilentCopy_Mutex_9D82A7C3";

    public const string DefaultConfigFileName = "config.json";
    public const string DefaultBackupFolderTemplate = "{VolumeLabel}_{DriveLetter}_{Date}_{Time}";

    // دسته‌بندی پیش‌فرض پسوندها
    public static readonly Dictionary<FileCategory, HashSet<string>> DefaultCategoryExtensions = new()
    {
        [FileCategory.Video] = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".ts", ".3gp"
        },
        [FileCategory.Audio] = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp3", ".wav", ".flac", ".aac", ".ogg", ".wma", ".m4a", ".opus"
        },
        [FileCategory.Images] = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".svg", ".ico", ".raw", ".heic"
        },
        [FileCategory.Documents] = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".rtf", ".odt", ".csv", ".md"
        },
        [FileCategory.Archives] = new(StringComparer.OrdinalIgnoreCase)
        {
            ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".xz", ".iso"
        },
        [FileCategory.Executables] = new(StringComparer.OrdinalIgnoreCase)
        {
            ".exe", ".msi", ".bat", ".cmd", ".ps1", ".vbs", ".dll"
        }
    };
}
