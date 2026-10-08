using Microsoft.Win32;
using USBSilentCopy.Core;

namespace USBSilentCopy.Infrastructure;

/// <summary>
/// مدیریت ثبت اجرای خودکار برنامه هنگام ورود به ویندوز (Startup)
/// </summary>
public static class StartupManager
{
    private const string RunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public static bool IsStartupEnabledCurrentUser(string appName = Constants.ApplicationName)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, false);
            return key?.GetValue(appName) != null;
        }
        catch
        {
            return false;
        }
    }

    public static bool SetStartupCurrentUser(bool enable, string appName = Constants.ApplicationName, string? executablePath = null)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true);
            if (key == null)
                return false;

            if (enable)
            {
                var exePath = executablePath ?? Environment.ProcessPath ?? Application.ExecutablePath;
                key.SetValue(appName, $"\"{exePath}\"");
            }
            else
            {
                if (key.GetValue(appName) != null)
                {
                    key.DeleteValue(appName, false);
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool SetStartupAllUsers(bool enable, string appName = Constants.ApplicationName, string? executablePath = null)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(RunRegistryKey, true);
            if (key == null)
                return false;

            if (enable)
            {
                var exePath = executablePath ?? Environment.ProcessPath ?? Application.ExecutablePath;
                key.SetValue(appName, $"\"{exePath}\"");
            }
            else
            {
                if (key.GetValue(appName) != null)
                {
                    key.DeleteValue(appName, false);
                }
            }

            return true;
        }
        catch
        {
            // در صورت عدم دسترسی ادمین خطای امنیتی رخ می‌دهد
            return false;
        }
    }
}
