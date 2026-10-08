using System.Diagnostics;
using USBSilentCopy.Configuration;
using USBSilentCopy.Core;
using USBSilentCopy.Filters;
using USBSilentCopy.Infrastructure;
using USBSilentCopy.Logging;
using USBSilentCopy.Profiles;

namespace USBSilentCopy.BackupEngine;

/// <summary>
/// موتور اجرایی کپی، بررسی خطاها، پایش قطع USB، اعمال فیلترها و محدودیت سرعت
/// </summary>
public class BackupWorker
{
    private readonly UsbDeviceInfo _device;
    private readonly AppConfig _config;
    private readonly UsbProfile? _profile;
    private readonly LogManager _logManager;
    private readonly ManualResetEventSlim _pauseEvent;
    private readonly CancellationToken _cancellationToken;

    public event Action<BackupProgressInfo>? ProgressChanged;
    public event Action<BackupStatus>? StatusChanged;

    public BackupWorker(
        UsbDeviceInfo device,
        AppConfig config,
        UsbProfile? profile,
        LogManager logManager,
        ManualResetEventSlim pauseEvent,
        CancellationToken cancellationToken)
    {
        _device = device;
        _config = config;
        _profile = profile;
        _logManager = logManager;
        _pauseEvent = pauseEvent;
        _cancellationToken = cancellationToken;
    }

