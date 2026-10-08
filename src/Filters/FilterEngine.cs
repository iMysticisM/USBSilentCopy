using USBSilentCopy.Configuration;
using USBSilentCopy.Core;

namespace USBSilentCopy.Filters;

/// <summary>
/// موتور ارزیابی فیلترهای فایل، پسوند، دسته‌بندی، تاریخ و حجم
/// </summary>
public static class FilterEngine
{
    public static FileCategory GetFileCategory(string extension)
    {
        var ext = extension.Trim().ToLowerInvariant();
        if (!ext.StartsWith('.'))
        {
            ext = "." + ext;
        }

        foreach (var (cat, extensions) in Constants.DefaultCategoryExtensions)
        {
            if (extensions.Contains(ext))
            {
                return cat;
            }
        }

        return FileCategory.Other;
    }

    public static bool ShouldIncludeDirectory(string relativeDirPath, FilterConfig config)
    {
        if (string.IsNullOrWhiteSpace(relativeDirPath) || relativeDirPath == ".")
            return true;

        var parts = relativeDirPath.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
            StringSplitOptions.RemoveEmptyEntries);

        // بررسی پوشه‌های مستثنی شده (Exclude Folders)
        if (config.ExcludeFolders.Count > 0)
        {
            foreach (var part in parts)
            {
                if (config.ExcludeFolders.Any(ex => string.Equals(ex.Trim(), part.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }
            }
        }

        // بررسی پوشه‌های مجاز (Include Folders)
        if (config.IncludeFolders.Count > 0)
        {
            var isIncluded = config.IncludeFolders.Any(inc =>
                parts.Any(p => string.Equals(p.Trim(), inc.Trim(), StringComparison.OrdinalIgnoreCase)) ||
                relativeDirPath.StartsWith(inc, StringComparison.OrdinalIgnoreCase));

            if (!isIncluded)
                return false;
        }

        return true;
    }

    public static bool ShouldIncludeFile(FileInfo fileInfo, string relativeFilePath, FilterConfig config)
    {
        // ۱. بررسی پوشه‌های والد
        var relDir = Path.GetDirectoryName(relativeFilePath);
        if (!string.IsNullOrEmpty(relDir) && !ShouldIncludeDirectory(relDir, config))
        {
            return false;
        }

        // ۲. فیلتر حجم فایل (برحسب مگابایت)
        var fileSizeBytes = fileInfo.Length;
        if (config.MinSizeMB.HasValue)
        {
            var minBytes = (long)(config.MinSizeMB.Value * 1024 * 1024);
            if (fileSizeBytes < minBytes)
                return false;
        }

        if (config.MaxSizeMB.HasValue)
        {
            var maxBytes = (long)(config.MaxSizeMB.Value * 1024 * 1024);
            if (fileSizeBytes > maxBytes)
                return false;
        }

        // ۳. فیلتر تاریخ‌ها
        if (config.ModifiedAfter.HasValue && fileInfo.LastWriteTime < config.ModifiedAfter.Value)
            return false;

        if (config.ModifiedBefore.HasValue && fileInfo.LastWriteTime > config.ModifiedBefore.Value)
            return false;

        if (config.CreatedAfter.HasValue && fileInfo.CreationTime < config.CreatedAfter.Value)
            return false;

        if (config.CreatedBefore.HasValue && fileInfo.CreationTime > config.CreatedBefore.Value)
            return false;

        // ۴. فیلتر پسوند
        var ext = fileInfo.Extension;
        if (config.ExcludeExtensions.Count > 0)
        {
            if (config.ExcludeExtensions.Any(e => NormalizeExt(e).Equals(NormalizeExt(ext), StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }
        }

        if (config.IncludeExtensions.Count > 0)
        {
            var isExtIncluded = config.IncludeExtensions.Any(e => NormalizeExt(e).Equals(NormalizeExt(ext), StringComparison.OrdinalIgnoreCase));
            if (!isExtIncluded)
            {
                return false;
            }
        }

        // ۵. فیلتر دسته‌بندی
        var category = GetFileCategory(ext);
        var catName = category.ToString();

        if (config.ExcludeCategories.Count > 0)
        {
            if (config.ExcludeCategories.Any(c => string.Equals(c, catName, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }
        }

        if (config.IncludeCategories.Count > 0)
        {
            var isCatIncluded = config.IncludeCategories.Any(c => string.Equals(c, catName, StringComparison.OrdinalIgnoreCase));
            if (!isCatIncluded)
            {
                return false;
            }
        }

        return true;
    }

    private static string NormalizeExt(string ext)
    {
        var trimmed = ext.Trim();
        return trimmed.StartsWith('.') ? trimmed.ToLowerInvariant() : "." + trimmed.ToLowerInvariant();
    }
}
