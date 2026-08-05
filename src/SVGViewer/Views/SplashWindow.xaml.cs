using System.Reflection;
using System.Windows;

namespace SVGViewer.Views;

/// <summary>
/// Startup splash: shows the splash image with the app version overlaid, then the
/// App closes it once the main window is up.
/// </summary>
public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = version is null
            ? string.Empty
            : $"v{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
    }
}
