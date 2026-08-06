using System.Collections.Concurrent;
using System.IO;

namespace SVGViewer.Services;

/// <summary>
/// Thread-safe requests to scan a user-selected folder ahead of the normal
/// background traversal. Requests are advisory: the scanner still visits every
/// accessible folder exactly once.
/// </summary>
public sealed class SvgScanPriority
{
    private readonly ConcurrentQueue<string> _requests = new();

    public void Prioritize(string path)
    {
        if (!string.IsNullOrWhiteSpace(path))
        {
            _requests.Enqueue(DirectoryScanner.NormalizeFolderPath(path));
        }
    }

    internal bool TryDequeue(out string path) => _requests.TryDequeue(out path!);
}

/// <summary>Mutable checkpoint for an optimized scan that may be paused and resumed.</summary>
public sealed class SvgScanWorkState
{
    internal Stack<string> Pending { get; } = new();
    internal HashSet<string> Visited { get; } = new(StringComparer.OrdinalIgnoreCase);
    internal int FoldersWithSvg { get; set; }

    public SvgScanWorkState(string rootPath, SvgFolderIndex? index = null, SvgScanPriority? priority = null)
    {
        RootPath = DirectoryScanner.NormalizeFolderPath(rootPath);
        Index = index ?? new SvgFolderIndex();
        Priority = priority ?? new SvgScanPriority();
        Pending.Push(RootPath);
    }

    public string RootPath { get; }
    public SvgFolderIndex Index { get; }
    public SvgScanPriority Priority { get; }
    public bool IsComplete { get; internal set; }
    public TimeSpan Elapsed { get; internal set; }
}

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

    /// <summary>
    /// Updates a folder's SVG count after a file operation (e.g. a delete). When
    /// the count reaches zero the folder drops out of <see cref="FoldersWithSvg"/>
    /// so its highlight/count badge disappears. Ancestor relevance is left as-is;
    /// a full Refresh recomputes it.
    /// </summary>
    internal void SetSvgCount(string path, int count)
    {
        var normalized = DirectoryScanner.NormalizeFolderPath(path);
        if (count > 0)
        {
            _svgCounts[normalized] = count;
        }
        else
        {
            _svgCounts.TryRemove(normalized, out _);
        }
    }

    /// <summary>Marks a folder as on-the-path-to an SVG folder. Returns false if
    /// it was already marked (so the caller can stop walking up).</summary>
    internal bool AddRelevant(string normalizedPath) =>
        _relevant.TryAdd(normalizedPath, 0);

    /// <summary>Reconstructs a completed index from the persistent scan cache.</summary>
    internal static SvgFolderIndex FromCachedData(
        IEnumerable<KeyValuePair<string, int>> svgFolders,
        IEnumerable<string> relevantFolders,
        int totalFoldersScanned)
    {
        var index = new SvgFolderIndex { TotalFoldersScanned = totalFoldersScanned };
        foreach (var folder in svgFolders)
        {
            if (folder.Value > 0)
            {
                index.AddSvgFolder(folder.Key, folder.Value);
            }
        }

        foreach (var folder in relevantFolders)
        {
            index.AddRelevant(folder);
        }

        return index;
    }
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
    /// Continues an optimized scan from its saved work state. Cancelling this
    /// operation leaves the pending paths in place, ready for a later resume.
    /// </summary>
    public Task<SvgFolderIndex> BuildIndexAsync(
        SvgScanWorkState state,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() => BuildIndex(state, progress, cancellationToken), cancellationToken);
    }

    /// <summary>
    /// Scans a drive into the supplied index. Passing the index in advance lets
    /// the tree bind to it and light up folders progressively as the scan runs.
    /// </summary>
    public Task<SvgFolderIndex> BuildIndexAsync(
        string rootPath,
        SvgFolderIndex index,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default,
        SvgScanPriority? priority = null)
    {
        var state = new SvgScanWorkState(rootPath, index, priority);
        return Task.Run(() => BuildIndex(state, progress, cancellationToken), cancellationToken);
    }

    private static SvgFolderIndex BuildIndex(
        SvgScanWorkState state,
        IProgress<ScanProgress>? progress,
        CancellationToken cancellationToken)
    {
        while (!state.IsComplete && TryTakeNextFolder(
                   state.Pending, state.Visited, state.Priority, state.RootPath, out var current))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                state.Index.WasCancelled = true;
                state.Visited.Remove(current);
                state.Pending.Push(current);
                break;
            }

            DirectoryScanResult scanResult;
            try
            {
                scanResult = DirectoryScanner.ScanDirectory(current, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Put the interrupted folder back so a resumed scan never loses it.
                state.Index.WasCancelled = true;
                state.Visited.Remove(current);
                state.Pending.Push(current);
                break;
            }

            state.Index.TotalFoldersScanned++;
            if (scanResult.SvgFileCount > 0)
            {
                state.Index.AddSvgFolder(current, scanResult.SvgFileCount);
                state.FoldersWithSvg++;
                MarkAncestorsRelevant(state.Index, current, state.RootPath);
            }

            foreach (var child in scanResult.SubDirectories)
            {
                state.Pending.Push(child);
            }

            if (state.Index.TotalFoldersScanned % ProgressInterval == 0)
            {
                progress?.Report(new ScanProgress(
                    state.Index.TotalFoldersScanned, state.FoldersWithSvg, current));
            }
        }

        if (state.Pending.Count == 0)
        {
            state.IsComplete = true;
        }

        progress?.Report(new ScanProgress(
            state.Index.TotalFoldersScanned, state.FoldersWithSvg, state.RootPath));
        return state.Index;
    }

    /// <summary>
    /// Takes a user-requested folder first after the root has been scanned. A
    /// regular-stack copy of the same path is skipped later through <paramref
    /// name="visited"/>, so reprioritizing never causes a duplicate scan.
    /// </summary>
    private static bool TryTakeNextFolder(
        Stack<string> pending,
        HashSet<string> visited,
        SvgScanPriority? priority,
        string root,
        out string current)
    {
        // Always scan the root first: it establishes the ordinary traversal and
        // prevents a request from an obsolete tree from jumping ahead of it.
        if (visited.Count == 0 && pending.TryPop(out var rootFolder))
        {
            current = rootFolder!;
            visited.Add(current);
            return true;
        }

        while (priority is not null && priority.TryDequeue(out var requested))
        {
            if (IsAtOrBelowRoot(requested, root) && visited.Add(requested))
            {
                current = requested;
                return true;
            }
        }

        while (pending.TryPop(out var candidate))
        {
            if (visited.Add(candidate))
            {
                current = candidate;
                return true;
            }
        }

        current = string.Empty;
        return false;
    }

    private static bool IsAtOrBelowRoot(string path, string root)
    {
        string canonicalPath;
        string canonicalRoot;
        try
        {
            canonicalPath = DirectoryScanner.NormalizeFolderPath(Path.GetFullPath(path));
            canonicalRoot = DirectoryScanner.NormalizeFolderPath(Path.GetFullPath(root));
        }
        catch (Exception)
        {
            return false;
        }

        if (string.Equals(canonicalPath, canonicalRoot, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var rootPrefix = canonicalRoot.EndsWith(Path.DirectorySeparatorChar)
            ? canonicalRoot
            : canonicalRoot + Path.DirectorySeparatorChar;
        return canonicalPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase);
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
