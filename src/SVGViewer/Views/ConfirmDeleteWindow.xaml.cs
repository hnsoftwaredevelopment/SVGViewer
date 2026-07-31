using System.Windows;
using System.Windows.Media.Imaging;
using SVGViewer.Localization;

namespace SVGViewer.Views;

/// <summary>
/// Localized "move this file to the Recycle Bin?" prompt with a "don't ask again"
/// option. Returned via <see cref="Window.DialogResult"/> and <see cref="DoNotAskAgain"/>.
/// </summary>
public partial class ConfirmDeleteWindow : Window
{
    public ConfirmDeleteWindow(string fileName)
    {
        InitializeComponent();
        MessageText.Text = Loc.Format("ConfirmDeleteMessage", fileName);
        TryLoadIcon();
    }

    public bool DoNotAskAgain => DoNotAskAgainCheck.IsChecked == true;

    private void Delete_Click(object sender, RoutedEventArgs e) => DialogResult = true;

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
