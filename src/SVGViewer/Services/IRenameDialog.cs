namespace SVGViewer.Services;

/// <summary>
/// Asks the user for a new file name. Returns the new full file name (with
/// extension) or null when the user cancels. Abstracted for testability.
/// </summary>
public interface IRenameDialog
{
    string? AskNewName(string currentFileName);
}
