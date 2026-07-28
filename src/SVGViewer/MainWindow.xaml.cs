using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using SVGViewer.ViewModels;

namespace SVGViewer;

public partial class MainWindow : Window
{
    // Distinguishes a single click (open the zoom preview) from a double click
    // (open in the editor) on the same thumbnail.
    private readonly DispatcherTimer _singleClickTimer;
    private SvgFileViewModel? _pendingClickFile;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        _singleClickTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(GetDoubleClickTime())
        };
        _singleClickTimer.Tick += OnSingleClickElapsed;
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

    /// <summary>
    /// One click on a thumbnail opens the zoom preview; a double click opens the
    /// editor. The single-click action is deferred by the system double-click
    /// time so a double click does not first flash the preview.
    /// </summary>
    private void Thumbnail_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: SvgFileViewModel file })
        {
            return;
        }

        if (e.ClickCount >= 2)
        {
            _singleClickTimer.Stop();
            _pendingClickFile = null;
            OpenInEditor(file);
            e.Handled = true;
            return;
        }

        _pendingClickFile = file;
        _singleClickTimer.Stop();
        _singleClickTimer.Start();
    }

    private void OnSingleClickElapsed(object? sender, EventArgs e)
    {
        _singleClickTimer.Stop();

        var file = _pendingClickFile;
        _pendingClickFile = null;

        if (file?.Thumbnail is not null)
        {
            ZoomViewer.Show(file.Thumbnail, file.FileName);
        }
    }

    /// <summary>Double-clicking a detail-list row opens the file in the editor.</summary>
    private void ListItem_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: SvgFileViewModel file })
        {
            OpenInEditor(file);
        }
    }

    private void OpenInEditor(SvgFileViewModel file)
    {
        if (DataContext is MainViewModel viewModel &&
            viewModel.OpenFileCommand.CanExecute(file))
        {
            viewModel.OpenFileCommand.Execute(file);
        }
    }

    /// <summary>The user's configured double-click time, in milliseconds.</summary>
    [DllImport("user32.dll")]
    private static extern uint GetDoubleClickTime();
}
