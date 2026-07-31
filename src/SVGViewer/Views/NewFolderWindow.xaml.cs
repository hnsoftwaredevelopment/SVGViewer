using System.Windows;
using System.Windows.Media.Imaging;

namespace SVGViewer.Views;

/// <summary>Prompts for a new folder name.</summary>
public partial class NewFolderWindow : Window
{
    public NewFolderWindow()
    {
        InitializeComponent();
        TryLoadIcon();
        Loaded += (_, _) => NameBox.Focus();
    }

    /// <summary>The chosen folder name, set when confirmed.</summary>
    public string? FolderName { get; private set; }

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        var entered = NameBox.Text?.Trim() ?? string.Empty;
        if (entered.Length == 0)
        {
            return; // keep the dialog open; nothing to create
        }

        FolderName = entered;
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
