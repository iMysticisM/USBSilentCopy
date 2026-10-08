using System.Text.RegularExpressions;
using USBSilentCopy.Core;

namespace USBSilentCopy.Profiles;

/// <summary>
/// جایگزینی متغیرهای الگوی نام‌گذاری پوشه پشتیبان
/// </summary>
public static class TemplateExpander
{
    public static string Expand(string template, UsbDeviceInfo device, DateTime? timestamp = null)
    {
        var now = timestamp ?? DateTime.Now;
        var cleanDrive = (device.DriveLetter ?? "USB").TrimEnd(':', '\\', '/');
        var cleanLabel = string.IsNullOrWhiteSpace(device.VolumeLabel) ? "USB" : SanitizePathName(device.VolumeLabel);
        var cleanSerial = string.IsNullOrWhiteSpace(device.SerialNumber) ? "UNKNOWN" : SanitizePathName(device.SerialNumber);
        var cleanDevId = string.IsNullOrWhiteSpace(device.DeviceId) ? "DEV" : SanitizePathName(device.DeviceId);

        var dateStr = now.ToString("yyyy-MM-dd");
        var timeStr = now.ToString("HH-mm-ss");

        var result = template
            .Replace("{DriveLetter}", cleanDrive, StringComparison.OrdinalIgnoreCase)
            .Replace("{VolumeLabel}", cleanLabel, StringComparison.OrdinalIgnoreCase)
            .Replace("{SerialNumber}", cleanSerial, StringComparison.OrdinalIgnoreCase)
            .Replace("{DeviceId}", cleanDevId, StringComparison.OrdinalIgnoreCase)
            .Replace("{Date}", dateStr, StringComparison.OrdinalIgnoreCase)
            .Replace("{Time}", timeStr, StringComparison.OrdinalIgnoreCase);

        return SanitizePathName(result);
    }

    public static string SanitizePathName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());
        return Regex.Replace(sanitized, @"[_\s]+", "_").Trim('_');
    }
}
