using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using USBSilentCopy.Core;

namespace USBSilentCopy.DeviceDetection;

/// <summary>
/// استخراج اطلاعات فنی، شماره سریال و شناسه درایوهای متصل از طریق Windows API و WMI
/// </summary>
public static class UsbDeviceQueryHelper
{
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool GetVolumeInformation(
        string rootPathName,
        StringBuilder? volumeNameBuffer,
        int volumeNameSize,
        out uint volumeSerialNumber,
        out uint maximumComponentLength,
        out uint fileSystemFlags,
        StringBuilder? fileSystemNameBuffer,
        int fileSystemNameSize);

    public static UsbDeviceInfo? QueryDeviceInfo(string driveLetter)
    {
        try
        {
            var cleanDrive = driveLetter.TrimEnd('\\', ':') + @":\";
            var driveInfo = new DriveInfo(cleanDrive);

            if (!driveInfo.IsReady || driveInfo.DriveType != DriveType.Removable)
            {
                // برخی درایوهای فلش ممکن است به عنوان Fixed شناسایی شوند (Fixed USB)
                if (driveInfo.IsReady && driveInfo.DriveType == DriveType.Fixed)
                {
                    if (!IsUsbDriveViaWmi(cleanDrive))
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }

            var volName = new StringBuilder(261);
            var fsName = new StringBuilder(261);
            uint serialNum = 0;

            GetVolumeInformation(cleanDrive, volName, volName.Capacity, out serialNum, out _, out _, fsName, fsName.Capacity);

            var (hardwareSerial, pnpDeviceId) = QueryHardwareInfoViaWmi(cleanDrive);

            var finalSerial = !string.IsNullOrWhiteSpace(hardwareSerial)
                ? hardwareSerial
                : (serialNum != 0 ? serialNum.ToString("X8") : "00000000");

            return new UsbDeviceInfo
            {
                DriveLetter = cleanDrive,
                VolumeLabel = !string.IsNullOrWhiteSpace(driveInfo.VolumeLabel) ? driveInfo.VolumeLabel : volName.ToString(),
                CapacityBytes = driveInfo.TotalSize,
                FreeSpaceBytes = driveInfo.AvailableFreeSpace,
                FileSystem = !string.IsNullOrWhiteSpace(driveInfo.DriveFormat) ? driveInfo.DriveFormat : fsName.ToString(),
                SerialNumber = finalSerial,
                DeviceId = !string.IsNullOrWhiteSpace(pnpDeviceId) ? pnpDeviceId : $"USB_{finalSerial}",
                Status = DeviceStatus.Detected,
                ConnectedTime = DateTime.Now
            };
        }
        catch
        {
            return null;
        }
    }

    public static List<UsbDeviceInfo> GetAllConnectedUsbDrives()
    {
        var result = new List<UsbDeviceInfo>();
        foreach (var drive in DriveInfo.GetDrives())
        {
            try
            {
                if (drive.IsReady && (drive.DriveType == DriveType.Removable || IsUsbDriveViaWmi(drive.Name)))
                {
                    var info = QueryDeviceInfo(drive.Name);
                    if (info != null)
                    {
                        result.Add(info);
                    }
                }
            }
            catch { }
        }
        return result;
    }

    private static (string HardwareSerial, string PnpDeviceId) QueryHardwareInfoViaWmi(string driveRoot)
    {
        try
        {
            var driveLetterOnly = driveRoot.TrimEnd('\\');
            var partitionQuery = $"ASSOCIATORS OF {{Win32_LogicalDisk.DeviceID='{driveLetterOnly}'}} WHERE AssocClass=Win32_LogicalDiskToPartition";
            using var partitionSearcher = new ManagementObjectSearcher(partitionQuery);

            foreach (ManagementObject partition in partitionSearcher.Get())
            {
                var diskQuery = $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{partition["DeviceID"]}'}} WHERE AssocClass=Win32_DiskDriveToDiskPartition";
                using var diskSearcher = new ManagementObjectSearcher(diskQuery);

                foreach (ManagementObject disk in diskSearcher.Get())
                {
                    var pnpId = disk["PNPDeviceID"]?.ToString() ?? string.Empty;
                    var serial = disk["SerialNumber"]?.ToString()?.Trim() ?? string.Empty;

                    // استخراج سریال نامبر از انتهای PNPDeviceID در صورت خالی بودن SerialNumber
                    if (string.IsNullOrWhiteSpace(serial) && !string.IsNullOrWhiteSpace(pnpId))
                    {
                        var lastSlash = pnpId.LastIndexOf('\\');
                        if (lastSlash >= 0 && lastSlash < pnpId.Length - 1)
                        {
                            var candidate = pnpId[(lastSlash + 1)..].Split('&')[0];
                            if (!string.IsNullOrWhiteSpace(candidate))
                            {
                                serial = candidate;
                            }
                        }
                    }

                    return (serial, pnpId);
                }
            }
        }
        catch { }

        return (string.Empty, string.Empty);
    }

    private static bool IsUsbDriveViaWmi(string driveRoot)
    {
        try
        {
            var driveLetterOnly = driveRoot.TrimEnd('\\');
            var query = $"ASSOCIATORS OF {{Win32_LogicalDisk.DeviceID='{driveLetterOnly}'}} WHERE AssocClass=Win32_LogicalDiskToPartition";
            using var searcher = new ManagementObjectSearcher(query);

            foreach (ManagementObject partition in searcher.Get())
            {
                var diskQuery = $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{partition["DeviceID"]}'}} WHERE AssocClass=Win32_DiskDriveToDiskPartition";
                using var diskSearcher = new ManagementObjectSearcher(diskQuery);

                foreach (ManagementObject disk in diskSearcher.Get())
                {
                    var interfaceType = disk["InterfaceType"]?.ToString() ?? string.Empty;
                    if (string.Equals(interfaceType, "USB", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
        }
        catch { }

        return false;
    }
}
