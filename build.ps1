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

# ۲. ساخت نسخه Release قابل حمل
Write-Host "ساخت نسخه پرتابل (Publish Single File)..." -ForegroundColor Yellow
dotnet publish src/USBSilentCopy.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish

# ۳. کپی فایل‌های پیکربندی پیش‌فرض
Copy-Item "config/config.json" "publish/config.json" -Force

Write-Host "=== بیلد با موفقیت انجام شد! فایل اجرایی در پوشه publish قرار دارد ===" -ForegroundColor Green
