namespace SVGViewer.Services;

/// <summary>Outcome of a delete confirmation prompt.</summary>
/// <param name="Confirmed">True when the user chose to delete.</param>
/// <param name="DoNotAskAgain">True when the user ticked "don't ask again".</param>
public readonly record struct DeleteConfirmation(bool Confirmed, bool DoNotAskAgain);

/// <summary>
/// Asks the user to confirm deleting a file. Abstracted so the view model stays
/// testable and the actual dialog lives in the UI layer.
/// </summary>
public interface IDeleteConfirmer
{
    DeleteConfirmation Confirm(string fileName);
}
