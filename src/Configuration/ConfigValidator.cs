namespace USBSilentCopy.Configuration;

/// <summary>
/// اعتبارسنجی تنظیمات برنامه و گزارش خطاهای احتمالی
/// </summary>
public static class ConfigValidator
{
    public static List<string> Validate(AppConfig config)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(config.DestinationDirectory))
        {
            errors.Add("مسیر پوشه مقصد (destinationDirectory) نمی‌تواند خالی باشد.");
        }

        if (config.EnableSecondaryDestination && string.IsNullOrWhiteSpace(config.SecondaryDestination))
        {
            errors.Add("مسیر مقصد دوم (secondaryDestination) فعال شده ولی مقداری برای آن تعیین نشده است.");
        }

        if (config.DelayAfterUsbConnectionSeconds < 0)
        {
            errors.Add("تأخیر پس از اتصال USB نمی‌تواند منفی باشد.");
        }

        if (config.SpeedLimitMBps < 0)
        {
            errors.Add("محدودیت سرعت (speedLimitMBps) نمی‌تواند مقدار منفی داشته باشد.");
        }

        if (config.MaxConcurrentTasks < 1)
        {
            errors.Add("تعداد وظایف همزمان (maxConcurrentTasks) باید حداقل ۱ باشد.");
        }

        if (config.RetryCount < 0)
        {
            errors.Add("تعداد تلاش مجدد (retryCount) نمی‌تواند منفی باشد.");
        }

        if (config.RetryDelaySeconds < 0)
        {
            errors.Add("فاصله زمانی تلاش مجدد (retryDelaySeconds) نمی‌تواند منفی باشد.");
        }

        if (config.MinimumFreeSpaceMB < 0)
        {
            errors.Add("حداقل فضای خالی مجاز (minimumFreeSpaceMB) نمی‌تواند منفی باشد.");
        }

        if (config.Filters.MinSizeMB.HasValue && config.Filters.MaxSizeMB.HasValue &&
            config.Filters.MinSizeMB.Value > config.Filters.MaxSizeMB.Value)
        {
            errors.Add("حداقل حجم فایل نمی‌تواند بزرگتر از حداکثر حجم فایل باشد.");
        }

        if (config.Filters.ModifiedAfter.HasValue && config.Filters.ModifiedBefore.HasValue &&
            config.Filters.ModifiedAfter.Value > config.Filters.ModifiedBefore.Value)
        {
            errors.Add("تاریخ فیلتر (ModifiedAfter) نمی‌تواند بعد از (ModifiedBefore) باشد.");
        }

        if (config.Filters.CreatedAfter.HasValue && config.Filters.CreatedBefore.HasValue &&
            config.Filters.CreatedAfter.Value > config.Filters.CreatedBefore.Value)
        {
            errors.Add("تاریخ فیلتر (CreatedAfter) نمی‌تواند بعد از (CreatedBefore) باشد.");
        }

        if (config.Logging.MaxLogSizeBytes <= 0)
        {
            errors.Add("حداکثر حجم فایل لاگ باید بزرگتر از صفر باشد.");
        }

        if (config.Logging.MaxLogFiles < 1)
        {
            errors.Add("تعداد نگهداری فایل‌های لاگ باید حداقل ۱ باشد.");
        }

        return errors;
    }
}
