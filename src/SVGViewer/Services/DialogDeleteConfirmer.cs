using System.Windows;
using SVGViewer.Views;

namespace SVGViewer.Services;

/// <summary>Shows the localized delete confirmation window (WPF implementation).</summary>
public sealed class DialogDeleteConfirmer : IDeleteConfirmer
{
    public DeleteConfirmation Confirm(string fileName)
    {
        var window = new ConfirmDeleteWindow(fileName)
        {
            Owner = Application.Current?.MainWindow
        };

        var confirmed = window.ShowDialog() == true;
        return new DeleteConfirmation(confirmed, confirmed && window.DoNotAskAgain);
    }
}
