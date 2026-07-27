namespace SVGViewer.Models;

/// <summary>Size of the SVG previews shown for the selected folder.</summary>
public enum PreviewSize
{
    Large,
    Medium,
    Small,
    DetailsOnly
}

/// <summary>Determines which folders are shown in the tree.</summary>
public enum FolderFilterMode
{
    /// <summary>Show every folder on the drive.</summary>
    All,

    /// <summary>Show only folders that contain SVG files (plus their parents).</summary>
    SvgOnly
}
