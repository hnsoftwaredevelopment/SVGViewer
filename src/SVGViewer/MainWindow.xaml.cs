using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SVGViewer.ViewModels;

namespace SVGViewer;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
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
