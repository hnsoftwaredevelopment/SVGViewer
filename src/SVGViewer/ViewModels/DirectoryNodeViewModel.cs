using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using SVGViewer.Localization;
using SVGViewer.Models;
using SVGViewer.Services;

namespace SVGViewer.ViewModels;

/// <summary>
/// One folder in the tree. Children are loaded lazily the first time the node is
/// expanded, so opening a drive never blocks on a full recursive scan.
/// </summary>
public partial class DirectoryNodeViewModel : ObservableObject
{
    private readonly SvgFolderIndex? _index;
    private readonly FolderFilterMode _filterMode;
    private bool _childrenLoaded;

    public DirectoryNodeViewModel(
        string fullPath,
        string displayName,
        FolderFilterMode filterMode,
        SvgFolderIndex? index)
    {
        FullPath = DirectoryScanner.NormalizeFolderPath(fullPath);
        DisplayName = displayName;
        _filterMode = filterMode;
        _index = index;

        Children = new ObservableCollection<DirectoryNodeViewModel>();

        // The folder's own SVG count is read straight from disk so a folder with
        // SVGs is marked the instant it appears, without waiting for the scan.
        SvgFileCount = DirectoryScanner.CountSvgFiles(FullPath);

        // "Ancestor" = leads to SVGs deeper down but has none itself. This comes
        // from the background scan's index and fills in progressively.
        _isAncestorOfSvg = ComputeIsAncestor();

        AddPlaceholderIfNeeded();
    }

    public string FullPath { get; }

    public string DisplayName { get; }

    /// <summary>Number of SVG files directly in this folder (drives the count badge).</summary>
    public int SvgFileCount { get; }

    /// <summary>True when this folder directly contains SVG files.</summary>
    public bool HasSvgFiles => SvgFileCount > 0;

    /// <summary>True when this folder itself has no SVGs but a descendant does.</summary>
    [ObservableProperty]
    private bool _isAncestorOfSvg;

    /// <summary>True when the folder should be highlighted (direct or ancestor).</summary>
    public bool IsMarked => HasSvgFiles || IsAncestorOfSvg;

    public string SvgTooltip => Loc.Format("TooltipContainsSvg", SvgFileCount);

    partial void OnIsAncestorOfSvgChanged(bool value) => OnPropertyChanged(nameof(IsMarked));

    private bool ComputeIsAncestor() =>
        _index is not null && SvgFileCount == 0 && _index.IsRelevant(FullPath);

    /// <summary>
    /// Re-reads the (still filling) scan index and updates the ancestor marking,
    /// then does the same for any children already loaded. Cheap: only realized
    /// nodes are touched.
    /// </summary>
    public void RefreshMarking()
    {
        IsAncestorOfSvg = ComputeIsAncestor();

        foreach (var child in Children)
        {
            if (!child.IsPlaceholder)
            {
                child.RefreshMarking();
            }
        }
    }

    public ObservableCollection<DirectoryNodeViewModel> Children { get; }

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isExpanded;

    /// <summary>
    /// Expanding a node triggers the lazy load of its children.
    /// </summary>
    partial void OnIsExpandedChanged(bool value)
    {
        if (value)
        {
            LoadChildren();
        }
    }

    /// <summary>
    /// Placeholder child so WPF renders an expander arrow for folders we have
    /// not opened yet. Folders known to have no relevant children get none.
    /// </summary>
    private void AddPlaceholderIfNeeded()
    {
        if (HasPotentialChildren())
        {
            Children.Add(CreatePlaceholder());
        }
    }

    private bool HasPotentialChildren()
    {
        if (_filterMode == FolderFilterMode.SvgOnly && _index is not null)
        {
            // Only relevant sub-folders will ever be shown.
            return DirectoryScanner.GetSubDirectories(FullPath)
                .Any(d => _index.IsRelevant(d.FullName));
        }

        return DirectoryScanner.GetSubDirectories(FullPath).Count > 0;
    }

    private static DirectoryNodeViewModel CreatePlaceholder() =>
        new(string.Empty, Loc.Get("TreeLoading"), FolderFilterMode.All, null) { IsPlaceholder = true };

    public bool IsPlaceholder { get; private init; }

    /// <summary>Loads the real child folders, replacing the placeholder.</summary>
    public void LoadChildren()
    {
        if (_childrenLoaded)
        {
            return;
        }

        _childrenLoaded = true;
        Children.Clear();

        foreach (var directory in DirectoryScanner.GetSubDirectories(FullPath))
        {
            if (_filterMode == FolderFilterMode.SvgOnly &&
                _index is not null &&
                !_index.IsRelevant(directory.FullName))
            {
                continue;
            }

            Children.Add(new DirectoryNodeViewModel(
                directory.FullName,
                directory.Name,
                _filterMode,
                _index));
        }
    }

    /// <summary>Expands this node and loads children immediately (used for roots).</summary>
    public void ExpandAndLoad()
    {
        LoadChildren();
        IsExpanded = true;
    }
}
