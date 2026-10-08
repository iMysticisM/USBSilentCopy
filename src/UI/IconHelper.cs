using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace USBSilentCopy.UI;

/// <summary>
/// تولید و دسترسی به آیکون اختصاصی برنامه به صورت پویا و پایدار
/// </summary>
public static class IconHelper
{
    private static Icon? _cachedIcon;

    public static Icon GetAppIcon()
    {
        if (_cachedIcon != null)
            return _cachedIcon;

        var assetPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "icon.ico");
        if (File.Exists(assetPath))
        {
            try
            {
                _cachedIcon = new Icon(assetPath);
                return _cachedIcon;
            }
            catch { }
        }

        // تولید داینامیک یک آیکون حرفه‌ای ۳۲x۳۲ در حافظه
        using var bitmap = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            // پس‌زمینه دایره‌ای با گرادیان سرمه‌ای-آبی
            using var brush = new LinearGradientBrush(new Rectangle(0, 0, 32, 32), Color.FromArgb(24, 76, 120), Color.FromArgb(12, 36, 64), 45f);
            g.FillEllipse(brush, 1, 1, 30, 30);

            // حاشیه نقره‌ای
            using var pen = new Pen(Color.FromArgb(180, 210, 240), 1.5f);
            g.DrawEllipse(pen, 1, 1, 30, 30);

            // نماد USB / درایو
            using var whiteBrush = new SolidBrush(Color.White);
            g.FillRectangle(whiteBrush, 12, 14, 8, 12);
            g.FillRectangle(whiteBrush, 14, 7, 4, 7);

            // پین‌های USB
            using var darkBrush = new SolidBrush(Color.FromArgb(24, 76, 120));
            g.FillRectangle(darkBrush, 13, 9, 2, 2);
            g.FillRectangle(darkBrush, 17, 9, 2, 2);
        }

        var hIcon = bitmap.GetHicon();
        _cachedIcon = (Icon)Icon.FromHandle(hIcon).Clone();
        DestroyIcon(hIcon);

        // ذخیره در پوشه assets در صورت امکان
        try
        {
            var dir = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            using var fs = new FileStream(assetPath, FileMode.Create, FileAccess.Write);
            _cachedIcon.Save(fs);
        }
        catch { }

        return _cachedIcon;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
