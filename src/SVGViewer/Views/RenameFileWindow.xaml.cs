using System.Windows;
using System.Windows.Media.Imaging;
using SVGViewer.Localization;

namespace SVGViewer.Views;

/// <summary>
/// Prompts for a new file name. The extension is shown as a fixed suffix and
/// preserved, so the result is always a valid file name of the same type.
/// </summary>
public partial class RenameFileWindow : Window
{
    private readonly string _extension;

    public RenameFileWindow(string currentFileName)
    {
        InitializeComponent();

        _extension = System.IO.Path.GetExtension(currentFileName);
        ExtensionText.Text = _extension;
        NameBox.Text = System.IO.Path.GetFileNameWithoutExtension(currentFileName);

        TryLoadIcon();
        Loaded += (_, _) =>
        {
            NameBox.Focus();
            NameBox.SelectAll();
        };
    }

    /// <summary>The chosen new file name (with extension), set when confirmed.</summary>
    public string? NewName { get; private set; }

    private void Rename_Click(object sender, RoutedEventArgs e)
    {
        var entered = NameBox.Text?.Trim() ?? string.Empty;
        if (entered.Length == 0)
        {
            return; // keep the dialog open; nothing to do with an empty name
        }

        NewName = entered.EndsWith(_extension, StringComparison.OrdinalIgnoreCase)
            ? entered
            : entered + _extension;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

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
