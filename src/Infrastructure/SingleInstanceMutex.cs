using USBSilentCopy.Core;

namespace USBSilentCopy.Infrastructure;

/// <summary>
/// اطمینان از تک‌نسخه‌ای بودن برنامه در سطح نشست کاربری ویندوز
/// </summary>
public sealed class SingleInstanceMutex : IDisposable
{
    private Mutex? _mutex;
    private bool _hasHandle;

    public bool TryAcquire(string? customMutexName = null)
    {
        var name = customMutexName ?? Constants.SingleInstanceMutexId;
        try
        {
            _mutex = new Mutex(true, name, out _hasHandle);
            return _hasHandle;
        }
        catch (AbandonedMutexException)
        {
            // در صورتی که نمونه قبلی به صورت ناگهانی بسته یا کرش کرده باشد، قفل با موفقیت به نمونه فعلی واگذار می‌شود
            _hasHandle = true;
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            // اگر به دلیل محدودیت‌های سیستم‌عامل خطای دسترسی رخ داد، با نام اختصاصی کاربر تلاش می‌کنیم
            try
            {
                var fallbackName = $"USBSilentCopy_Mutex_{Environment.UserName}";
                _mutex = new Mutex(true, fallbackName, out _hasHandle);
                return _hasHandle;
            }
            catch (AbandonedMutexException)
            {
                _hasHandle = true;
                return true;
            }
            catch
            {
                _hasHandle = false;
                return false;
            }
        }
        catch
        {
            _hasHandle = false;
            return false;
        }
    }

    public void Release()
    {
        if (_hasHandle && _mutex != null)
        {
            try
            {
                _mutex.ReleaseMutex();
            }
            catch { }
            _hasHandle = false;
        }
    }

    public void Dispose()
    {
        Release();
        _mutex?.Dispose();
        _mutex = null;
    }
}
