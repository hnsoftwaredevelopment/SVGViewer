using System.Collections.Concurrent;
using System.IO;

namespace SVGViewer.Services;

/// <summary>
/// Result of a drive scan: which folders contain SVG files, and which folders
/// must stay visible in the tree to reach them.
/// </summary>
/// <remarks>
/// Backed by concurrent dictionaries so the UI thread can read markings while
/// the background scan is still writing to them (progressive highlighting).
/// </remarks>
public sealed class SvgFolderIndex
{
    private readonly ConcurrentDictionary<string, int> _svgCounts =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, byte> _relevant =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Folders that directly contain one or more SVG files.</summary>
    public ICollection<string> FoldersWithSvg => _svgCounts.Keys;

    /// <summary>
    /// Folders that must be shown in a filtered tree: the folders with SVG files
    /// plus all of their parent folders up to the drive root.
    /// </summary>
    public ICollection<string> RelevantFolders => _relevant.Keys;

    public int TotalFoldersScanned { get; internal set; }

    public bool WasCancelled { get; internal set; }

    public bool ContainsSvg(string path) =>
        _svgCounts.ContainsKey(DirectoryScanner.NormalizeFolderPath(path));

    public bool IsRelevant(string path) =>
        _relevant.ContainsKey(DirectoryScanner.NormalizeFolderPath(path));

    public int GetSvgCount(string path) =>
        _svgCounts.TryGetValue(DirectoryScanner.NormalizeFolderPath(path), out var count) ? count : 0;

    /// <summary>Records a folder that directly contains SVG files.</summary>
    internal void AddSvgFolder(string normalizedPath, int count) =>
        _svgCounts[normalizedPath] = count;

    /// <summary>Marks a folder as on-the-path-to an SVG folder. Returns false if
    /// it was already marked (so the caller can stop walking up).</summary>
    internal bool AddRelevant(string normalizedPath) =>
        _relevant.TryAdd(normalizedPath, 0);
}

/// <summary>
/// Walks an entire drive on a background thread to find every folder containing
/// SVG files. Iterative (no recursion) so a deep tree cannot overflow the stack.
/// </summary>
public sealed class SvgIndexService
{
    /// <summary>How often progress is reported, in folders.</summary>
    private const int ProgressInterval = 50;

    /// <summary>Scans a drive into a fresh index.</summary>
    public Task<SvgFolderIndex> BuildIndexAsync(
        string rootPath,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return BuildIndexAsync(rootPath, new SvgFolderIndex(), progress, cancellationToken);
    }

    /// <summary>
    /// Scans a drive into the supplied index. Passing the index in advance lets
    /// the tree bind to it and light up folders progressively as the scan runs.
    /// </summary>
    public Task<SvgFolderIndex> BuildIndexAsync(
        string rootPath,
        SvgFolderIndex index,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() => BuildIndex(rootPath, index, progress, cancellationToken), cancellationToken);
    }

    private static SvgFolderIndex BuildIndex(
        string rootPath,
        SvgFolderIndex index,
        IProgress<ScanProgress>? progress,
        CancellationToken cancellationToken)
    {
        var pending = new Stack<string>();
        pending.Push(DirectoryScanner.NormalizeFolderPath(rootPath));

        var scanned = 0;
        var foundWithSvg = 0;

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
                index.AddSvgFolder(current, svgCount);
                foundWithSvg++;
                MarkAncestorsRelevant(index, current, rootPath);
            }

            foreach (var child in DirectoryScanner.GetSubDirectories(current))
            {
                pending.Push(DirectoryScanner.NormalizeFolderPath(child.FullName));
            }

            if (scanned % ProgressInterval == 0)
            {
                progress?.Report(new ScanProgress(scanned, foundWithSvg, current));
            }
        }

        index.TotalFoldersScanned = scanned;
        progress?.Report(new ScanProgress(scanned, foundWithSvg, rootPath));
        return index;
    }

    /// <summary>
    /// Adds the folder and every parent up to the drive root to the relevant set,
    /// so a filtered tree can still show the path leading to the SVG folder.
    /// </summary>
    private static void MarkAncestorsRelevant(SvgFolderIndex index, string folder, string rootPath)
    {
        var root = DirectoryScanner.NormalizeFolderPath(rootPath);
        var current = DirectoryScanner.NormalizeFolderPath(folder);

        while (!string.IsNullOrEmpty(current))
        {
            if (!index.AddRelevant(current))
            {
                // Already processed this branch, so its ancestors are known too.
                break;
            }

            if (string.Equals(current, root, StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            var parent = Path.GetDirectoryName(current);
            current = parent is null ? null : DirectoryScanner.NormalizeFolderPath(parent);
        }
    }
}
