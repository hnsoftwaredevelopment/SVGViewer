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

    /// <summary>Opens the zoom viewer when a thumbnail is clicked (if rendered).</summary>
    private void Thumbnail_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: SvgFileViewModel file } &&
            file.Thumbnail is not null)
        {
            ZoomViewer.Show(file.Thumbnail, file.FileName);
        }
    }
}
