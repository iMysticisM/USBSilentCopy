using USBSilentCopy.Configuration;
using USBSilentCopy.Core;
using USBSilentCopy.Profiles;

namespace USBSilentCopy.Logging;

/// <summary>
/// مدیریت جامع سیستم ثبت گزارش و چرخش خودکار فایل‌های لاگ (Log Rotation)
/// </summary>
public class LogManager
{
    private readonly string _logsDirectory;
    private readonly LoggingConfig _config;
    private readonly object _lock = new();

    public event Action<string>? LogMessageReceived;

    public LogManager(string logsDirectory, LoggingConfig? config = null)
    {
        _logsDirectory = Path.GetFullPath(logsDirectory);
        _config = config ?? new LoggingConfig();

        if (!Directory.Exists(_logsDirectory))
        {
            Directory.CreateDirectory(_logsDirectory);
        }

        var devLogsDir = Path.Combine(_logsDirectory, "devices");
        if (!Directory.Exists(devLogsDir))
        {
            Directory.CreateDirectory(devLogsDir);
        }
    }

    public string LogsDirectory => _logsDirectory;

    public void LogApp(string message, LogLevel level = LogLevel.Info, Exception? ex = null)
    {
        if (!_config.EnableLogging || level < _config.MinLogLevel)
            return;

        var fullMessage = FormatLogEntry("APP", level, message, ex);
        WriteToFile(Path.Combine(_logsDirectory, "application.log"), fullMessage);
        LogMessageReceived?.Invoke(fullMessage);
    }

    public void LogError(string errorType, string message, string source = "", string dest = "", Exception? ex = null)
    {
        if (!_config.EnableLogging)
            return;

        var details = $"{errorType} | Src: {source} | Dst: {dest} | Msg: {message}";
        var fullMessage = FormatLogEntry("ERROR", LogLevel.Error, details, ex);
        WriteToFile(Path.Combine(_logsDirectory, "error.log"), fullMessage);
        WriteToFile(Path.Combine(_logsDirectory, "application.log"), fullMessage);
        LogMessageReceived?.Invoke(fullMessage);
    }

    public void LogBackupSession(BackupSessionResult session)
    {
        if (!_config.EnableLogging)
            return;

        var summary = $"[BACKUP_SESSION] ID: {session.SessionId} | Device: {session.DisplayName} | " +
                      $"Status: {session.Status} | Files: {session.CopiedFiles}/{session.TotalFiles} " +
                      $"(Skipped: {session.SkippedFiles}, Failed: {session.FailedFiles}) | " +
                      $"Bytes: {session.CopiedBytes:N0}/{session.TotalBytes:N0} | " +
                      $"Duration: {session.Duration.TotalSeconds:N1}s | Speed: {session.AverageSpeedMBps:N2} MB/s";

        var fullMessage = FormatLogEntry("BACKUP", LogLevel.Info, summary);
        WriteToFile(Path.Combine(_logsDirectory, "backup.log"), fullMessage);

        // لاگ اختصاصی همان دستگاه
        if (!string.IsNullOrWhiteSpace(session.DeviceId) || !string.IsNullOrWhiteSpace(session.SerialNumber))
        {
            var devId = TemplateExpander.SanitizePathName(!string.IsNullOrEmpty(session.SerialNumber) ? session.SerialNumber : session.DeviceId);
            var devLogFile = Path.Combine(_logsDirectory, "devices", $"{devId}.log");
            WriteToFile(devLogFile, fullMessage);
        }

        LogMessageReceived?.Invoke(fullMessage);
    }

    public void LogDevice(string deviceIdentifier, string message, LogLevel level = LogLevel.Info)
    {
        if (!_config.EnableLogging)
            return;

        var devId = TemplateExpander.SanitizePathName(deviceIdentifier);
        var devLogFile = Path.Combine(_logsDirectory, "devices", $"{devId}.log");
        var fullMessage = FormatLogEntry($"DEV:{devId}", level, message);
        WriteToFile(devLogFile, fullMessage);
        LogMessageReceived?.Invoke(fullMessage);
    }

    public List<string> ReadRecentLogs(string logFileName = "application.log", int maxLines = 100)
    {
        var filePath = Path.Combine(_logsDirectory, logFileName);
        if (!File.Exists(filePath))
            return new List<string>();

        try
        {
            lock (_lock)
            {
                var lines = File.ReadAllLines(filePath);
                return lines.TakeLast(maxLines).ToList();
            }
        }
        catch
        {
            return new List<string>();
        }
    }

    private void WriteToFile(string filePath, string message)
    {
        try
        {
            lock (_lock)
            {
                CheckAndRotate(filePath);
                File.AppendAllText(filePath, message + Environment.NewLine);
            }
        }
        catch
        {
            // جلوگیری از کرش برنامه در صورت قفل موقت فایل سیستم
        }
    }

    private void CheckAndRotate(string filePath)
    {
        if (!File.Exists(filePath))
            return;

        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length < _config.MaxLogSizeBytes)
            return;

        // چرخش فایل‌ها: log.9 -> delete, log.8 -> log.9, ..., log -> log.1
        var maxFiles = Math.Max(1, _config.MaxLogFiles);
        var oldestFile = $"{filePath}.{maxFiles}";
        if (File.Exists(oldestFile))
        {
            try { File.Delete(oldestFile); } catch { }
        }

        for (int i = maxFiles - 1; i >= 1; i--)
        {
            var src = $"{filePath}.{i}";
            var dst = $"{filePath}.{i + 1}";
            if (File.Exists(src))
            {
                try { File.Move(src, dst, true); } catch { }
            }
        }

        try
        {
            File.Move(filePath, $"{filePath}.1", true);
        }
        catch { }
    }

    private static string FormatLogEntry(string category, LogLevel level, string message, Exception? ex = null)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var baseStr = $"[{timestamp}] [{level.ToString().ToUpperInvariant()}] [{category}] {message}";
        if (ex != null)
        {
            baseStr += $" | Exception: {ex.GetType().Name}: {ex.Message}";
        }
        return baseStr;
    }
}

