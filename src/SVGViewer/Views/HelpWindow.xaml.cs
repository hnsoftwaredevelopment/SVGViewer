using System.Windows;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using SVGViewer.Localization;

namespace SVGViewer.Views;

/// <summary>
/// A small in-app window that shows the quick reference rendered from Markdown
/// (no browser). Its close button, Esc, and title-bar X dismiss only this window.
/// </summary>
public partial class HelpWindow : Window
{
    public HelpWindow(FlowDocument document)
    {
        InitializeComponent();
        Title = $"SVG Viewer — {Loc.Get("ButtonHelp")}";
        TryLoadIcon();
        Viewer.Document = document;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

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