    public async Task<BackupSessionResult> ExecuteAsync()
    {
        var session = new BackupSessionResult
        {
            DeviceId = _device.DeviceId,
            SerialNumber = _device.SerialNumber,
            DriveLetter = _device.DriveLetter,
            VolumeLabel = _device.VolumeLabel,
            SourcePath = _device.DriveLetter,
            StartTime = DateTime.Now,
            Status = BackupStatus.Scanning
        };

        StatusChanged?.Invoke(BackupStatus.Scanning);
        _logManager.LogDevice(_device.ProfileIdentifier, $"شروع بررسی و اسکن درایو {_device.DisplayName} برای پشتیبان‌گیری.");

        try
        {
            // ۱. تعیین مسیر پوشه مقصد اصلی
            var baseDest = ResolveDestinationPath(_profile?.CustomDestination ?? _config.DestinationDirectory);
            var folderTemplate = _profile?.BackupFolderTemplate ?? _config.BackupFolderTemplate;
            var targetFolderName = TemplateExpander.Expand(folderTemplate, _device, session.StartTime);

            var primaryDestFolder = Path.Combine(baseDest, targetFolderName);

            // در صورتی که گزینه بازنویسی پشتیبان قبلی غیرفعال باشد، پوشه جدید با شماره افزایشی ایجاد می‌شود
            if (!_config.OverwriteExistingBackup && Directory.Exists(primaryDestFolder))
            {
                primaryDestFolder = GetUniqueDirectoryPath(baseDest, targetFolderName);
            }

            Directory.CreateDirectory(primaryDestFolder);
            session.DestinationPath = primaryDestFolder;

            // مسیر مقصد ثانویه در صورت فعال بودن
            string? secondaryDestFolder = null;
            if (_config.EnableSecondaryDestination && !string.IsNullOrWhiteSpace(_config.SecondaryDestination))
            {
                var baseSec = ResolveDestinationPath(_config.SecondaryDestination);
                secondaryDestFolder = Path.Combine(baseSec, targetFolderName);
                Directory.CreateDirectory(secondaryDestFolder);
            }

            // ۲. بررسی شرط اولیه حداقل فضای خالی دیسک
            if (_config.PreventDiskFull && !DiskGuard.HasEnoughSpace(primaryDestFolder, 0, _config.MinimumFreeSpaceMB))
            {
                var msg = $"فضای خالی درایو مقصد کمتر از حد مجاز تعیین‌شده ({_config.MinimumFreeSpaceMB} MB) است.";
                session.Errors.Add(msg);
                session.Status = BackupStatus.Failed;
                _logManager.LogError("DISK_FULL", msg, _device.DriveLetter, primaryDestFolder);
                return FinishSession(session);
            }

            // ۳. اسکن و جمع‌آوری فایل‌ها و پوشه‌ها
            var filterConfig = _profile?.CustomFilters ?? _config.Filters;
            var candidateFiles = new List<(FileInfo File, string RelativePath)>();
            var candidateDirs = new List<string>();

            ScanDirectoryRecursively(new DirectoryInfo(_device.DriveLetter), _device.DriveLetter, filterConfig, candidateFiles, candidateDirs);

            session.TotalFiles = candidateFiles.Count;
            session.TotalBytes = candidateFiles.Sum(f => f.File.Length);

            _logManager.LogDevice(_device.ProfileIdentifier, $"تعداد {candidateFiles.Count} فایل ({session.TotalBytes / (1024 * 1024):N1} MB) برای کپی یافت شد.");

            // در صورت فعال بودن، ساختار پوشه‌های خالی در مقصد ایجاد می‌شود
            if (_config.CopyEmptyDirectories)
            {
                foreach (var relDir in candidateDirs)
                {
                    var targetDir = Path.Combine(primaryDestFolder, relDir);
                    if (!Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }
                    if (secondaryDestFolder != null)
                    {
                        var secDir = Path.Combine(secondaryDestFolder, relDir);
                        if (!Directory.Exists(secDir))
                        {
                            Directory.CreateDirectory(secDir);
                        }
                    }
                }
            }

            // ۴. فاز کپی فایل‌ها
            session.Status = BackupStatus.Copying;
            StatusChanged?.Invoke(BackupStatus.Copying);

            var speedLimit = _profile?.CustomSpeedLimitMBps ?? _config.SpeedLimitMBps;
            var progress = new BackupProgressInfo
            {
                DeviceIdentifier = _device.ProfileIdentifier,
                TotalFiles = candidateFiles.Count,
                TotalBytes = session.TotalBytes,
                Status = BackupStatus.Copying
            };

            for (int i = 0; i < candidateFiles.Count; i++)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                // توقف موقت (Pause) در صورت فعال بودن
                _pauseEvent.Wait(_cancellationToken);

                var item = candidateFiles[i];
                progress.CurrentFile = item.RelativePath;
                progress.CurrentFileIndex = i + 1;

                // بررسی آنلاین بودن درایو مبدأ (تشخیص قطع ناگهانی درایو USB)
                if (!IsSourceDriveAvailable(_device.DriveLetter))
                {
                    var disconnectMsg = $"درایو USB {_device.DisplayName} حین عملیات کپی از سیستم جدا شد.";
                    session.Errors.Add(disconnectMsg);
                    session.Status = BackupStatus.Interrupted;
                    _logManager.LogError("USB_DISCONNECT", disconnectMsg, _device.DriveLetter, primaryDestFolder);
                    return FinishSession(session);
                }

                var primaryTargetFile = Path.Combine(primaryDestFolder, item.RelativePath);
                var secTargetFile = secondaryDestFolder != null ? Path.Combine(secondaryDestFolder, item.RelativePath) : null;

                // بررسی شرط پر شدن دیسک برای فایل فعلی
                if (_config.PreventDiskFull && !DiskGuard.HasEnoughSpace(primaryDestFolder, item.File.Length, _config.MinimumFreeSpaceMB))
                {
                    var fullMsg = $"فضای ذخیره‌سازی درایو مقصد برای کپی فایل {item.RelativePath} کافی نیست.";
                    session.Errors.Add(fullMsg);
                    session.Status = BackupStatus.Failed;
                    _logManager.LogError("DISK_FULL", fullMsg, item.File.FullName, primaryTargetFile);
                    return FinishSession(session);
                }

                // بررسی افزایشی (Incremental)
                if (!IncrementalChecker.NeedsCopy(item.File, primaryTargetFile, _config))
                {
                    session.SkippedFiles++;
                    progress.BytesTransferred += item.File.Length;
                    ProgressChanged?.Invoke(progress);
                    continue;
                }

                // تلاش برای کپی با مدیریت Retry
                var copySuccess = await CopyFileWithRetryAsync(item.File, primaryTargetFile, speedLimit, progress);
                if (copySuccess)
                {
                    // کپی به مقصد دوم در صورت فعال بودن
                    if (secTargetFile != null)
                    {
                        await CopyFileWithRetryAsync(item.File, secTargetFile, speedLimit, null);
                    }

                    session.CopiedFiles++;
                    session.CopiedBytes += item.File.Length;

                    // در حالت انتقال (Move Mode)
                    if (_config.CopyMode == BackupMode.Move)
                    {
                        TryDeleteSourceFile(item.File.FullName);
                    }
                }
                else
                {
                    session.FailedFiles++;
                    session.Errors.Add($"خطا در انتقال فایل: {item.RelativePath}");
                    if (!_config.SkipIfFailed)
                    {
                        session.Status = BackupStatus.Failed;
                        break;
                    }
                }

                ProgressChanged?.Invoke(progress);
            }

            if (session.Status != BackupStatus.Failed && session.Status != BackupStatus.Interrupted)
            {
                session.Status = BackupStatus.Completed;
            }
        }
        catch (OperationCanceledException)
        {
            session.Status = BackupStatus.Cancelled;
            session.Errors.Add("عملیات پشتیبان‌گیری توسط کاربر لغو شد.");
            _logManager.LogDevice(_device.ProfileIdentifier, "پشتیبان‌گیری لغو شد.", LogLevel.Warning);
        }
        catch (Exception ex)
        {
            session.Status = BackupStatus.Failed;
            session.Errors.Add($"خطای سیستمی غیرمنتظره: {ex.Message}");
            _logManager.LogError("ENGINE_CRASH", ex.Message, _device.DriveLetter, "", ex);
        }

