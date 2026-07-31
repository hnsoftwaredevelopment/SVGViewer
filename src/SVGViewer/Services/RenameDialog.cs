using System.Windows;
using SVGViewer.Views;

namespace SVGViewer.Services;

/// <summary>Shows the localized rename window (WPF implementation).</summary>
public sealed class RenameDialog : IRenameDialog
{
    public string? AskNewName(string currentFileName)
    {
        var window = new RenameFileWindow(currentFileName)
        {
            Owner = Application.Current?.MainWindow
        };

        return window.ShowDialog() == true ? window.NewName : null;
    }
}
