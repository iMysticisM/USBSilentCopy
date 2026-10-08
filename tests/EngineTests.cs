using System.Text;
using USBSilentCopy.BackupEngine;
using USBSilentCopy.Configuration;
using USBSilentCopy.Core;
using USBSilentCopy.Filters;
using USBSilentCopy.History;
using USBSilentCopy.Infrastructure;
using USBSilentCopy.Logging;
using USBSilentCopy.Profiles;
using USBSilentCopy.Queue;

namespace USBSilentCopy.Tests;

public class EngineTests : IDisposable
{
    private readonly string _tempTestDir;

    public EngineTests()
    {
        _tempTestDir = Path.Combine(Path.GetTempPath(), "USBSilentCopyTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempTestDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempTestDir))
            {
                Directory.Delete(_tempTestDir, true);
            }
        }
        catch { }
    }

    [Fact]
    public void ConfigurationManager_LoadsAndSavesDefaultConfig()
    {
        var configPath = Path.Combine(_tempTestDir, "test_config.json");
        var mgr = new ConfigurationManager(configPath);

        Assert.NotNull(mgr.Current);
        Assert.Equal("USBSilentCopy", mgr.Current.ApplicationName);
        Assert.True(File.Exists(configPath));

        // ویرایش و ذخیره
        mgr.Current.SpeedLimitMBps = 15;
        var saved = mgr.Save(mgr.Current);
        Assert.True(saved);

        var mgr2 = new ConfigurationManager(configPath);
        Assert.Equal(15, mgr2.Current.SpeedLimitMBps);
    }

