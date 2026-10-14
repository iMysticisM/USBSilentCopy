<#
.SYNOPSIS
    اسکریپت بیلد و انتشار خودکار USBSilentCopy
#>

Write-Host "=== شروع بیلد پروژه USBSilentCopy ===" -ForegroundColor Cyan

# ۱. اجرای تست‌ها
Write-Host "اجرای آزمون‌های خودکار..." -ForegroundColor Yellow
dotnet test tests/USBSilentCopy.Tests.csproj -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "خطا در تست‌ها! عملیات متوقف شد." -ForegroundColor Red
    exit $LASTEXITCODE
}

# ۲. ساخت نسخه Release قابل حمل و کاملاً مستقل (بدون نیاز به پیش‌نیاز)
Write-Host "ساخت نسخه پرتابل مستقل (Self-Contained Single File)..." -ForegroundColor Yellow
dotnet publish src/USBSilentCopy.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o publish

# ۳. کپی فایل‌های پیکربندی پیش‌فرض و ایجاد فایل فشرده ریلیز
Copy-Item "config/config.json" "publish/config.json" -Force
Compress-Archive -Path "publish/*" -DestinationPath "USBSilentCopy-v1.0.0-win-x64.zip" -Force

Write-Host "=== بیلد با موفقیت انجام شد! فایل اجرایی در پوشه publish قرار دارد ===" -ForegroundColor Green
