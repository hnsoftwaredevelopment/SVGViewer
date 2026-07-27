using System.IO;

namespace SVGViewer.Tests;

/// <summary>
/// Creates a disposable folder tree used by the file-system tests:
/// <code>
///   A\Icons          2 SVG files      -> must be marked
///   A\Empty          nothing          -> must not be marked
///   B\deep\deeper    1 SVG file       -> ancestors must stay visible
///   C                .txt and .png    -> must not be marked
/// </code>
/// </summary>
public sealed class TestTree : IDisposable
{
    private const string SvgContent =
        """<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24"><circle cx="12" cy="12" r="10" fill="teal"/></svg>""";

    public TestTree()
    {
        Root = Path.Combine(Path.GetTempPath(), "SVGViewerTests", Guid.NewGuid().ToString("N"));

        Icons = Path.Combine(Root, "A", "Icons");
        Empty = Path.Combine(Root, "A", "Empty");
        Deep = Path.Combine(Root, "B", "deep", "deeper");
        OtherFilesOnly = Path.Combine(Root, "C");

        Directory.CreateDirectory(Icons);
        Directory.CreateDirectory(Empty);
        Directory.CreateDirectory(Deep);
        Directory.CreateDirectory(OtherFilesOnly);

        File.WriteAllText(Path.Combine(Icons, "one.svg"), SvgContent);
        File.WriteAllText(Path.Combine(Icons, "two.svg"), SvgContent);
        File.WriteAllText(Path.Combine(Deep, "buried.svg"), SvgContent);
        File.WriteAllText(Path.Combine(OtherFilesOnly, "notes.txt"), "not an svg");
        File.WriteAllText(Path.Combine(OtherFilesOnly, "image.png"), "not an svg");
    }

    /// <summary>Root of the generated tree.</summary>
    public string Root { get; }

    /// <summary>Folder containing two SVG files.</summary>
    public string Icons { get; }

    /// <summary>Folder containing nothing at all.</summary>
    public string Empty { get; }

    /// <summary>Deeply nested folder containing one SVG file.</summary>
    public string Deep { get; }

    /// <summary>Folder containing only non-SVG files.</summary>
    public string OtherFilesOnly { get; }

    /// <summary>Path of a folder inside the tree, e.g. <c>Path("A", "Icons")</c>.</summary>
    public string Path_(params string[] parts) =>
        System.IO.Path.Combine(new[] { Root }.Concat(parts).ToArray());

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
        catch (IOException)
        {
            // A locked file must not fail the test run.
        }
    }
}
