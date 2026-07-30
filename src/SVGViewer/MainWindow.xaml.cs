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
    private IntPtr _titleBarIcon;

    public MainWindow(MainViewModel viewModel, SettingsService settingsService, AppSettings settings)
    {
        InitializeComponent();
        DataContext = viewModel;
        _settingsService = settingsService;
        _settings = settings;
        TryLoadWindowIcon();
    }

    /// <summary>
    /// Sets the title-bar / taskbar icon from the embedded app icon resource when
    /// present. Loading from the embedded resource (rather than a file on disk)
    /// means it is always in sync with the build and never stale or locked. A
    /// missing or invalid icon must never prevent the window from opening.
    /// </summary>
    private void TryLoadWindowIcon()
    {
        try
        {
            Icon = System.Windows.Media.Imaging.BitmapFrame.Create(
                new Uri("pack://application:,,,/Assets/appicon.ico"));
        }
        catch
        {
            // No embedded icon (or load failed): fall back to the default icon.
        }
    }

    /// <summary>
    /// Once the window handle exists, give the title-bar (small) icon a white
    /// background so it stays visible on dark, theme-coloured title bars. The
    /// taskbar / Alt-Tab (large) icon keeps the transparent artwork.
    /// </summary>
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _titleBarIcon = TitleBarIconFixer.ApplySmallIcon(this, System.Windows.Media.Colors.White);
    }

    protected override void OnClosed(EventArgs e)
    {
        TitleBarIconFixer.Destroy(_titleBarIcon);
        base.OnClosed(e);
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

    /// <summary>Opens the user guide for the active language in the default browser.</summary>
    private void Help_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            new HelpService().OpenGuide(_settings.Language);
        }
        catch (Exception ex)
        {
            Logger.Warn("Could not open the user guide.", ex);
            MessageBox.Show(this, ex.Message, "SVG Viewer",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    /// <summary>Opens the modal "About" dialog (logos, version, description).</summary>
    private void About_Click(object sender, RoutedEventArgs e) =>
        new AboutWindow { Owner = this }.ShowDialog();

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
            // A standalone window: its close button dismisses only the preview.
            new SvgZoomWindow(file.Thumbnail, file.FileName) { Owner = this }.Show();
        }
    }
}
