using System.Windows;

namespace SVGViewer.Services;

/// <summary>
/// Shows short notifications and yes/no confirmations to the user. Abstracted so
/// view models can be tested without popping real dialogs.
/// </summary>
public interface IUserNotifier
{
    void Notify(string message, string title);

    /// <summary>Asks a yes/no question; returns true when the user confirms.</summary>
    bool Confirm(string message, string title);
}

/// <summary>Default notifier backed by a WPF message box.</summary>
public sealed class MessageBoxNotifier : IUserNotifier
{
    public void Notify(string message, string title) =>
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);

    public bool Confirm(string message, string title) =>
        MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question)
            == MessageBoxResult.Yes;
}
