using USBSilentCopy.BackupEngine;
using USBSilentCopy.Configuration;
using USBSilentCopy.Core;
using USBSilentCopy.History;
using USBSilentCopy.Logging;
using USBSilentCopy.Profiles;

namespace USBSilentCopy.Queue;

public class QueueItem
{
    public UsbDeviceInfo Device { get; set; }
    public DateTime EnqueuedTime { get; set; } = DateTime.Now;
    public DeviceStatus Status { get; set; } = DeviceStatus.Queued;

    public QueueItem(UsbDeviceInfo device)
    {
        Device = device;
    }
}

/// <summary>
/// مدیریت صف پردازش وظایف پشتیبان‌گیری درایوهای USB بر اساس اولویت و همروندی مجاز
/// </summary>
public class BackupQueue : IDisposable
{
    private readonly ConfigurationManager _configManager;
    private readonly ProfileManager _profileManager;
    private readonly LogManager _logManager;
    private readonly HistoryManager _historyManager;

    private readonly List<QueueItem> _queue = new();
    private readonly HashSet<string> _processedInCurrentSession = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _currentlyProcessing = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    private readonly SemaphoreSlim _concurrencySemaphore;
    private readonly ManualResetEventSlim _pauseEvent = new(true);
    private CancellationTokenSource _cts = new();
    private bool _isPaused;

    public event Action<QueueItem>? ItemEnqueued;
    public event Action<QueueItem>? ItemStarted;
    public event Action<BackupProgressInfo>? ItemProgress;
    public event Action<QueueItem, BackupSessionResult>? ItemCompleted;
    public event Action<bool>? PauseStateChanged;
    public event Action? QueueStatusChanged;

    public bool IsPaused => _isPaused;
    public int PendingCount
    {
        get
        {
            lock (_lock)
            {
                return _queue.Count;
            }
        }
    }
    public int ActiveCount
    {
        get
        {
            lock (_lock)
            {
                return _currentlyProcessing.Count;
            }
        }
    }

    public BackupQueue(
        ConfigurationManager configManager,
        ProfileManager profileManager,
        LogManager logManager,
        HistoryManager historyManager)
    {
        _configManager = configManager;
        _profileManager = profileManager;
        _logManager = logManager;
        _historyManager = historyManager;

        var maxConcurrent = Math.Max(1, _configManager.Current.MaxConcurrentTasks);
        _concurrencySemaphore = new SemaphoreSlim(maxConcurrent, maxConcurrent);
    }

