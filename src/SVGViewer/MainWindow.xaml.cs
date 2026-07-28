using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SVGViewer.Models;
using SVGViewer.Services;
using SVGViewer.ViewModels;
using SVGViewer.Views;

namespace SVGViewer;

public partial class MainWindow : Window
{
    private readonly SettingsService _settingsService;
    private readonly AppSettings _settings;

    public MainWindow(MainViewModel viewModel, SettingsService settingsService, AppSettings settings)
    {
        InitializeComponent();
        DataContext = viewModel;
        _settingsService = settingsService;
        _settings = settings;
    }

    /// <summary>
    /// TreeView.SelectedItem is read-only, so the selection is forwarded to the
    /// view model here. This is the only selection-related code-behind.
    /// </summary>
    private void FolderTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.SelectedNode = e.NewValue as DirectoryNodeViewModel;
        }
    }

    /// <summary>Opens the modal Settings dialog (language, confirmations).</summary>
    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var viewModel = new SettingsViewModel(_settingsService, _settings);
        var window = new SettingsWindow(viewModel) { Owner = this };
        window.ShowDialog();
    }

    /// <summary>
    /// Double-clicking a thumbnail opens the zoom preview. A single click is left
    /// free for future file-management gestures (drag to move, select). Opening in
    /// the editor lives in the right-click menu.
    /// </summary>
    private void Thumbnail_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount < 2)
        {
            return;
        }

        if (sender is FrameworkElement { DataContext: SvgFileViewModel file })
        {
            OpenPreview(file);
            e.Handled = true;
        }
    }

    /// <summary>Double-clicking a detail-list row opens the zoom preview.</summary>
    private void ListItem_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: SvgFileViewModel file })
        {
            OpenPreview(file);
        }
    }

    /// <summary>
    /// Shows the SVG in the zoom viewer. In the detail view no thumbnail has been
    /// rendered yet, so it is rendered on demand first.
    /// </summary>
    private async void OpenPreview(SvgFileViewModel file)
    {
        if (file.Thumbnail is null && !file.RenderFailed)
        {
            await file.LoadThumbnailAsync();
        }

        if (file.Thumbnail is not null)
        {
            ZoomViewer.Show(file.Thumbnail, file.FileName);
        }
    }
}
