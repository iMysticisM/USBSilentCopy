using USBSilentCopy.Configuration;
using USBSilentCopy.DeviceDetection;
using USBSilentCopy.History;
using USBSilentCopy.Infrastructure;
using USBSilentCopy.Logging;
using USBSilentCopy.Profiles;
using USBSilentCopy.Queue;

namespace USBSilentCopy.Core;

/// <summary>
/// هماهنگ‌کننده مرکزی چرخه حیات نرم‌افزار، اتصال سخت‌افزار، صف پردازش و سیستم‌های جانبی
/// </summary>
public class AppCoordinator : IDisposable
{
    public ConfigurationManager ConfigManager { get; }
    public ProfileManager ProfileManager { get; }
    public LogManager LogManager { get; }
    public HistoryManager HistoryManager { get; }
    public BackupQueue BackupQueue { get; }
    public DeviceDetector DeviceDetector { get; }
    public GlobalHotKeyManager HotKeyManager { get; }
    public SingleInstanceMutex InstanceMutex { get; }

    public event Action<string>? StatusMessageUpdated;

    public AppCoordinator(string? configPath = null)
    {
        InstanceMutex = new SingleInstanceMutex();
        ConfigManager = new ConfigurationManager(configPath);

        var config = ConfigManager.Current;
        var logsDir = ConfigManager.ResolveAbsolutePath(config.LogsDirectory);
        var histDir = ConfigManager.ResolveAbsolutePath(config.HistoryDirectory);
        var profDir = ConfigManager.ResolveAbsolutePath(config.ProfilesDirectory);

        LogManager = new LogManager(logsDir, config.Logging);
        HistoryManager = new HistoryManager(histDir);
        ProfileManager = new ProfileManager(profDir);
        BackupQueue = new BackupQueue(ConfigManager, ProfileManager, LogManager, HistoryManager);
        DeviceDetector = new DeviceDetector(LogManager, config.DelayAfterUsbConnectionSeconds);
        HotKeyManager = new GlobalHotKeyManager();

        WireEvents();
    }

    private void WireEvents()
    {
        // اتصال رویدادهای شناسایی سخت‌افزار به صف پشتیبان‌گیری
        DeviceDetector.DeviceConnected += device =>
        {
            BackupQueue.EnqueueDevice(device);
            StatusMessageUpdated?.Invoke($"درایو {device.DisplayName} دریافت و در صف قرار گرفت.");
        };

        DeviceDetector.DeviceDisconnected += driveLetter =>
        {
            BackupQueue.MarkDeviceDisconnected(driveLetter);
            StatusMessageUpdated?.Invoke($"درایو {driveLetter} قطع شد.");
        };

        // کلید میانبر سراسری Ctrl + O + P
        if (ConfigManager.Current.PauseHotkeyEnabled)
        {
            HotKeyManager.HotKeyPressed += () =>
            {
                BackupQueue.TogglePause();
                var state = BackupQueue.IsPaused ? "متوقف (Pause)" : "در حال اجرا (Resume)";
                StatusMessageUpdated?.Invoke($"وضعیت پشتیبان‌گیری تغییر کرد: {state}");
            };
            HotKeyManager.Start();
        }

        // بررسی تنظیمات Startup
        if (ConfigManager.Current.StartupCurrentUser)
        {
            StartupManager.SetStartupCurrentUser(true);
        }
    }

    public void Start()
    {
        LogManager.LogApp($"نرم‌افزار {Constants.ApplicationName} (نسخه {Constants.ApplicationVersion}) با موفقیت آغاز به کار کرد.");
        StatusMessageUpdated?.Invoke("سیستم آماده به کار و در حال پایش درایوهای USB است.");
        DeviceDetector.CheckConnectedDrivesImmediately();
    }

    public void Stop()
    {
        LogManager.LogApp($"در حال بستن و خروج امن از برنامه {Constants.ApplicationName}...");
        HotKeyManager.Stop();
        BackupQueue.CancelAll();
        DeviceDetector.Dispose();
        BackupQueue.Dispose();
        InstanceMutex.Dispose();
    }

    public void Dispose()
    {
        Stop();
    }
}
