using USBSilentCopy.Core;
using USBSilentCopy.Queue;

namespace USBSilentCopy.UI;

/// <summary>
/// مدیریت آیکون در سینی ویندوز (System Tray) و منوی راست‌کلیک در حالت Non-Silent
/// </summary>
public sealed class TrayAppContext : ApplicationContext
{
    private readonly AppCoordinator _coordinator;
    private readonly NotifyIcon _notifyIcon;
    private MainDashboardForm? _dashboardForm;

    public TrayAppContext(AppCoordinator coordinator)
    {
        _coordinator = coordinator;

        var icon = IconHelper.GetAppIcon();

        var contextMenu = new ContextMenuStrip();
        var menuStatus = new ToolStripMenuItem("وضعیت: در حال آماده‌باش") { Enabled = false };
        var menuOpen = new ToolStripMenuItem("داشبورد و تنظیمات", null, (s, e) => ShowDashboard());
        var menuPause = new ToolStripMenuItem("توقف موقت / ادامه (Ctrl+O+P)", null, (s, e) => _coordinator.BackupQueue.TogglePause());
        var menuCancel = new ToolStripMenuItem("لغو تمام وظایف صف", null, (s, e) => _coordinator.BackupQueue.CancelAll());
        var menuOpenBackup = new ToolStripMenuItem("باز کردن پوشه Backup", null, (s, e) => OpenBackupFolder());
        var menuOpenLogs = new ToolStripMenuItem("باز کردن پوشه لاگ‌ها", null, (s, e) => OpenLogsFolder());
        var menuExit = new ToolStripMenuItem("خروج", null, (s, e) => ExitApplication());

        contextMenu.Items.AddRange(new ToolStripItem[]
        {
            menuStatus,
            new ToolStripSeparator(),
            menuOpen,
            menuPause,
            menuCancel,
            new ToolStripSeparator(),
            menuOpenBackup,
            menuOpenLogs,
            new ToolStripSeparator(),
            menuExit
        });

        _notifyIcon = new NotifyIcon
        {
            Icon = icon,
            ContextMenuStrip = contextMenu,
            Text = $"{Constants.ApplicationName} - پایش خودکار USB",
            Visible = !_coordinator.ConfigManager.Current.SilentMode
        };

        _notifyIcon.DoubleClick += (s, e) => ShowDashboard();

        // اتصال به رویداد درخواست نمایش رابط کاربری (مثلاً اجرای USBSilentCopy.exe --ui)
        _coordinator.ShowUiRequested += () =>
        {
            if (_dashboardForm != null && !_dashboardForm.IsDisposed)
            {
                _dashboardForm.InvokeSafe(() => ShowDashboard());
            }
            else
            {
                ShowDashboard();
            }
        };

        // واکنش به تغییرات تنظیمات برای فعال یا غیرفعال کردن پویای آیکون سینی ویندوز
        _coordinator.ConfigManager.ConfigChanged += updatedConfig =>
        {
            _notifyIcon.Visible = !updatedConfig.SilentMode;
            if (updatedConfig.SilentMode)
            {
                if (_dashboardForm != null && !_dashboardForm.IsDisposed)
                {
                    _dashboardForm.InvokeSafe(() => _dashboardForm.Close());
                }
            }
        };

        // اتصال رویدادهای صف برای به‌روزرسانی منو و ارسال اعلان‌های اختیاری
        _coordinator.BackupQueue.QueueStatusChanged += () =>
        {
            var isPaused = _coordinator.BackupQueue.IsPaused;
            var active = _coordinator.BackupQueue.ActiveCount;
            var pending = _coordinator.BackupQueue.PendingCount;

            menuStatus.Text = isPaused
                ? "وضعیت: متوقف موقت (Paused)"
                : (active > 0 ? $"وضعیت: در حال پشتیبان‌گیری ({active} فعال، {pending} در صف)" : "وضعیت: آماده‌باش");
        };

        _coordinator.BackupQueue.ItemStarted += item =>
        {
            if (!_coordinator.ConfigManager.Current.SilentMode && _coordinator.ConfigManager.Current.EnableNotifications)
            {
                _notifyIcon.ShowBalloonTip(3000, "شروع پشتیبان‌گیری", $"در حال پشتیبان‌گیری از درایو {item.Device.DisplayName}...", ToolTipIcon.Info);
            }
        };

        _coordinator.BackupQueue.ItemCompleted += (item, result) =>
        {
            if (!_coordinator.ConfigManager.Current.SilentMode && _coordinator.ConfigManager.Current.EnableNotifications)
            {
                var iconType = result.Status == BackupStatus.Completed ? ToolTipIcon.Info : ToolTipIcon.Warning;
                var msg = result.Status == BackupStatus.Completed
                    ? $"پشتیبان‌گیری {item.Device.DisplayName} با موفقیت پایان یافت. ({result.CopiedFiles} فایل)"
                    : $"پشتیبان‌گیری {item.Device.DisplayName} ناقص ماند یا با خطا متوقف شد.";
                _notifyIcon.ShowBalloonTip(4000, "پایان عملیات", msg, iconType);
            }
        };

        _coordinator.Start();
    }

    public void ShowDashboard()
    {
        if (_dashboardForm == null || _dashboardForm.IsDisposed)
        {
            _dashboardForm = new MainDashboardForm(_coordinator);
        }

        _dashboardForm.Show();
        _dashboardForm.WindowState = FormWindowState.Normal;
        _dashboardForm.BringToFront();
        _dashboardForm.Activate();
    }

    private void OpenBackupFolder()
    {
        try
        {
            var path = _coordinator.ConfigManager.ResolveAbsolutePath(_coordinator.ConfigManager.Current.DestinationDirectory);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
        catch { }
    }

    private void OpenLogsFolder()
    {
        try
        {
            var path = _coordinator.ConfigManager.ResolveAbsolutePath(_coordinator.ConfigManager.Current.LogsDirectory);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
        catch { }
    }

    private void ExitApplication()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _coordinator.Stop();
        ExitThread();
    }

    protected override void ExitThreadCore()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        base.ExitThreadCore();
    }
}
