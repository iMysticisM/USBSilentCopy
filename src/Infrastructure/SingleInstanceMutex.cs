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
