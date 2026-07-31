using System.IO;
using Microsoft.VisualBasic.FileIO;

namespace SVGViewer.Services;

/// <summary>Result of a file operation, so the UI can react and report.</summary>
public enum FileOperationOutcome
{
    Success,
    FileNotFound,
    Failed
}

public interface IFileOperationService
{
    /// <summary>Moves a file to the Windows Recycle Bin (reversible).</summary>
    FileOperationOutcome DeleteToRecycleBin(string path);
}

/// <summary>
/// File-management operations for the viewer. Deletes go to the Recycle Bin so
/// they stay reversible; failures are logged and reported via the return value
/// rather than throwing.
/// </summary>
public sealed class FileOperationService : IFileOperationService
{
    public FileOperationOutcome DeleteToRecycleBin(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return FileOperationOutcome.FileNotFound;
            }

            // OnlyErrorDialogs = no confirmation prompt from Windows (we ask our
            // own, localized question); SendToRecycleBin keeps it reversible.
            FileSystem.DeleteFile(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
            return FileOperationOutcome.Success;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to delete '{path}'.", ex);
            return FileOperationOutcome.Failed;
        }
    }
}
