using System.Windows;
using SVGViewer.ViewModels;

namespace SVGViewer.Views;

/// <summary>
/// Modal settings dialog. Changes are applied and persisted immediately by the
/// <see cref="SettingsViewModel"/>, so the dialog only needs a Close button.
/// </summary>
public partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
