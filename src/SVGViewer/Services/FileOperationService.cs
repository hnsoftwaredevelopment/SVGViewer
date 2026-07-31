using System.IO;
using Microsoft.VisualBasic.FileIO;

namespace SVGViewer.Services;

/// <summary>Result of a file operation, so the UI can react and report.</summary>
public enum FileOperationOutcome
{
    Success,
    FileNotFound,
    TargetExists,
    InvalidName,
    Failed
}

public interface IFileOperationService
{
    /// <summary>Moves a file to the Windows Recycle Bin (reversible).</summary>
    FileOperationOutcome DeleteToRecycleBin(string path);

    /// <summary>
    /// Renames a file within its folder. Returns <see cref="FileOperationOutcome.TargetExists"/>
    /// when a different file already uses the new name and <paramref name="overwrite"/>
    /// is false, so the caller can ask first.
    /// </summary>
    FileOperationOutcome Rename(string path, string newName, bool overwrite);

    /// <summary>
    /// Creates a new sub-folder. Returns <see cref="FileOperationOutcome.TargetExists"/>
    /// when a file or folder with that name is already present.
    /// </summary>
    FileOperationOutcome CreateFolder(string parentPath, string name);

    /// <summary>
    /// Copies a file into a target folder. Pasting into the file's own folder makes
    /// a uniquely-named duplicate. Returns <see cref="FileOperationOutcome.TargetExists"/>
    /// when a different folder already holds a file of that name and
    /// <paramref name="overwrite"/> is false, so the caller can ask first.
    /// </summary>
    FileOperationOutcome Copy(string sourcePath, string targetDirectory, bool overwrite);
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

    public FileOperationOutcome Rename(string path, string newName, bool overwrite)
    {
        try
        {
            if (!File.Exists(path))
            {
                return FileOperationOutcome.FileNotFound;
            }

            if (string.IsNullOrWhiteSpace(newName) ||
                newName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                return FileOperationOutcome.InvalidName;
            }

            var directory = Path.GetDirectoryName(path);
            if (directory is null)
            {
                return FileOperationOutcome.Failed;
            }

            var target = Path.Combine(directory, newName);

            // Same file, different casing (e.g. "logo.svg" -> "Logo.svg") is a valid
            // rename, not a conflict.
            var sameFile = string.Equals(target, path, StringComparison.OrdinalIgnoreCase);

            if (!overwrite && !sameFile && File.Exists(target))
            {
                return FileOperationOutcome.TargetExists;
            }

            File.Move(path, target, overwrite && !sameFile);
            return FileOperationOutcome.Success;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to rename '{path}' to '{newName}'.", ex);
            return FileOperationOutcome.Failed;
        }
    }

    public FileOperationOutcome CreateFolder(string parentPath, string name)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name) ||
                name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                return FileOperationOutcome.InvalidName;
            }

            var target = Path.Combine(parentPath, name);
            if (Directory.Exists(target) || File.Exists(target))
            {
                return FileOperationOutcome.TargetExists;
            }

            Directory.CreateDirectory(target);
            return FileOperationOutcome.Success;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to create folder '{name}' in '{parentPath}'.", ex);
            return FileOperationOutcome.Failed;
        }
    }

    public FileOperationOutcome Copy(string sourcePath, string targetDirectory, bool overwrite)
    {
        try
        {
            if (!File.Exists(sourcePath))
            {
                return FileOperationOutcome.FileNotFound;
            }

            if (!Directory.Exists(targetDirectory))
            {
                return FileOperationOutcome.Failed;
            }

            var fileName = Path.GetFileName(sourcePath);
            var sourceDir = Path.GetDirectoryName(sourcePath) ?? string.Empty;
            var sameFolder = string.Equals(
                Path.GetFullPath(sourceDir),
                Path.GetFullPath(targetDirectory),
                StringComparison.OrdinalIgnoreCase);

            string target;
            if (sameFolder)
            {
                // Pasting into its own folder is an intentional duplicate: give it a
                // fresh "(n)" name rather than asking to overwrite the original.
                target = UniqueTarget(targetDirectory, fileName);
            }
            else
            {
                target = Path.Combine(targetDirectory, fileName);
                if (!overwrite && File.Exists(target))
                {
                    return FileOperationOutcome.TargetExists;
                }
            }

            File.Copy(sourcePath, target, overwrite && !sameFolder);
            return FileOperationOutcome.Success;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to copy '{sourcePath}' to '{targetDirectory}'.", ex);
            return FileOperationOutcome.Failed;
        }
    }

    private static string UniqueTarget(string directory, string fileName)
    {
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);

        var candidate = Path.Combine(directory, fileName);
        var counter = 2;
        while (File.Exists(candidate))
        {
            candidate = Path.Combine(directory, $"{baseName} ({counter}){extension}");
            counter++;
        }

        return candidate;
    }
}
