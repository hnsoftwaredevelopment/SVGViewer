using System.IO;

namespace SVGViewer.Services;

/// <summary>Progress information reported while scanning a drive.</summary>
public readonly record struct ScanProgress(int FoldersScanned, int FoldersWithSvg, string CurrentFolder);

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
