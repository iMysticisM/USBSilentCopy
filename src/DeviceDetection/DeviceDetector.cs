using System.Runtime.InteropServices;
using USBSilentCopy.Core;
using USBSilentCopy.Logging;

namespace USBSilentCopy.DeviceDetection;

/// <summary>
/// شناساگر بلادرنگ اتصال و جداسازی حافظه‌های فلش USB با استفاده از رویدادهای Win32 و تایمر پشتیبان
/// </summary>
public class DeviceDetector : IDisposable
{
    private const int WM_DEVICECHANGE = 0x0219;
    private const int DBT_DEVICEARRIVAL = 0x8000;
    private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
    private const int DBT_DEVTYP_VOLUME = 0x00000002;

    private readonly LogManager _logManager;
    private readonly int _delaySeconds;
    private readonly DeviceChangeWindow _window;
    private readonly System.Threading.Timer _pollingTimer;
    private readonly HashSet<string> _knownDrives = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    public event Action<UsbDeviceInfo>? DeviceConnected;
    public event Action<string>? DeviceDisconnected;

    public DeviceDetector(LogManager logManager, int delaySeconds = 3)
    {
        _logManager = logManager;
        _delaySeconds = Math.Max(0, delaySeconds);

        // ۱. ساخت پنجره مخفی دریافت رویدادهای سیستم‌عامل
        _window = new DeviceChangeWindow(this);

        // ۲. شناسایی درایوهای متصل در زمان شروع برنامه
        foreach (var drive in UsbDeviceQueryHelper.GetAllConnectedUsbDrives())
        {
            _knownDrives.Add(drive.DriveLetter);
        }

        // ۳. تایمر بررسی وضعیت هر ۵ ثانیه یکبار به عنوان پشتیبان برای تضمین پایداری
        _pollingTimer = new System.Threading.Timer(OnPollingCheck, null, 5000, 5000);
    }

    public void CheckConnectedDrivesImmediately()
    {
        OnPollingCheck(null);
    }

    private void OnPollingCheck(object? state)
    {
        try
        {
            var currentDrives = UsbDeviceQueryHelper.GetAllConnectedUsbDrives();
            var currentLetters = new HashSet<string>(currentDrives.Select(d => d.DriveLetter), StringComparer.OrdinalIgnoreCase);

            lock (_lock)
            {
                // تشخیص اتصال جدید
                foreach (var drive in currentDrives)
                {
                    if (!_knownDrives.Contains(drive.DriveLetter))
                    {
                        _knownDrives.Add(drive.DriveLetter);
                        HandleNewDeviceDetected(drive);
                    }
                }

                // تشخیص جدا شدن
                var removedDrives = _knownDrives.Where(d => !currentLetters.Contains(d)).ToList();
                foreach (var removed in removedDrives)
                {
                    _knownDrives.Remove(removed);
                    _logManager.LogApp($"درایو {removed} از سیستم جدا شد.");
                    DeviceDisconnected?.Invoke(removed);
                }
            }
        }
        catch (Exception ex)
        {
            _logManager.LogApp($"خطا در بررسی دوره‌ای درایوها: {ex.Message}", LogLevel.Debug);
        }
    }

    private void HandleNewDeviceDetected(UsbDeviceInfo device)
    {
        _logManager.LogApp($"درایو جدید شناسایی شد: {device.DisplayName} | سریال: {device.SerialNumber}");

        _ = Task.Run(async () =>
        {
            if (_delaySeconds > 0)
            {
                await Task.Delay(_delaySeconds * 1000);
            }

            // بازخوانی مجدد اطلاعات پس از سپری شدن تأخیر
            var refreshedInfo = UsbDeviceQueryHelper.QueryDeviceInfo(device.DriveLetter) ?? device;
            DeviceConnected?.Invoke(refreshedInfo);
        });
    }

    internal void OnDeviceChangeEvent(int eventType, IntPtr lParam)
    {
        try
        {
            if (eventType == DBT_DEVICEARRIVAL)
            {
                var driveLetter = GetDriveLetterFromLParam(lParam);
                if (!string.IsNullOrEmpty(driveLetter))
                {
                    lock (_lock)
                    {
                        if (!_knownDrives.Contains(driveLetter))
                        {
                            var info = UsbDeviceQueryHelper.QueryDeviceInfo(driveLetter);
                            if (info != null)
                            {
                                _knownDrives.Add(driveLetter);
                                HandleNewDeviceDetected(info);
                            }
                        }
                    }
                }
            }
            else if (eventType == DBT_DEVICEREMOVECOMPLETE)
            {
                var driveLetter = GetDriveLetterFromLParam(lParam);
                if (!string.IsNullOrEmpty(driveLetter))
                {
                    lock (_lock)
                    {
                        _knownDrives.Remove(driveLetter);
                    }
                    _logManager.LogApp($"درایو {driveLetter} از سیستم جدا شد (رویداد Win32).");
                    DeviceDisconnected?.Invoke(driveLetter);
                }
            }
        }
        catch (Exception ex)
        {
            _logManager.LogApp($"خطا در پردازش پیام تغییر دستگاه: {ex.Message}", LogLevel.Debug);
        }
    }

    private static string? GetDriveLetterFromLParam(IntPtr lParam)
    {
        if (lParam == IntPtr.Zero)
            return null;

        var devType = Marshal.ReadInt32(lParam, 4);
        if (devType == DBT_DEVTYP_VOLUME)
        {
            var unitMask = Marshal.ReadInt32(lParam, 12);
            for (char c = 'A'; c <= 'Z'; c++)
            {
                if ((unitMask & 0x1) != 0)
                {
                    return c + @":\";
                }
                unitMask >>= 1;
            }
        }
        return null;
    }

    public void Dispose()
    {
        _pollingTimer.Dispose();
        _window.Dispose();
    }

    private sealed class DeviceChangeWindow : NativeWindow, IDisposable
    {
        private readonly DeviceDetector _detector;

        public DeviceChangeWindow(DeviceDetector detector)
        {
            _detector = detector;
            CreateHandle(new CreateParams());
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_DEVICECHANGE)
            {
                _detector.OnDeviceChangeEvent((int)m.WParam, m.LParam);
            }
            base.WndProc(ref m);
        }

        public void Dispose()
        {
            DestroyHandle();
        }
    }
}
