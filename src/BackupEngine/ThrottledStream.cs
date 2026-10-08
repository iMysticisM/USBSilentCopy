using System.Diagnostics;

namespace USBSilentCopy.BackupEngine;

/// <summary>
/// استریم کنترل‌کننده سرعت انتقال داده (Bandwidth Throttler) بدون مصرف غیرضروری پردازنده
/// </summary>
public class ThrottledStream : Stream
{
    private readonly Stream _baseStream;
    private readonly long _maxBytesPerSecond;
    private readonly Stopwatch _stopwatch;
    private long _totalBytesTransferred;

    public ThrottledStream(Stream baseStream, int maxMBps)
    {
        _baseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
        _maxBytesPerSecond = maxMBps > 0 ? (long)maxMBps * 1024 * 1024 : 0;
        _stopwatch = Stopwatch.StartNew();
    }

    public override bool CanRead => _baseStream.CanRead;
    public override bool CanSeek => _baseStream.CanSeek;
    public override bool CanWrite => _baseStream.CanWrite;
    public override long Length => _baseStream.Length;
    public override long Position
    {
        get => _baseStream.Position;
        set => _baseStream.Position = value;
    }

    public override void Flush() => _baseStream.Flush();

    public override int Read(byte[] buffer, int offset, int count)
    {
        Throttle(count);
        var bytesRead = _baseStream.Read(buffer, offset, count);
        _totalBytesTransferred += bytesRead;
        return bytesRead;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await ThrottleAsync(count, cancellationToken);
        var bytesRead = await _baseStream.ReadAsync(buffer, offset, count, cancellationToken);
        _totalBytesTransferred += bytesRead;
        return bytesRead;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        Throttle(count);
        _baseStream.Write(buffer, offset, count);
        _totalBytesTransferred += count;
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await ThrottleAsync(count, cancellationToken);
        await _baseStream.WriteAsync(buffer, offset, count, cancellationToken);
        _totalBytesTransferred += count;
    }

    public override long Seek(long offset, SeekOrigin origin) => _baseStream.Seek(offset, origin);
    public override void SetLength(long value) => _baseStream.SetLength(value);

    private void Throttle(int byteCount)
    {
        if (_maxBytesPerSecond <= 0)
            return;

        var elapsedSeconds = _stopwatch.Elapsed.TotalSeconds;
        if (elapsedSeconds <= 0.001)
            return;

        var expectedSeconds = (double)(_totalBytesTransferred + byteCount) / _maxBytesPerSecond;
        var diff = expectedSeconds - elapsedSeconds;

        if (diff > 0.005)
        {
            var sleepMs = (int)(diff * 1000);
            if (sleepMs > 0)
            {
                Thread.Sleep(Math.Min(sleepMs, 500));
            }
        }
    }

    private async Task ThrottleAsync(int byteCount, CancellationToken cancellationToken)
    {
        if (_maxBytesPerSecond <= 0)
            return;

        var elapsedSeconds = _stopwatch.Elapsed.TotalSeconds;
        if (elapsedSeconds <= 0.001)
            return;

        var expectedSeconds = (double)(_totalBytesTransferred + byteCount) / _maxBytesPerSecond;
        var diff = expectedSeconds - elapsedSeconds;

        if (diff > 0.005)
        {
            var sleepMs = (int)(diff * 1000);
            if (sleepMs > 0)
            {
                await Task.Delay(Math.Min(sleepMs, 500), cancellationToken);
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _baseStream.Dispose();
        }
        base.Dispose(disposing);
    }
}
