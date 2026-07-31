using System.Windows;
using SVGViewer.Views;

namespace SVGViewer.Services;

/// <summary>Shows the localized new-folder window (WPF implementation).</summary>
public sealed class NewFolderDialog : INewFolderDialog
{
    public string? AskFolderName()
    {
        var window = new NewFolderWindow
        {
            Owner = Application.Current?.MainWindow
        };

        return window.ShowDialog() == true ? window.FolderName : null;
    }
}
