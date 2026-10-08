using USBSilentCopy.Core;

namespace USBSilentCopy.UI;

/// <summary>
/// کانتکست اجرای کاملاً نامرئی در حالت Silent Mode (بدون پنجره، بدون آیکون سینی و بدون اعلان)
/// </summary>
public sealed class HeadlessContext : ApplicationContext
{
    private readonly AppCoordinator _coordinator;

    public HeadlessContext(AppCoordinator coordinator)
    {
        _coordinator = coordinator;
        _coordinator.Start();
    }

    protected override void ExitThreadCore()
    {
        _coordinator.Stop();
        base.ExitThreadCore();
    }
}