        return FinishSession(session);
    }

    private async Task<bool> CopyFileWithRetryAsync(
        FileInfo sourceFile,
        string destinationFilePath,
        int speedLimitMBps,
        BackupProgressInfo? progress)
    {
        var targetDir = Path.GetDirectoryName(destinationFilePath);
        if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        var attempts = 0;
        var maxAttempts = Math.Max(1, _config.RetryCount + 1);

        while (attempts < maxAttempts)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            _pauseEvent.Wait(_cancellationToken);
            attempts++;

            try
            {
                // ایجاد فایل موقت در مقصد برای جلوگیری از باقی ماندن فایل ناقص در صورت قطع اتصال
                var tempDestFile = destinationFilePath + ".tmp_" + Guid.NewGuid().ToString("N")[..8];

                await using (var sourceStream = new FileStream(sourceFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 64 * 1024, true))
                await using (var targetStream = new FileStream(tempDestFile, FileMode.Create, FileAccess.Write, FileShare.None, 64 * 1024, true))
                {
                    Stream copyStream = speedLimitMBps > 0 ? new ThrottledStream(targetStream, speedLimitMBps) : targetStream;
                    var buffer = new byte[64 * 1024];
                    int bytesRead;

                    var fileStopwatch = Stopwatch.StartNew();
                    long fileBytesCopied = 0;

                    while ((bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length, _cancellationToken)) > 0)
                    {
                        _pauseEvent.Wait(_cancellationToken);

                        if (!IsSourceDriveAvailable(_device.DriveLetter))
                        {
                            throw new IOException("درایو مبدأ در حین انتقال فایل جدا شد.");
                        }

                        await copyStream.WriteAsync(buffer, 0, bytesRead, _cancellationToken);
                        fileBytesCopied += bytesRead;

                        if (progress != null)
                        {
                            progress.BytesTransferred += bytesRead;
                            var elapsed = fileStopwatch.Elapsed.TotalSeconds;
                            if (elapsed > 0.05)
                            {
                                progress.SpeedMBps = (fileBytesCopied / (1024.0 * 1024.0)) / elapsed;
                            }
                            ProgressChanged?.Invoke(progress);
                        }
                    }

                    await copyStream.FlushAsync(_cancellationToken);
                }

                // جایگزینی فایل موقت با فایل نهایی
                if (File.Exists(destinationFilePath))
                {
                    File.Delete(destinationFilePath);
                }
                File.Move(tempDestFile, destinationFilePath);

                // حفظ زمان‌های اصلی فایل برای هماهنگی Incremental
                try
                {
                    File.SetCreationTimeUtc(destinationFilePath, sourceFile.CreationTimeUtc);
                    File.SetLastWriteTimeUtc(destinationFilePath, sourceFile.LastWriteTimeUtc);
                }
                catch { }

                return true;
            }
            catch (OperationCanceledException)
            {
                CleanUpPartialFile(destinationFilePath);
                throw;
            }
            catch (Exception ex)
            {
                CleanUpPartialFile(destinationFilePath);
                _logManager.LogError("FILE_COPY_ATTEMPT_FAILED",
                    $"تلاش {attempts}/{maxAttempts} برای کپی {sourceFile.Name} با شکست مواجه شد: {ex.Message}",
                    sourceFile.FullName, destinationFilePath);

                if (attempts < maxAttempts)
                {
                    var delayMs = Math.Max(100, _config.RetryDelaySeconds * 1000);
                    await Task.Delay(delayMs, _cancellationToken);
                }
            }
        }

        return false;
    }

    private void ScanDirectoryRecursively(
        DirectoryInfo rootDir,
        string baseSourcePath,
        FilterConfig filterConfig,
        List<(FileInfo File, string RelativePath)> candidateFiles,
        List<string> candidateDirs)
    {
        try
        {
            var relPath = Path.GetRelativePath(baseSourcePath, rootDir.FullName);
            if (!FilterEngine.ShouldIncludeDirectory(relPath, filterConfig))
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(relPath) && relPath != ".")
            {
                candidateDirs.Add(relPath);
            }

            FileInfo[] files;
            try
            {
                files = rootDir.GetFiles();
            }
            catch
            {
                // در صورت عدم دسترسی به یک پوشه محافظت‌شده سیستم‌عامل
                return;
            }

            foreach (var file in files)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                var relFilePath = Path.GetRelativePath(baseSourcePath, file.FullName);
                if (FilterEngine.ShouldIncludeFile(file, relFilePath, filterConfig))
                {
                    candidateFiles.Add((file, relFilePath));
                }
            }

            DirectoryInfo[] subDirs;
            try
            {
                subDirs = rootDir.GetDirectories();
            }
            catch
            {
                return;
            }

            foreach (var subDir in subDirs)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                ScanDirectoryRecursively(subDir, baseSourcePath, filterConfig, candidateFiles, candidateDirs);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // نادیده گرفتن پوشه‌های با عدم دسترسی بدون نمایش پیام ادمین
        }
    }

    private static bool IsSourceDriveAvailable(string driveLetter)
    {
        try
        {
            var root = Path.GetPathRoot(driveLetter);
            if (string.IsNullOrEmpty(root))
                return false;

            return Directory.Exists(root);
        }
        catch
        {
            return false;
        }
    }

    private static void CleanUpPartialFile(string targetFilePath)
    {
        try
        {
            var dir = Path.GetDirectoryName(targetFilePath);
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
                return;

            var pattern = Path.GetFileName(targetFilePath) + ".tmp_*";
            foreach (var tmp in Directory.GetFiles(dir, pattern))
            {
                try { File.Delete(tmp); } catch { }
            }
        }
        catch { }
    }

    private static void TryDeleteSourceFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.SetAttributes(filePath, FileAttributes.Normal);
                File.Delete(filePath);
            }
        }
        catch { }
    }

    private BackupSessionResult FinishSession(BackupSessionResult session)
    {
        session.EndTime = DateTime.Now;
        StatusChanged?.Invoke(session.Status);
        _logManager.LogBackupSession(session);
        return session;
    }

    private static string ResolveDestinationPath(string path)
    {
        if (Path.IsPathRooted(path))
            return path;

        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        return Path.GetFullPath(Path.Combine(baseDir, path));
    }

    private static string GetUniqueDirectoryPath(string parentDir, string baseName)
    {
        var counter = 2;
        string candidate;
        do
        {
            candidate = Path.Combine(parentDir, $"{baseName} ({counter})");
            counter++;
        } while (Directory.Exists(candidate));

        return candidate;
    }
}
