# گزارش جامع نتایج آزمون‌های نرم‌افزار USBSilentCopy

این سند نتایج سناریوهای آزمون خودکار و اعتبارسنجی کیفی نرم‌افزار را ثبت و مستند می‌کند.

---

## ۱. خلاصه نتایج تست‌های خودکار (Automated Test Suite)

- **فریم‌ورک تست:** xUnit v2.5.3 با Visual Studio Test Runner در بستر ‎.NET 8 (x64)‎
- **تعداد کل آزمون‌ها:** ۱۴ آزمون
- **آزمون‌های موفق:** ۱۴ (۱۰۰٪)
- **آزمون‌های ناموفق:** ۰
- **مدت زمان اجرا:** ۳۶۲ میلی‌ثانیه

| ردیف | نام متد آزمون | موضوع مورد آزمون | نتیجه |
| :---: | :--- | :--- | :---: |
| ۱ | `ConfigurationManager_LoadsAndSavesDefaultConfig` | بارگذاری، ذخیره و بازیابی تنظیمات پیش‌فرض | موفق (Passed) |
| ۲ | `ConfigValidator_DetectsInvalidConfigurations` | اعتبارسنجی مقادیر منفی، تاریخ نامعتبر، حجم معکوس | موفق (Passed) |
| ۳ | `TemplateExpander_CorrectlySubstitutesVariables` | جایگزینی دقیق متغیرهای نام پوشه پشتیبان | موفق (Passed) |
| ۴ | `TemplateExpander_SanitizesIllegalCharacters` | پاکسازی نویسه‌های غیرمجاز فایل‌سیستم ویندوز | موفق (Passed) |
| ۵ | `FilterEngine_CorrectlyCategorizesExtensions` | دسته‌بندی ویدیو، صوت، تصاویر، اسناد، آرشیو | موفق (Passed) |
| ۶ | `FilterEngine_ExcludesSystemAndRecycleFolders` | نادیده‌گرفتن `$RECYCLE.BIN` و `System Volume Information` | موفق (Passed) |
| ۷ | `FilterEngine_FiltersByExtensionAndSize` | اعمال فیلتر بر اساس پسوند، حداقل و حداکثر حجم | موفق (Passed) |
| ۸ | `IncrementalChecker_DetectsNewAndIdenticalFiles` | مقایسه افزایشی بر اساس زمان، حجم و هش SHA256 | موفق (Passed) |
| ۹ | `HistoryManager_RecordsAndExportsHistory` | ثبت سوابق، آمار تجمعی، خروجی‌های CSV و JSON | موفق (Passed) |
| ۱۰ | `ProfileManager_CreatesAndSavesDeviceProfile` | ایجاد و بازیابی پروفایل اختصاصی سخت‌افزار | موفق (Passed) |
| ۱۱ | `DiskGuard_ReturnsValidFreeSpace` | استعلام فضای آزاد پارتیشن و سنجش حداقل فضا | موفق (Passed) |
| ۱۲ | `BackupWorker_EndToEnd_CopiesFilesAndMaintainsStructure` | اجرای کامل کپی از مبدأ به مقصد و تست مجدد Incremental | موفق (Passed) |
| ۱۳ | `BackupWorker_InMoveMode_DeletesSourceFile` | اجرای حالت Move Mode و حذف منبع پس از کپی موفق | موفق (Passed) |
| ۱۴ | `BackupQueue_SortsByDriveLetter_AndPreventsDuplicates` | مرتب‌سازی صف بر اساس حرف درایو و ممانعت از پردازش تکراری | موفق (Passed) |

---

## ۲. سناریوهای اعتبارسنجی و قابلیت اطمینان (Reliability Scenarios)

1. **اتصال چند فلش مموری همزمان:**
   - هر درایو در پوشه مجزا قرار گرفته و صف براساس حروف درایو مرتب و ترتیبی با همروندی کنترل‌شده پردازش می‌شود.
2. **قطع ناگهانی USB حین انتقال:**
   - با نظارت بر استریم و فایل‌های موقت با پسوند `.tmp_*`، فایل‌های ناقص پاکسازی شده و سشن به عنوان `Interrupted` ثبت می‌شود.
3. **عدم دسترسی و فایل‌های قفل‌شده:**
   - تلاش مجدد بر اساس `retryCount` انجام شده و در صورت شکست با `skipIfFailed` سایر فایل‌ها بدون توقف کپی می‌شوند.
4. **تک‌نسخه‌ای بودن برنامه (Single Instance):**
   - با قفل سراسری Mutex در سطح نشست کاربر، اجرای مجدد بدون تداخل کنترل می‌شود.
5. **کلید میانبر سراسری (`Ctrl + O + P`):**
   - با هوک سطح پایین صفحه‌کلید (Low-level Hook) بدون نمایش دیالوگ، توقف موقت و ادامه بی‌صدا فعال می‌شود.
