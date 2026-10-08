using System.Security.Cryptography;
using USBSilentCopy.Configuration;
using USBSilentCopy.Core;

namespace USBSilentCopy.BackupEngine;

/// <summary>
/// بررسی افزایشی (Incremental) فایل‌ها جهت تشخیص نیاز به کپی یا صرف‌نظر
/// </summary>
public static class IncrementalChecker
{
    public static bool NeedsCopy(FileInfo sourceFile, string destinationFilePath, AppConfig config)
    {
        if (!File.Exists(destinationFilePath))
            return true;

        // در صورت فعال بودن سیاست کپی فقط فایل‌های جدید
        if (config.CopyOnlyNew)
        {
            return false;
        }

        var destInfo = new FileInfo(destinationFilePath);

        switch (config.ExistingFilePolicy)
        {
            case ExistingFilePolicy.Overwrite:
                return true;

            case ExistingFilePolicy.Skip:
                if (config.CompareHash)
                {
                    return !AreHashesEqual(sourceFile.FullName, destinationFilePath);
                }
                return false;

            case ExistingFilePolicy.ReplaceIfNewer:
                // مقایسه با دقت یک ثانیه جهت تطابق با فایل‌سیستم‌های FAT32
                var timeDiff = sourceFile.LastWriteTimeUtc - destInfo.LastWriteTimeUtc;
                if (timeDiff.TotalSeconds > 2)
                {
                    return true;
                }

                if (sourceFile.Length != destInfo.Length)
                {
                    return true;
                }

                if (config.CompareHash)
                {
                    return !AreHashesEqual(sourceFile.FullName, destinationFilePath);
                }

                return false;

            case ExistingFilePolicy.CreateNewBackupFolder:
                return true;

            default:
                return true;
        }
    }

    public static bool AreHashesEqual(string file1Path, string file2Path)
    {
        try
        {
            using var sha = SHA256.Create();
            using var stream1 = File.OpenRead(file1Path);
            using var stream2 = File.OpenRead(file2Path);

            var hash1 = sha.ComputeHash(stream1);
            var hash2 = sha.ComputeHash(stream2);

            return hash1.SequenceEqual(hash2);
        }
        catch
        {
            // در صورت بروز هرگونه خطای محاسبه، برای اطمینان فایل دوباره کپی می‌شود
            return false;
        }
    }
}
