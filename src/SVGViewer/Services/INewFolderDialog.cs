namespace SVGViewer.Services;

/// <summary>
/// Asks the user for a new folder name. Returns the name, or null when the user
/// cancels. Abstracted for testability.
/// </summary>
public interface INewFolderDialog
{
    string? AskFolderName();
}
