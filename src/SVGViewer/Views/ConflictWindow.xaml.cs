using System.Windows;
using System.Windows.Media.Imaging;
using SVGViewer.Localization;
using SVGViewer.Services;

namespace SVGViewer.Views;

/// <summary>Asks how to resolve a name conflict: overwrite / overwrite all / skip / skip all.</summary>
public partial class ConflictWindow : Window
{
    public ConflictWindow(string fileName)
    {
        InitializeComponent();
        MessageText.Text = Loc.Format("ConflictMessage", fileName);
        TryLoadIcon();
    }

    /// <summary>The chosen action. Defaults to Skip (safe) when the window is closed.</summary>
    public ConflictChoice Choice { get; private set; } = ConflictChoice.Skip;

    private void Overwrite_Click(object sender, RoutedEventArgs e) => Close(ConflictChoice.Overwrite);

    private void OverwriteAll_Click(object sender, RoutedEventArgs e) => Close(ConflictChoice.OverwriteAll);

    private void Skip_Click(object sender, RoutedEventArgs e) => Close(ConflictChoice.Skip);

    private void SkipAll_Click(object sender, RoutedEventArgs e) => Close(ConflictChoice.SkipAll);

    private void Close(ConflictChoice choice)
    {
        Choice = choice;
        DialogResult = true;
        base.Close();
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
