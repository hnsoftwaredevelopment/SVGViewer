using System.Collections.ObjectModel;
using System.IO;
using SVGViewer.Services;

namespace SVGViewer.ViewModels;

/// <summary>
/// Builds the "SVG only" tree incrementally from the scan index. Because a
/// folder's relevance only ever grows (once it leads to an SVG it always will),
/// building is purely additive: branches appear as they are discovered and
/// nothing is ever removed or reordered, so there is no flicker.
/// </summary>
public static class SvgOnlyTreeBuilder
{
    /// <summary>
    /// Ensures every folder that directly contains SVGs (and its ancestors) is
    /// present under <paramref name="root"/>. Idempotent; pass the same
    /// <paramref name="alreadyInserted"/> set across calls to skip known folders.
    /// </summary>
    public static void Sync(
        DirectoryNodeViewModel root,
        string rootPath,
        SvgFolderIndex index,
        ISet<string> alreadyInserted)
    {
        foreach (var folder in index.FoldersWithSvg.ToArray())
        {
            if (alreadyInserted.Add(folder))
            {
                InsertPath(root, rootPath, folder, index);
            }
        }
    }

    private static void InsertPath(
        DirectoryNodeViewModel root,
        string rootPath,
        string target,
        SvgFolderIndex index)
    {
        var normRoot = DirectoryScanner.NormalizeFolderPath(rootPath);
        var normTarget = DirectoryScanner.NormalizeFolderPath(target);

        if (string.Equals(normRoot, normTarget, StringComparison.OrdinalIgnoreCase) ||
            !normTarget.StartsWith(normRoot, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var relative = normTarget[normRoot.Length..].Trim(Path.DirectorySeparatorChar);
        var segments = relative.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

        var current = root;
        var currentPath = normRoot;

        foreach (var segment in segments)
        {
            currentPath = DirectoryScanner.NormalizeFolderPath(Path.Combine(currentPath, segment));

            var child = current.Children.FirstOrDefault(c =>
                string.Equals(c.FullPath, currentPath, StringComparison.OrdinalIgnoreCase));

            if (child is null)
            {
                child = DirectoryNodeViewModel.CreateExplicit(currentPath, segment, index);
                InsertSorted(current.Children, child);
            }

            current = child;
        }
    }

    private static void InsertSorted(
        ObservableCollection<DirectoryNodeViewModel> children,
        DirectoryNodeViewModel node)
    {
        var i = 0;
        while (i < children.Count &&
               string.Compare(children[i].DisplayName, node.DisplayName, StringComparison.OrdinalIgnoreCase) < 0)
        {
            i++;
        }

        children.Insert(i, node);
    }
}
