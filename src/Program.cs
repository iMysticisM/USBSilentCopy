using USBSilentCopy.Core;
using USBSilentCopy.DeviceDetection;
using USBSilentCopy.UI;

namespace USBSilentCopy;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        // ۱. ثبت مدیریت خطاهای بحرانی سراسری
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            HandleCrash(ex, "AppDomain UnhandledException");
        };

        Application.ThreadException += (s, e) =>
        {
            HandleCrash(e.Exception, "UI ThreadException");
        };

        try
        {
            RunApplication(args);
        }
        catch (Exception ex)
        {
            HandleCrash(ex, "Main Process Startup Exception");
        }
    }

    private static void RunApplication(string[] args)
    {
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
        var coordinator = new AppCoordinator(customConfigPath);

        // ۴. بررسی اجرای تک‌نسخه‌ای (Single Instance)
        if (!coordinator.InstanceMutex.TryAcquire())
        {
            // نسخه دیگری از برنامه از قبل در حال اجراست
            if (forceUi)
            {
                // درخواست از پروسس پس‌زمینه برای نمایش پنجره داشبورد
                DeviceDetector.PostMessage((IntPtr)DeviceDetector.HWND_BROADCAST, DeviceDetector.WM_SHOW_UI, IntPtr.Zero, IntPtr.Zero);
            }
            coordinator.Dispose();
            return;
        }

        // ۵. راه‌اندازی کانتکست اجرای برنامه
        var trayContext = new TrayAppContext(coordinator);

        if (forceUi)
        {
            trayContext.ShowDashboard();
        }

        Application.Run(trayContext);
    }

    private static void HandleCrash(Exception? ex, string context)
    {
        var exitCode = ex?.HResult != 0 && ex?.HResult != null ? ex.HResult : 1;
        try
        {
            var timeStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var logDetails = $"[{timeStr}] FATAL CRASH ({context})\n" +
                             $"ExitCode: {exitCode} (0x{exitCode:X8})\n" +
                             $"Type: {ex?.GetType().FullName}\n" +
                             $"Message: {ex?.Message}\n" +
                             $"StackTrace:\n{ex?.StackTrace}\n\n";

            var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
            File.AppendAllText(Path.Combine(logDir, "crash.log"), logDetails);

            // خروج موقت از سایلنت و نمایش پیام صریح و کاربردی خطا
            var userMessage = $"برنامه USBSilentCopy با یک خطای غیرمنتظره مواجه شد و متوقف گردید.\n\n" +
                              $"نوع خطا: {ex?.GetType().Name}\n" +
                              $"پیام خطا: {ex?.Message}\n" +
                              $"کد خروج (Exit Code): {exitCode} (0x{exitCode:X8})\n\n" +
                              $"جزئیات کامل در مسیر زیر ثبت شد:\nlogs\\crash.log\n\n" +
                              $"(توجه: تنظیمات شما از جمله Silent Mode در config.json کاملاً دست‌نخورده و محفوظ باقی مانده است)";

            MessageBox.Show(
                userMessage,
                $"خطای بحرانی در USBSilentCopy (کد خروج: {exitCode})",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch { }
        finally
        {
            Environment.Exit(exitCode);
        }
    }
}