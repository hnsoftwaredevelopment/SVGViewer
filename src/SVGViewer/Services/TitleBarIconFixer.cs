using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using WpfColor = System.Windows.Media.Color;

namespace SVGViewer.Services;

/// <summary>
/// Gives the window's <em>small</em> icon (the one Windows draws in the title
/// bar) a solid background, so the logo stays visible on dark, theme-coloured
/// title bars. The <em>large</em> icon (taskbar / Alt-Tab) is left untouched and
/// keeps the transparent artwork from <see cref="Window.Icon"/>.
/// </summary>
/// <remarks>
/// The title bar and taskbar normally share one icon; the only way to give them
/// different backgrounds is to override just the small icon via WM_SETICON.
/// </remarks>
public static class TitleBarIconFixer
{
    private const int WmSetIcon = 0x0080;
    private const int IconSmall = 0;

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    /// <summary>
    /// Composites the app icon onto <paramref name="background"/> and sets it as
    /// the window's small (title-bar) icon. Returns the created native icon handle
    /// so the caller can destroy it when the window closes; returns
    /// <see cref="IntPtr.Zero"/> on failure (in which case the default icon stays).
    /// </summary>
    public static IntPtr ApplySmallIcon(Window window, WpfColor background,
        string iconResourceUri = "/Assets/appicon.ico")
    {
        try
        {
            var info = Application.GetResourceStream(new Uri(iconResourceUri, UriKind.Relative));
            if (info?.Stream is null)
            {
                return IntPtr.Zero;
            }

            using var stream = info.Stream;

            // Decode a crisp square frame; 32 px keeps the title bar sharp on high DPI.
            using var appIcon = new Icon(stream, new System.Drawing.Size(32, 32));
            using var logo = appIcon.ToBitmap();

            const int size = 32;
            const int inset = 3;

            using var canvas = new Bitmap(size, size);
            using (var g = Graphics.FromImage(canvas))
            {
                g.Clear(Color.FromArgb(background.A, background.R, background.G, background.B));
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(logo, inset, inset, size - (inset * 2), size - (inset * 2));
            }

            var hIcon = canvas.GetHicon();
            var hwnd = new WindowInteropHelper(window).Handle;
            SendMessage(hwnd, WmSetIcon, IconSmall, hIcon);
            return hIcon;
        }
        catch
        {
            return IntPtr.Zero;
        }
    }

    /// <summary>Releases a handle returned by <see cref="ApplySmallIcon"/>.</summary>
    public static void Destroy(IntPtr hIcon)
    {
        if (hIcon != IntPtr.Zero)
        {
            DestroyIcon(hIcon);
        }
    }
}