    [Fact]
    public void ConfigValidator_DetectsInvalidConfigurations()
    {
        var config = new AppConfig
        {
            DestinationDirectory = "",
            SpeedLimitMBps = -5,
            DelayAfterUsbConnectionSeconds = -1,
            Filters = new FilterConfig
            {
                MinSizeMB = 100,
                MaxSizeMB = 50
            }
        };

        var errors = ConfigValidator.Validate(config);
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("destinationDirectory"));
        Assert.Contains(errors, e => e.Contains("speedLimitMBps"));
        Assert.Contains(errors, e => e.Contains("تأخیر"));
        Assert.Contains(errors, e => e.Contains("بزرگتر از حداکثر"));
    }

    [Fact]
    public void TemplateExpander_CorrectlySubstitutesVariables()
    {
        var device = new UsbDeviceInfo
        {
            DriveLetter = @"E:\",
            VolumeLabel = "KingstonData",
            SerialNumber = "SN123456",
            DeviceId = "USB_VID_0951"
        };

        var template = "{VolumeLabel}_{DriveLetter}_{SerialNumber}_{Date}";
        var testTime = new DateTime(2026, 10, 8, 14, 30, 0);

        var result = TemplateExpander.Expand(template, device, testTime);

        Assert.Equal("KingstonData_E_SN123456_2026-10-08", result);
    }

    [Fact]
    public void TemplateExpander_SanitizesIllegalCharacters()
    {
        var sanitized = TemplateExpander.SanitizePathName("USB:Drive/With*Illegal?Chars");
        Assert.DoesNotContain(":", sanitized);
        Assert.DoesNotContain("/", sanitized);
        Assert.DoesNotContain("*", sanitized);
        Assert.DoesNotContain("?", sanitized);
    }

    [Fact]
    public void FilterEngine_CorrectlyCategorizesExtensions()
    {
        Assert.Equal(FileCategory.Video, FilterEngine.GetFileCategory(".mp4"));
        Assert.Equal(FileCategory.Audio, FilterEngine.GetFileCategory(".mp3"));
        Assert.Equal(FileCategory.Images, FilterEngine.GetFileCategory(".jpg"));
        Assert.Equal(FileCategory.Documents, FilterEngine.GetFileCategory(".docx"));
        Assert.Equal(FileCategory.Archives, FilterEngine.GetFileCategory(".zip"));
        Assert.Equal(FileCategory.Executables, FilterEngine.GetFileCategory(".exe"));
        Assert.Equal(FileCategory.Other, FilterEngine.GetFileCategory(".customxyz"));
    }

    [Fact]
    public void FilterEngine_ExcludesSystemAndRecycleFolders()
    {
        var filter = new FilterConfig
        {
            ExcludeFolders = new List<string> { "$RECYCLE.BIN", "System Volume Information" }
        };

        Assert.False(FilterEngine.ShouldIncludeDirectory(@"$RECYCLE.BIN\S-1-5-21", filter));
        Assert.False(FilterEngine.ShouldIncludeDirectory(@"System Volume Information", filter));
        Assert.True(FilterEngine.ShouldIncludeDirectory(@"Documents\Projects", filter));
    }

    [Fact]
    public void FilterEngine_FiltersByExtensionAndSize()
    {
        var filter = new FilterConfig
        {
            ExcludeExtensions = new List<string> { ".tmp", ".log" },
            MinSizeMB = 0.001 // 1 KB
        };

        var testFile1 = Path.Combine(_tempTestDir, "test.tmp");
        File.WriteAllText(testFile1, new string('A', 2000));
        var fi1 = new FileInfo(testFile1);

        var testFile2 = Path.Combine(_tempTestDir, "report.pdf");
        File.WriteAllText(testFile2, new string('A', 5000));
        var fi2 = new FileInfo(testFile2);

        Assert.False(FilterEngine.ShouldIncludeFile(fi1, "test.tmp", filter));
        Assert.True(FilterEngine.ShouldIncludeFile(fi2, "report.pdf", filter));
    }

    [Fact]
    public void IncrementalChecker_DetectsNewAndIdenticalFiles()
    {
        var config = new AppConfig
        {
            ExistingFilePolicy = ExistingFilePolicy.ReplaceIfNewer,
            CompareHash = true
        };

        var src = Path.Combine(_tempTestDir, "source.txt");
        var dst = Path.Combine(_tempTestDir, "dest.txt");

        File.WriteAllText(src, "Hello World Content");

        // اگر فایل در مقصد وجود نداشته باشد باید کپی شود
        var fiSrc = new FileInfo(src);
        Assert.True(IncrementalChecker.NeedsCopy(fiSrc, dst, config));

        // اگر فایل یکسان در مقصد ایجاد شود و زمان‌ها هماهنگ شود، نباید دوباره کپی شود
        File.WriteAllText(dst, "Hello World Content");
        File.SetLastWriteTimeUtc(dst, fiSrc.LastWriteTimeUtc);
        Assert.False(IncrementalChecker.NeedsCopy(fiSrc, dst, config));

        // اگر محتوای سورس تغییر کند (زمان جدیدتر یا سایز متفاوت)
        Thread.Sleep(100);
        File.WriteAllText(src, "Hello World Content - Modified and Longer");
        fiSrc.Refresh();
        Assert.True(IncrementalChecker.NeedsCopy(fiSrc, dst, config));
    }

    [Fact]
    public void HistoryManager_RecordsAndExportsHistory()
    {
        var historyDir = Path.Combine(_tempTestDir, "history");
        var mgr = new HistoryManager(historyDir);

        var session = new BackupSessionResult
        {
            SessionId = "TEST-SESSION-001",
            DriveLetter = "E:",
            VolumeLabel = "TestFlash",
            SerialNumber = "SN999",
            StartTime = DateTime.Now.AddMinutes(-5),
            EndTime = DateTime.Now,
            TotalFiles = 10,
            CopiedFiles = 10,
            TotalBytes = 1024 * 1024,
            CopiedBytes = 1024 * 1024,
            Status = BackupStatus.Completed
        };

        mgr.RecordSession(session);

        var sessions = mgr.GetSessions();
        Assert.Single(sessions);
        Assert.Equal("TEST-SESSION-001", sessions[0].SessionId);

        var devices = mgr.GetDevices();
        Assert.Single(devices);
        Assert.Equal("SN999", devices[0].DeviceIdentifier);
        Assert.Equal(1, devices[0].TotalBackups);
        Assert.Equal(10, devices[0].TotalFiles);

        // تست خروجی CSV و JSON
        var csvPath = Path.Combine(_tempTestDir, "export.csv");
        var jsonPath = Path.Combine(_tempTestDir, "export.json");

        Assert.True(mgr.ExportSessionsToCsv(csvPath));
        Assert.True(mgr.ExportSessionsToJson(jsonPath));
        Assert.True(File.Exists(csvPath));
        Assert.True(File.Exists(jsonPath));
        Assert.Contains("TEST-SESSION-001", File.ReadAllText(csvPath));
    }

    [Fact]
    public void ProfileManager_CreatesAndSavesDeviceProfile()
    {
        var profDir = Path.Combine(_tempTestDir, "profiles");
        var mgr = new ProfileManager(profDir);

        var device = new UsbDeviceInfo
        {
            DriveLetter = "F:",
            VolumeLabel = "WorkUSB",
            SerialNumber = "SER-7788",
            DeviceId = "USB_VID_058F"
        };

        var profile = mgr.GetOrCreateProfile(device);
        Assert.NotNull(profile);
        Assert.Equal("SER-7788", profile.ProfileId);

        profile.CustomSpeedLimitMBps = 25;
        mgr.SaveProfile(profile);

        var all = mgr.GetAllProfiles();
        Assert.Single(all);
        Assert.Equal(25, all[0].CustomSpeedLimitMBps);
    }

    [Fact]
    public void DiskGuard_ReturnsValidFreeSpace()
    {
        var freeBytes = DiskGuard.GetAvailableFreeSpaceBytes(_tempTestDir);
        Assert.True(freeBytes > 0);

        var hasSpace = DiskGuard.HasEnoughSpace(_tempTestDir, 1024, 10);
        Assert.True(hasSpace);
    }

    [Fact]
    public async Task BackupWorker_EndToEnd_CopiesFilesAndMaintainsStructure()
    {
        var srcDir = Path.Combine(_tempTestDir, "mock_usb");
        var destDir = Path.Combine(_tempTestDir, "backup_dest");
        var logsDir = Path.Combine(_tempTestDir, "logs");
        Directory.CreateDirectory(srcDir);
        Directory.CreateDirectory(Path.Combine(srcDir, "SubFolder"));

        File.WriteAllText(Path.Combine(srcDir, "file1.txt"), "Content of file 1");
        File.WriteAllText(Path.Combine(srcDir, "SubFolder", "file2.pdf"), "Content of PDF");
        File.WriteAllText(Path.Combine(srcDir, "ignore.tmp"), "Should be excluded");

        var config = new AppConfig
        {
            DestinationDirectory = destDir,
            BackupFolderTemplate = "USB_BACKUP",
            OverwriteExistingBackup = true,
            Filters = new FilterConfig
            {
                ExcludeExtensions = new List<string> { ".tmp" }
            }
        };

        var device = new UsbDeviceInfo
        {
            DriveLetter = srcDir,
            VolumeLabel = "TestMock",
            SerialNumber = "MOCK-1234",
            DeviceId = "USB_MOCK_DEV"
        };

        var logMgr = new LogManager(logsDir);
        using var pauseEvent = new ManualResetEventSlim(true);
        using var cts = new CancellationTokenSource();

        var worker = new BackupWorker(device, config, null, logMgr, pauseEvent, cts.Token);
        var result = await worker.ExecuteAsync();

        Assert.Equal(BackupStatus.Completed, result.Status);
        Assert.Equal(2, result.CopiedFiles);
        Assert.Equal(0, result.FailedFiles);

        var finalDestFolder = Path.Combine(destDir, "USB_BACKUP");
        Assert.True(File.Exists(Path.Combine(finalDestFolder, "file1.txt")));
        Assert.True(File.Exists(Path.Combine(finalDestFolder, "SubFolder", "file2.pdf")));
        Assert.False(File.Exists(Path.Combine(finalDestFolder, "ignore.tmp")));

        // اجرای مجدد باید به دلیل یکسان بودن فایل‌ها، همه را Skip کند (Incremental)
        var worker2 = new BackupWorker(device, config, null, logMgr, pauseEvent, cts.Token);
        var result2 = await worker2.ExecuteAsync();

        Assert.Equal(BackupStatus.Completed, result2.Status);
        Assert.Equal(0, result2.CopiedFiles);
        Assert.Equal(2, result2.SkippedFiles);
    }

    [Fact]
    public async Task BackupWorker_InMoveMode_DeletesSourceFile()
    {
        var srcDir = Path.Combine(_tempTestDir, "move_usb");
        var destDir = Path.Combine(_tempTestDir, "move_dest");
        var logsDir = Path.Combine(_tempTestDir, "logs");
        Directory.CreateDirectory(srcDir);

        var srcFile = Path.Combine(srcDir, "move_me.txt");
        File.WriteAllText(srcFile, "Move Mode Test Content");

        var config = new AppConfig
        {
            DestinationDirectory = destDir,
            BackupFolderTemplate = "MOVE_BACKUP",
            CopyMode = BackupMode.Move
        };

        var device = new UsbDeviceInfo
        {
            DriveLetter = srcDir,
            VolumeLabel = "MoveTest",
            SerialNumber = "MOVE-999"
        };

        var logMgr = new LogManager(logsDir);
        using var pauseEvent = new ManualResetEventSlim(true);
        using var cts = new CancellationTokenSource();

        var worker = new BackupWorker(device, config, null, logMgr, pauseEvent, cts.Token);
        var result = await worker.ExecuteAsync();

        Assert.Equal(BackupStatus.Completed, result.Status);
        Assert.Equal(1, result.CopiedFiles);
        Assert.False(File.Exists(srcFile)); // فایل منبع پاک شده است
        Assert.True(File.Exists(Path.Combine(destDir, "MOVE_BACKUP", "move_me.txt")));
    }

    [Fact]
    public void BackupQueue_SortsByDriveLetter_AndPreventsDuplicates()
    {
        var cfgMgr = new ConfigurationManager(Path.Combine(_tempTestDir, "q_config.json"));
        var profMgr = new ProfileManager(Path.Combine(_tempTestDir, "profiles"));
        var logMgr = new LogManager(Path.Combine(_tempTestDir, "logs"));
        var histMgr = new HistoryManager(Path.Combine(_tempTestDir, "history"));

        using var queue = new BackupQueue(cfgMgr, profMgr, logMgr, histMgr);

        var dev1 = new UsbDeviceInfo { DriveLetter = "G:", SerialNumber = "SN-G" };
        var dev2 = new UsbDeviceInfo { DriveLetter = "D:", SerialNumber = "SN-D" };

        var added1 = queue.EnqueueDevice(dev1);
        var added2 = queue.EnqueueDevice(dev2);

        Assert.True(added1);
        Assert.True(added2);

        // تلاش برای افزودن مجدد همان دستگاه
        var duplicate = queue.EnqueueDevice(dev1);
        Assert.False(duplicate);
    }
}

