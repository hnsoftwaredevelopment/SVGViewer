using System.Diagnostics;
using System.IO;

namespace SVGViewer.Services;

/// <summary>Opens a generated HTML file (used to show the help in the browser).</summary>
public interface IHelpLauncher
{
    void Open(string path);
}

/// <summary>Opens a file with its associated application via the shell.</summary>
public sealed class ShellHelpLauncher : IHelpLauncher
{
    public void Open(string path) =>
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
}

/// <summary>
/// Provides in-app help: it takes the localized user guide shipped next to the
/// executable, renders it to a styled HTML page, and opens it in the default
/// browser. Rendering to the browser is offline and does not depend on any
/// Markdown file association being present on the machine.
/// </summary>
public sealed class HelpService
{
    private static readonly string[] Supported = { "nl", "en", "de" };

    private readonly string _helpDirectory;
    private readonly string _outputDirectory;
    private readonly IHelpLauncher _launcher;

    public HelpService(string? helpDirectory = null, string? outputDirectory = null, IHelpLauncher? launcher = null)
    {
        _helpDirectory = helpDirectory ?? Path.Combine(AppContext.BaseDirectory, "Help");
        _outputDirectory = outputDirectory ?? Path.Combine(Path.GetTempPath(), "SVGViewer");
        _launcher = launcher ?? new ShellHelpLauncher();
    }

    /// <summary>Normalizes any culture name to one of the supported guide languages.</summary>
    public static string NormalizeCulture(string? culture)
    {
        var two = (culture ?? "nl").Trim().ToLowerInvariant();
        var dash = two.IndexOf('-');
        if (dash > 0) two = two[..dash];
        return Array.IndexOf(Supported, two) >= 0 ? two : "nl";
    }

    /// <summary>
    /// Returns the guide file for a culture, falling back to Dutch when the
    /// requested language's guide is missing. The returned path may still not
    /// exist if no guide is present at all.
    /// </summary>
    public string ResolveGuidePath(string? culture)
    {
        var wanted = Path.Combine(_helpDirectory, $"QuickReference.{NormalizeCulture(culture)}.md");
        if (File.Exists(wanted)) return wanted;

        var dutch = Path.Combine(_helpDirectory, "QuickReference.nl.md");
        return File.Exists(dutch) ? dutch : wanted;
    }

    /// <summary>
    /// Renders the active-language guide to an HTML file in the output directory
    /// and returns its path. Throws <see cref="FileNotFoundException"/> when no
    /// guide is available to render.
    /// </summary>
    public string GenerateHelpFile(string? culture)
    {
        var guide = ResolveGuidePath(culture);
        if (!File.Exists(guide))
            throw new FileNotFoundException("No user guide is available.", guide);

        var markdown = File.ReadAllText(guide);

        // Image paths in the guides are relative to the guide itself (e.g.
        // "images/foo.png"), so the base is the guide's own folder.
        var guideDir = Path.GetDirectoryName(guide) ?? _helpDirectory;
        var imageBase = new Uri(guideDir + Path.DirectorySeparatorChar).AbsoluteUri;

        var html = MarkdownToHtml.Convert(markdown, "SVG Viewer – Help", imageBase);

        Directory.CreateDirectory(_outputDirectory);
        var outPath = Path.Combine(_outputDirectory, $"help-{NormalizeCulture(culture)}.html");
        File.WriteAllText(outPath, html);
        return outPath;
    }

    /// <summary>Generates and opens the help page for the active culture.</summary>
    public void OpenGuide(string? culture)
    {
        var path = GenerateHelpFile(culture);
        _launcher.Open(path);
    }
}