    public bool EnqueueDevice(UsbDeviceInfo device)
    {
        lock (_lock)
        {
            var key = device.ProfileIdentifier;

            // جلوگیری از پردازش تکراری درایو در یک سشن برنامه
            if (_processedInCurrentSession.Contains(key) || _currentlyProcessing.Contains(key))
            {
                _logManager.LogDevice(key, $"درایو {device.DisplayName} قبلاً در این سشن پردازش شده است؛ از کپی مجدد صرف‌نظر شد.");
                return false;
            }

            // بررسی موجود بودن در صف
            if (_queue.Any(q => q.Device.ProfileIdentifier.Equals(key, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            var item = new QueueItem(device);
            _queue.Add(item);

            // مرتب‌سازی صف بر اساس حرف درایو (Drive Letter)
            _queue.Sort((a, b) => string.Compare(a.Device.DriveLetter, b.Device.DriveLetter, StringComparison.OrdinalIgnoreCase));

            _logManager.LogDevice(key, $"درایو {device.DisplayName} در صف پردازش قرار گرفت. (تعداد در صف: {_queue.Count})");
            ItemEnqueued?.Invoke(item);
            QueueStatusChanged?.Invoke();
        }

        // بررسی و راه‌اندازی ورکر در صورت وجود ظرفیت خالی
        _ = ProcessQueueAsync();
        return true;
    }

    private async Task ProcessQueueAsync()
    {
        QueueItem? itemToProcess = null;

        lock (_lock)
        {
            if (_queue.Count == 0 || _cts.IsCancellationRequested)
                return;

            itemToProcess = _queue[0];
            _queue.RemoveAt(0);
            _currentlyProcessing.Add(itemToProcess.Device.ProfileIdentifier);
            itemToProcess.Status = DeviceStatus.InProgress;
            itemToProcess.Device.Status = DeviceStatus.InProgress;
        }

        ItemStarted?.Invoke(itemToProcess);
        QueueStatusChanged?.Invoke();

        await _concurrencySemaphore.WaitAsync(_cts.Token);

        _ = Task.Run(async () =>
        {
            BackupSessionResult result = new();
            try
            {
                var profile = _profileManager.GetOrCreateProfile(itemToProcess.Device);
                var worker = new BackupWorker(
                    itemToProcess.Device,
                    _configManager.Current,
                    profile,
                    _logManager,
                    _pauseEvent,
                    _cts.Token);

                worker.ProgressChanged += info => ItemProgress?.Invoke(info);

                result = await worker.ExecuteAsync();
            }
            catch (Exception ex)
            {
                result = new BackupSessionResult
                {
                    DeviceId = itemToProcess.Device.DeviceId,
                    SerialNumber = itemToProcess.Device.SerialNumber,
                    DriveLetter = itemToProcess.Device.DriveLetter,
                    VolumeLabel = itemToProcess.Device.VolumeLabel,
                    Status = BackupStatus.Failed,
                    EndTime = DateTime.Now
                };
                result.Errors.Add(ex.Message);
            }
            finally
            {
                _concurrencySemaphore.Release();

                lock (_lock)
                {
                    _currentlyProcessing.Remove(itemToProcess.Device.ProfileIdentifier);
                    if (result.Status == BackupStatus.Completed)
                    {
                        _processedInCurrentSession.Add(itemToProcess.Device.ProfileIdentifier);
                    }
                    itemToProcess.Status = result.Status == BackupStatus.Completed
                        ? DeviceStatus.Completed
                        : DeviceStatus.Failed;
                    itemToProcess.Device.Status = itemToProcess.Status;
                }

                _historyManager.RecordSession(result);
                ItemCompleted?.Invoke(itemToProcess, result);
                QueueStatusChanged?.Invoke();

                // فراخوانی پردازش آیتم بعدی در صف
                _ = ProcessQueueAsync();
            }
        });
    }

    public void TogglePause()
    {
        if (_isPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    public void Pause()
    {
        _isPaused = true;
        _pauseEvent.Reset();
        _logManager.LogApp("عملیات پشتیبان‌گیری متوقف موقت (Pause) شد.");
        PauseStateChanged?.Invoke(true);
    }

    public void Resume()
    {
        _isPaused = false;
        _pauseEvent.Set();
        _logManager.LogApp("عملیات پشتیبان‌گیری ادامه یافت (Resume).");
        PauseStateChanged?.Invoke(false);
    }

    public void CancelAll()
    {
        lock (_lock)
        {
            _queue.Clear();
        }

        _cts.Cancel();
        _cts.Dispose();
        _cts = new CancellationTokenSource();

        _logManager.LogApp("تمام وظایف صف پشتیبان‌گیری لغو شدند.");
        QueueStatusChanged?.Invoke();
    }

    public void MarkDeviceDisconnected(string driveLetter)
    {
        lock (_lock)
        {
            var cleanDrive = driveLetter.TrimEnd('\\', ':').ToUpperInvariant();
            var matched = _queue.FirstOrDefault(q => q.Device.DriveLetter.TrimEnd('\\', ':').ToUpperInvariant() == cleanDrive);
            if (matched != null)
            {
                _queue.Remove(matched);
                matched.Status = DeviceStatus.Disconnected;
                QueueStatusChanged?.Invoke();
            }
        }
    }

    public void Dispose()
    {
        _pauseEvent.Dispose();
        _concurrencySemaphore.Dispose();
        _cts.Dispose();
    }
}
