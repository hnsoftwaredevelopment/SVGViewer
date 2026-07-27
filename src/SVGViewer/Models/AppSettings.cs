namespace SVGViewer.Models;

/// <summary>
/// User preferences, persisted as JSON in %AppData%\SVGViewer\settings.json.
/// </summary>
public sealed class AppSettings
{
    /// <summary>UI language: "nl" (default), "en" or "de".</summary>
    public string Language { get; set; } = "nl";

    /// <summary>Preferred preview size.</summary>
    public PreviewSize PreviewSize { get; set; } = PreviewSize.Medium;

    /// <summary>Which folders to show in the tree.</summary>
    public FolderFilterMode FilterMode { get; set; } = FolderFilterMode.All;

    /// <summary>Drive selected during the previous session, e.g. "C:\".</summary>
    public string? LastDrive { get; set; }
}
