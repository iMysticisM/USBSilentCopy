namespace USBSilentCopy.Core;

/// <summary>
/// مشخصات کامل یک حافظه فلش یا درایو متصل‌شده
/// </summary>
public class UsbDeviceInfo
{
    public string DriveLetter { get; set; } = string.Empty;
    public string VolumeLabel { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public long CapacityBytes { get; set; }
    public long FreeSpaceBytes { get; set; }
    public string FileSystem { get; set; } = string.Empty;
    public DeviceStatus Status { get; set; } = DeviceStatus.Detected;
    public DateTime ConnectedTime { get; set; } = DateTime.Now;
    public DateTime? DisconnectedTime { get; set; }

    public string DisplayName =>
        string.IsNullOrWhiteSpace(VolumeLabel)
            ? $"Removable Drive ({DriveLetter})"
            : $"{VolumeLabel} ({DriveLetter})";

    public string ProfileIdentifier =>
        !string.IsNullOrWhiteSpace(SerialNumber) && SerialNumber != "00000000"
            ? SerialNumber
            : (!string.IsNullOrWhiteSpace(DeviceId) ? DeviceId.Replace('\\', '_').Replace('/', '_') : DriveLetter.TrimEnd('\\', ':'));

    public override string ToString() =>
        $"{DisplayName} | FS: {FileSystem} | Cap: {CapacityBytes / (1024 * 1024):N0} MB | S/N: {SerialNumber}";
}

/// <summary>
/// اطلاعات وضعیت پیشرفت کپی و پشتیبان‌گیری
/// </summary>
public class BackupProgressInfo
{
    public string DeviceIdentifier { get; set; } = string.Empty;
    public string CurrentFile { get; set; } = string.Empty;
    public int CurrentFileIndex { get; set; }
    public int TotalFiles { get; set; }
    public long BytesTransferred { get; set; }
    public long TotalBytes { get; set; }
    public double SpeedMBps { get; set; }
    public double OverallPercent => TotalBytes > 0 ? Math.Min(100.0, (double)BytesTransferred / TotalBytes * 100.0) : 0;
    public BackupStatus Status { get; set; }
}

/// <summary>
/// نتیجه جامع اجرای یک سشن پشتیبان‌گیری
/// </summary>
public class BackupSessionResult
{
    public string SessionId { get; set; } = Guid.NewGuid().ToString("N");
    public string DeviceId { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string DriveLetter { get; set; } = string.Empty;
    public string VolumeLabel { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public string DestinationPath { get; set; } = string.Empty;

    public string DisplayName =>
        !string.IsNullOrWhiteSpace(VolumeLabel)
            ? $"{VolumeLabel} ({DriveLetter})"
            : $"USB ({DriveLetter})";

    public DateTime StartTime { get; set; } = DateTime.Now;
    public DateTime? EndTime { get; set; }
    public TimeSpan Duration => EndTime.HasValue ? EndTime.Value - StartTime : TimeSpan.Zero;

    public int TotalFiles { get; set; }
    public int CopiedFiles { get; set; }
    public int SkippedFiles { get; set; }
    public int FailedFiles { get; set; }

    public long TotalBytes { get; set; }
    public long CopiedBytes { get; set; }

    public double AverageSpeedMBps =>
        Duration.TotalSeconds > 0 ? (CopiedBytes / (1024.0 * 1024.0)) / Duration.TotalSeconds : 0.0;

    public BackupStatus Status { get; set; } = BackupStatus.Completed;
    public List<string> Errors { get; set; } = new();
}
