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

    // Drag & drop: where a possible drag began, and which file it was.
    private Point _dragStartPoint;
    private SvgFileViewModel? _dragCandidate;

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

    /// <summary>Shows the quick reference for the active language in an in-app window.</summary>
    private void Help_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var markdown = new HelpService().ReadQuickReference(_settings.Language);
            var document = MarkdownToFlowDocument.Convert(markdown);
            new HelpWindow(document) { Owner = this }.Show();
        }
        catch (Exception ex)
        {
            Logger.Warn("Could not open the quick reference.", ex);
            MessageBox.Show(this, ex.Message, "SVG Viewer",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    /// <summary>Opens the modal "About" dialog (logos, version, description).</summary>
    private void About_Click(object sender, RoutedEventArgs e) =>
        new AboutWindow { Owner = this }.ShowDialog();

    /// <summary>
    /// Updates the view model with the current preview selection (icon or list view),
    /// so file commands and drags act on all selected files.
    /// </summary>
    private void Files_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel &&
            sender is System.Windows.Controls.ListBox list)
        {
            viewModel.SetSelectedFiles(list.SelectedItems);
        }
    }

    /// <summary>Starts a move/copy drag of the selected file(s) once the pointer moves far enough.</summary>
    private void Thumbnail_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragCandidate is null)
        {
            return;
        }

        var position = e.GetPosition(null);
        if (Math.Abs(position.X - _dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(position.Y - _dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        var files = DataContext is MainViewModel viewModel
            ? viewModel.PathsForDrag(_dragCandidate)
            : new[] { _dragCandidate.FullPath };
        _dragCandidate = null;

        var data = new DataObject(DataFormats.FileDrop, files);
        DragDrop.DoDragDrop((DependencyObject)sender, data, DragDropEffects.Move | DragDropEffects.Copy);
    }

    /// <summary>Shows the move/copy cursor when a file is dragged over a folder node.</summary>
    private void Folder_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = CanDropInto(sender, e) ? EffectFor(e) : DragDropEffects.None;
        e.Handled = true;
    }

    /// <summary>Drops the dragged file(s) into the folder node (move, or copy with Ctrl).</summary>
    private void Folder_Drop(object sender, DragEventArgs e)
    {
        e.Handled = true;
        if (!CanDropInto(sender, e) || DataContext is not MainViewModel viewModel)
        {
            return;
        }

        var target = (DirectoryNodeViewModel)((FrameworkElement)sender).DataContext;
        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        viewModel.DropFiles(files, target, copy: EffectFor(e) == DragDropEffects.Copy);
    }

    private static bool CanDropInto(object sender, DragEventArgs e) =>
        e.Data.GetDataPresent(DataFormats.FileDrop) &&
        (sender as FrameworkElement)?.DataContext is DirectoryNodeViewModel { IsPlaceholder: false } node &&
        !string.IsNullOrEmpty(node.FullPath);

    private static DragDropEffects EffectFor(DragEventArgs e) =>
        (e.KeyStates & DragDropKeyStates.ControlKey) == DragDropKeyStates.ControlKey
            ? DragDropEffects.Copy
            : DragDropEffects.Move;

    /// <summary>Double-clicking a detail-list row opens the zoom preview.</summary>
    private void ListItem_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: SvgFileViewModel file })
        {
            OpenPreview(file);
        }
    }

    /// <summary>
    /// Records a possible drag start for a detail-list row. Opening the preview is
    /// handled separately by <see cref="ListItem_DoubleClick"/>, so here we only
    /// remember the pressed file; the drag itself begins in Thumbnail_MouseMove.
    /// </summary>
    private void ListItem_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(null);
        _dragCandidate = (sender as FrameworkElement)?.DataContext as SvgFileViewModel;
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
