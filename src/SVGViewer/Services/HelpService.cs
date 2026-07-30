using System.IO;

namespace SVGViewer.Services;

/// <summary>
/// Provides the in-app quick reference: it locates the localized Markdown file
/// shipped next to the executable and returns its text, so the UI can render it
/// inside the app. Falls back to Dutch when the requested language is missing.
/// </summary>
public sealed class HelpService
{
    private static readonly string[] Supported = { "nl", "en", "de" };

    private readonly string _helpDirectory;

    public HelpService(string? helpDirectory = null)
    {
        _helpDirectory = helpDirectory ?? Path.Combine(AppContext.BaseDirectory, "Help");
    }

    /// <summary>Normalizes any culture name to one of the supported languages.</summary>
    public static string NormalizeCulture(string? culture)
    {
        var two = (culture ?? "nl").Trim().ToLowerInvariant();
        var dash = two.IndexOf('-');
        if (dash > 0) two = two[..dash];
        return Array.IndexOf(Supported, two) >= 0 ? two : "nl";
    }

    /// <summary>
    /// Returns the quick-reference file for a culture, falling back to Dutch when
    /// the requested language's file is missing. The returned path may still not
    /// exist if no file is present at all.
    /// </summary>
    public string ResolveGuidePath(string? culture)
    {
        var wanted = Path.Combine(_helpDirectory, $"QuickReference.{NormalizeCulture(culture)}.md");
        if (File.Exists(wanted)) return wanted;

        var dutch = Path.Combine(_helpDirectory, "QuickReference.nl.md");
        return File.Exists(dutch) ? dutch : wanted;
    }

    /// <summary>
    /// Reads the quick-reference Markdown for the active culture. Throws
    /// <see cref="FileNotFoundException"/> when no file is available.
    /// </summary>
    public string ReadQuickReference(string? culture)
    {
        var path = ResolveGuidePath(culture);
        if (!File.Exists(path))
            throw new FileNotFoundException("No quick reference is available.", path);

        return File.ReadAllText(path);
    }
}
