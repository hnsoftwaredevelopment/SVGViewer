namespace SVGViewer.Services;

/// <summary>What to do when a file being copied/moved already exists at the target.</summary>
public enum ConflictChoice
{
    Overwrite,
    OverwriteAll,
    Skip,
    SkipAll
}

/// <summary>
/// Asks the user how to resolve a name conflict during a multi-file copy/move.
/// The "all" choices apply to the rest of the running operation only.
/// </summary>
public interface IConflictResolver
{
    ConflictChoice Resolve(string fileName);
}
