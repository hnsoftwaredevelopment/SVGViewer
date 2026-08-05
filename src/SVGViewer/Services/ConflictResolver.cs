using System.Windows;
using SVGViewer.Views;

namespace SVGViewer.Services;

/// <summary>Shows the localized conflict window (WPF implementation).</summary>
public sealed class ConflictResolver : IConflictResolver
{
    public ConflictChoice Resolve(string fileName)
    {
        var window = new ConflictWindow(fileName)
        {
            Owner = Application.Current?.MainWindow
        };

        window.ShowDialog();
        return window.Choice;
    }
}
