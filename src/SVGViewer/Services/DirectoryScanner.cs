using System.IO;

namespace SVGViewer.Services;

/// <summary>Progress information reported while scanning a drive.</summary>
public readonly record struct ScanProgress(int FoldersScanned, int FoldersWithSvg, string CurrentFolder);

/// <summary>Directory data collected by one scan-specific file-system pass.</summary>
public readonly record struct DirectoryScanResult(int SvgFileCount, IReadOnlyList<string> SubDirectories);

/// <summary>
/// File-system helpers for locating SVG files. All members are defensive:
/// folders that cannot be read are skipped instead of throwing, because a full
/// drive always contains some protected locations.
/// </summary>
public static class DirectoryScanner
{
    public const string SvgSearchPattern = "*.svg";

    /// <summary>Folder names that are never useful to scan and cost a lot of time.</summary>
    private static readonly string[] SkippedFolderNames =
    {
        "$Recycle.Bin",
        "System Volume Information",
        "$WinREAgent",
        "Config.Msi"
    };

    /// <summary>
    /// Normalizes a folder path for comparison and enumeration.
    /// </summary>
    /// <remarks>
    /// Trailing separators are removed so the same folder always yields the same
    /// string, <b>except</b> for a drive root. On Windows "C:" is drive-relative
    /// (it means "the current directory on C:"), so the root must keep its
    /// separator as "C:\" — otherwise enumeration silently returns the contents
    /// of the process working directory instead of the drive root.
    /// </remarks>
    public static string NormalizeFolderPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var trimmed = path.TrimEnd('\\', '/');

        // "C:" or "Z:" -> restore the separator.
        if (trimmed.Length == 2 && trimmed[1] == ':' && char.IsLetter(trimmed[0]))
        {
            return trimmed + "\\";
        }

        // A path consisting only of separators (e.g. "\\") has nothing to trim to.
        return trimmed.Length == 0 ? path : trimmed;
    }

    /// <summary>Returns true when the folder directly contains at least one SVG file.</summary>
    public static bool HasSvgFiles(string path)
    {
        try
        {
            return Directory.EnumerateFiles(path, SvgSearchPattern).Any();
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>Counts the SVG files directly inside the folder; 0 when unreadable.</summary>
    public static int CountSvgFiles(string path)
    {
        try
        {
            return Directory.EnumerateFiles(path, SvgSearchPattern).Count();
        }
        catch (Exception)
        {
            return 0;
        }
    }

    /// <summary>Returns the full paths of the SVG files directly inside the folder.</summary>
    public static IReadOnlyList<string> GetSvgFiles(string path)
    {
        try
        {
            return Directory.EnumerateFiles(path, SvgSearchPattern)
                            .OrderBy(f => f, StringComparer.CurrentCultureIgnoreCase)
                            .ToList();
        }
        catch (Exception)
        {
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// Returns readable sub-directories, skipping hidden/system folders,
    /// reparse points (junctions and symlinks, which can cause cycles) and
    /// well-known noise folders.
    /// </summary>
    public static IReadOnlyList<DirectoryInfo> GetSubDirectories(string path)
    {
        try
        {
            return new DirectoryInfo(path)
                .EnumerateDirectories()
                .Where(IsScannable)
                .OrderBy(d => d.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }
        catch (Exception)
        {
            return Array.Empty<DirectoryInfo>();
        }
    }

    /// <summary>
    /// Scans one directory in a single, unsorted pass. This is intentionally
    /// separate from <see cref="GetSubDirectories"/>: the TreeView needs sorted
    /// folders, while a background scan does not and should avoid that cost.
    /// </summary>
    public static DirectoryScanResult ScanDirectory(string path, CancellationToken cancellationToken = default)
    {
        var svgCount = 0;
        var subDirectories = new List<string>();

        try
        {
            foreach (var entry in new DirectoryInfo(path).EnumerateFileSystemInfos())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (entry is DirectoryInfo directory)
                {
                    if (IsScannable(directory))
                    {
                        subDirectories.Add(NormalizeFolderPath(directory.FullName));
                    }
                }
                else if (entry is FileInfo &&
                         string.Equals(Path.GetExtension(entry.Name), ".svg", StringComparison.OrdinalIgnoreCase))
                {
                    svgCount++;
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            // The accessible entries found before an error are still useful; the
            // directory is otherwise treated as unreadable, as in the legacy path.
        }

        return new DirectoryScanResult(svgCount, subDirectories);
    }

    private static bool IsScannable(DirectoryInfo directory)
    {
        try
        {
            if (directory.Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                return false;
            }

            if (directory.Attributes.HasFlag(FileAttributes.System))
            {
                return false;
            }

            return !SkippedFolderNames.Contains(directory.Name, StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception)
        {
            return false;
        }
    }
}
