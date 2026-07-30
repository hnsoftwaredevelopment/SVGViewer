using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SVGViewer.Views;

/// <summary>
/// A standalone popup window that shows one SVG in the zoom viewer. Being its own
/// window, its title-bar close button (and Esc, and the viewer's Close button)
/// dismiss only the preview — never the whole application.
/// </summary>
public partial class SvgZoomWindow : Window
{
    public SvgZoomWindow(ImageSource image, string fileName)
    {
        InitializeComponent();
        Title = $"SVG Viewer — {fileName}";
        TryLoadIcon();

        // Load once the window has its final size, so "fit" is accurate.
        Loaded += (_, _) => Viewer.Show(image, fileName);
    }

    private void TryLoadIcon()
    {
        try
        {
            Icon = BitmapFrame.Create(new Uri("pack://application:,,,/Assets/appicon.ico"));
        }
        catch
        {
            // No embedded icon; the default window icon is fine.
        }
    }
}
