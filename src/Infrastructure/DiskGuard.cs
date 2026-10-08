namespace USBSilentCopy.Infrastructure;

/// <summary>
/// محافظت از پر شدن دیسک مقصد و پایش حداقل فضای خالی مجاز
/// </summary>
public static class DiskGuard
{
    public static long GetAvailableFreeSpaceBytes(string path)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            var root = Path.GetPathRoot(fullPath);
            if (string.IsNullOrEmpty(root))
                return long.MaxValue;

            var driveInfo = new DriveInfo(root);
            return driveInfo.AvailableFreeSpace;
        }
        catch
        {
            return long.MaxValue;
        }
    }

    public static bool HasEnoughSpace(string targetPath, long requiredBytes, long minimumFreeSpaceMB)
    {
        var freeBytes = GetAvailableFreeSpaceBytes(targetPath);
        var minThresholdBytes = minimumFreeSpaceMB * 1024 * 1024;
        return (freeBytes - requiredBytes) >= minThresholdBytes;
    }
}
