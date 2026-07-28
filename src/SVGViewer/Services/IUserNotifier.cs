using System.Windows;

namespace SVGViewer.Services;

/// <summary>
/// Shows short notifications to the user. Abstracted so view models can be
/// tested without popping real dialogs.
/// </summary>
public interface IUserNotifier
{
    void Notify(string message, string title);
}

/// <summary>Default notifier backed by a WPF message box.</summary>
public sealed class MessageBoxNotifier : IUserNotifier
{
    public void Notify(string message, string title) =>
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
}
