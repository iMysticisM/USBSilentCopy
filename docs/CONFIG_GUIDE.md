# راهنمای جامع تنظیمات فایل config.json

تمام رفتارها و عملکردهای نرم‌افزار **USBSilentCopy** از طریق فایل `config.json` قابل سفارشی‌سازی است. در این سند، هر یک از فیلدها و کاربرد دقیق آن‌ها شرح داده شده است.

---

## ۱. تنظیمات عمومی و مسیرها (General & Paths)

| کلید تنظیمات | نوع داده | مقدار پیش‌فرض | توضیحات |
| :--- | :--- | :--- | :--- |
| `applicationName` | رشته | `"USBSilentCopy"` | نام برنامه در سیستم‌عامل و رجیستری |
| `destinationDirectory` | رشته | `"Backup"` | مسیر اصلی ذخیره فایل‌های پشتیبان (می‌تواند نسبی یا مطلق باشد مانند `D:\Backups`) |
| `secondaryDestination` | رشته | `""` | مسیر مقصد دوم در صورت تمایل به ذخیره نسخه همزمان در دو محل مختلف |
| `enableSecondaryDestination` | بولی | `false` | فعال یا غیرفعال بودن کپی همزمان در مقصد دوم |
| `logsDirectory` | رشته | `"logs"` | پوشه ذخیره فایل‌های گزارش و وقایع سیستم |
| `historyDirectory` | رشته | `"history"` | پوشه ذخیره تاریخچه سوابق پشتیبان‌گیری و آمار دستگاه‌ها |
| `profilesDirectory` | رشته | `"profiles"` | پوشه نگهداری پروفایل‌های اختصاصی درایوهای USB |
| `delayAfterUsbConnectionSeconds`| عدد صحیح | `3` | زمان انتظار پس از شناسایی اتصال USB تا شروع خواندن (جهت تثبیت مانت شدن ویندوز) |

---

## ۲. حالت اجرا و سینی ویندوز (Execution & Silent Mode)

| کلید تنظیمات | نوع داده | مقدار پیش‌فرض | توضیحات |
| :--- | :--- | :--- | :--- |
| `silentMode` | بولی | `false` | در صورت `true`: برنامه کاملاً خاموش و بدون آیکون سینی و اعلان در پس‌زمینه اجرا می‌شود. |
| `enableNotifications` | بولی | `true` | نمایش بالون اعلان در سینی ویندوز هنگام شروع و پایان کپی (فقط در حالت غیر-سایلنت) |
| `startupCurrentUser` | بولی | `true` | اجرای خودکار هنگام ورود به ویندوز برای کاربر جاری (بدون نیاز به ادمین) |
| `startupAllUsers` | بولی | `false` | اجرای خودکار برای تمام کاربران ویندوز (نیازمند دسترسی Administrator) |
| `pauseHotkeyEnabled` | بولی | `true` | فعال بودن کلید میانبر سراسری `Ctrl + O + P` جهت توقف موقت و ادامه پشتیبان‌گیری |

---

## ۳. موتور انتقال و محافظت از دیسک (Backup Engine & Safety)

| کلید تنظیمات | نوع داده | مقدار پیش‌فرض | توضیحات |
| :--- | :--- | :--- | :--- |
| `copyMode` | رشته | `"Copy"` | حالت انتقال فایل: `"Copy"` (منبع دست‌نخورده می‌ماند) یا `"Move"` (حذف منبع پس از کپی موفق) |
| `speedLimitMBps` | عدد صحیح | `0` | محدودیت پهنای باند انتقال برحسب مگابایت بر ثانیه (`0` یعنی سرعت نامحدود) |
| `maxConcurrentTasks` | عدد صحیح | `1` | حداکثر تعداد پردازش‌های همزمان درایوها |
| `retryCount` | عدد صحیح | `3` | تعداد دفعات تلاش مجدد در صورت خطای خواندن یا نوشتن فایل |
| `retryDelaySeconds` | عدد صحیح | `5` | فاصله زمانی بین هر بار تلاش مجدد (ثانیه) |
| `skipIfFailed` | بولی | `true` | در صورت شکست نهایی کپی یک فایل، از آن صرف‌نظر کرده و سایر فایل‌ها کپی شوند |
| `preventDiskFull` | بولی | `true` | بررسی فضای دیسک و جلوگیری از پر شدن کامل پارتیشن مقصد |
| `minimumFreeSpaceMB` | عدد | `500` | حداقل فضای خالی مجاز در مقصد؛ اگر فضا کمتر از این مقدار باشد کپی متوقف می‌شود |

---

## ۴. مقایسه افزایشی و نام‌گذاری (Incremental & Naming)

| کلید تنظیمات | نوع داده | مقدار پیش‌فرض | توضیحات |
| :--- | :--- | :--- | :--- |
| `skipExisting` | بولی | `true` | عدم کپی مجدد فایل‌های بدون تغییر در مقصد |
| `copyOnlyNew` | بولی | `false` | صرفاً کپی فایل‌هایی که اصلاً در مقصد وجود ندارند |
| `compareHash` | بولی | `false` | مقایسه دقیق هش الگوریتم SHA256 برای تضمین ۱۰۰٪ یکسان بودن محتوای فایل |
| `existingFilePolicy` | رشته | `"ReplaceIfNewer"`| سیاست برخورد با فایل موجود: `"ReplaceIfNewer"`، `"Skip"` یا `"Overwrite"` |
| `overwriteExistingBackup` | بولی | `true` | در صورت اتصال مجدد درایو، آیا پوشه قبلی بروز شود یا پوشه جدید با شماره افزایشی مانند `(2)` ساخته شود |
| `backupFolderTemplate` | رشته | `"{VolumeLabel}_{DriveLetter}_{Date}_{Time}"` | الگوی نام‌گذاری پوشه با متغیرهای درایو، نام فلش، تاریخ و ساعت |
| `copyEmptyDirectories` | بولی | `true` | ایجاد ساختار پوشه‌های خالی در مقصد |

---

## ۵. فیلترهای پیشرفته (Filters)

```json
"filters": {
  "includeExtensions": [".pdf", ".docx"],
  "excludeExtensions": [".tmp", ".part"],
  "includeCategories": ["Documents", "Images"],
  "excludeCategories": ["Executables"],
  "includeFolders": [],
  "excludeFolders": [
    "$RECYCLE.BIN",
    "System Volume Information"
  ],
  "minSizeMB": 0.1,
  "maxSizeMB": 2048.0,
  "modifiedAfter": "2024-01-01T00:00:00",
  "modifiedBefore": null,
  "createdAfter": null,
  "createdBefore": null
}
```

- **دسته‌بندی‌ها (Categories):**
  - `Video`: پسوندهای ویدیویی (`.mp4`, `.mkv`, ...)
  - `Audio`: فایل‌های صوتی (`.mp3`, `.wav`, ...)
  - `Images`: تصاویر (`.jpg`, `.png`, `.svg`, ...)
  - `Documents`: اسناد (`.pdf`, `.docx`, `.xlsx`, ...)
  - `Archives`: آرشیوها (`.zip`, `.rar`, `.7z`, ...)
  - `Executables`: فایل‌های اجرایی (`.exe`, `.bat`, ...)
