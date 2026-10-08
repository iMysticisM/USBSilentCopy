using USBSilentCopy.Core;
using USBSilentCopy.UI;

namespace USBSilentCopy;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        // ۱. تنظیم مدیریت کرش و خطاهای غیرمنتظره سراسری
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            try
            {
                var ex = e.ExceptionObject as Exception;
                var logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "crash.log");
                var dir = Path.GetDirectoryName(logFile);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.AppendAllText(logFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] CRASH: {ex?.Message}\n{ex?.StackTrace}\n\n");
            }
            catch { }
        };

        Application.ThreadException += (s, e) =>
        {
            try
            {
                var logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "crash.log");
                var dir = Path.GetDirectoryName(logFile);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.AppendAllText(logFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] UI_THREAD_EXCEPTION: {e.Exception.Message}\n{e.Exception.StackTrace}\n\n");
            }
            catch { }
        };

        // ۲. پردازش آرگومان‌های خط فرمان
        var forceSilent = args.Any(a => string.Equals(a, "--silent", StringComparison.OrdinalIgnoreCase) || string.Equals(a, "-s", StringComparison.OrdinalIgnoreCase));
        var forceUi = args.Any(a => string.Equals(a, "--ui", StringComparison.OrdinalIgnoreCase) || string.Equals(a, "-u", StringComparison.OrdinalIgnoreCase));
        string? customConfigPath = null;

        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], "--config", StringComparison.OrdinalIgnoreCase) || string.Equals(args[i], "-c", StringComparison.OrdinalIgnoreCase))
            {
                customConfigPath = args[i + 1];
                break;
            }
        }

        // ۳. ساخت هماهنگ‌کننده برنامه
        AppCoordinator coordinator;
        try
        {
            coordinator = new AppCoordinator(customConfigPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در راه‌اندازی هسته برنامه: {ex.Message}", "خطای راه‌اندازی", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // ۴. بررسی اجرای تک‌نسخه‌ای (Single Instance)
        if (!coordinator.InstanceMutex.TryAcquire())
        {
            // نسخه دیگری از برنامه در حال اجراست
            coordinator.Dispose();
            return;
        }

        // ۵. تعیین وضعیت حالت خاموش یا رابط کاربری
        var isSilent = forceSilent || (coordinator.ConfigManager.Current.SilentMode && !forceUi);

        if (isSilent)
        {
            Application.Run(new HeadlessContext(coordinator));
        }
        else
        {
            var trayContext = new TrayAppContext(coordinator);
            if (forceUi)
            {
                trayContext.ShowDashboard();
            }
            Application.Run(trayContext);
        }
    }
}