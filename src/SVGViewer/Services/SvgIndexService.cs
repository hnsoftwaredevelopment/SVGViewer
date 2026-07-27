using System.IO;

namespace SVGViewer.Services;

/// <summary>
/// Result of a drive scan: which folders contain SVG files, and which folders
/// must stay visible in the tree to reach them.
/// </summary>
public sealed class SvgFolderIndex
{
    /// <summary>Folders that directly contain one or more SVG files.</summary>
    public HashSet<string> FoldersWithSvg { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Folders that must be shown in a filtered tree: the folders with SVG files
    /// plus all of their parent folders up to the drive root.
    /// </summary>
    public HashSet<string> RelevantFolders { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Number of SVG files per folder, used for the badge in the tree.</summary>
    public Dictionary<string, int> SvgCountPerFolder { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public int TotalFoldersScanned { get; internal set; }

    public bool WasCancelled { get; internal set; }

    public bool ContainsSvg(string path) => FoldersWithSvg.Contains(path.TrimEnd('\\'));

    public bool IsRelevant(string path) => RelevantFolders.Contains(path.TrimEnd('\\'));

    public int GetSvgCount(string path) =>
        SvgCountPerFolder.TryGetValue(path.TrimEnd('\\'), out var count) ? count : 0;
}

/// <summary>
/// Walks an entire drive on a background thread to find every folder containing
/// SVG files. Iterative (no recursion) so a deep tree cannot overflow the stack.
/// </summary>
public sealed class SvgIndexService
{
    /// <summary>How often progress is reported, in folders.</summary>
    private const int ProgressInterval = 50;

    public Task<SvgFolderIndex> BuildIndexAsync(
        string rootPath,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() => BuildIndex(rootPath, progress, cancellationToken), cancellationToken);
    }

    private static SvgFolderIndex BuildIndex(
        string rootPath,
        IProgress<ScanProgress>? progress,
        CancellationToken cancellationToken)
    {
        var index = new SvgFolderIndex();
        var pending = new Stack<string>();
        pending.Push(rootPath.TrimEnd('\\'));

        var scanned = 0;

        while (pending.Count > 0)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                index.WasCancelled = true;
                break;
            }

            var current = pending.Pop();
            scanned++;

            var svgCount = DirectoryScanner.CountSvgFiles(current);
            if (svgCount > 0)
            {
                index.FoldersWithSvg.Add(current);
                index.SvgCountPerFolder[current] = svgCount;
                MarkAncestorsRelevant(index, current, rootPath);
            }

            foreach (var child in DirectoryScanner.GetSubDirectories(current))
            {
                pending.Push(child.FullName.TrimEnd('\\'));
            }

            if (scanned % ProgressInterval == 0)
            {
                progress?.Report(new ScanProgress(scanned, index.FoldersWithSvg.Count, current));
            }
        }

        index.TotalFoldersScanned = scanned;
        progress?.Report(new ScanProgress(scanned, index.FoldersWithSvg.Count, rootPath));
        return index;
    }

    /// <summary>
    /// Adds the folder and every parent up to the drive root to the relevant set,
    /// so a filtered tree can still show the path leading to the SVG folder.
    /// </summary>
    private static void MarkAncestorsRelevant(SvgFolderIndex index, string folder, string rootPath)
    {
        var root = rootPath.TrimEnd('\\');
        var current = folder;

        while (!string.IsNullOrEmpty(current))
        {
            if (!index.RelevantFolders.Add(current))
            {
                // Already processed this branch, so its ancestors are known too.
                break;
            }

            if (string.Equals(current, root, StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            current = Path.GetDirectoryName(current)?.TrimEnd('\\');
        }
    }
}
