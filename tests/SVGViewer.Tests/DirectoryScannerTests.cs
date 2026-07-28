using System.IO;
using SVGViewer.Services;
using Xunit;

namespace SVGViewer.Tests;

public class DirectoryScannerTests
{
    [Fact]
    public void CountSvgFiles_counts_only_svg_files()
    {
        using var tree = new TestTree();

        Assert.Equal(2, DirectoryScanner.CountSvgFiles(tree.Icons));
        Assert.Equal(1, DirectoryScanner.CountSvgFiles(tree.Deep));
    }

    [Fact]
    public void CountSvgFiles_ignores_txt_and_png()
    {
        using var tree = new TestTree();

        Assert.Equal(0, DirectoryScanner.CountSvgFiles(tree.OtherFilesOnly));
        Assert.False(DirectoryScanner.HasSvgFiles(tree.OtherFilesOnly));
    }

    [Fact]
    public void HasSvgFiles_is_true_only_when_svg_present()
    {
        using var tree = new TestTree();

        Assert.True(DirectoryScanner.HasSvgFiles(tree.Icons));
        Assert.False(DirectoryScanner.HasSvgFiles(tree.Empty));
    }

    [Fact]
    public void GetSvgFiles_returns_svg_files_sorted_by_name()
    {
        using var tree = new TestTree();

        var files = DirectoryScanner.GetSvgFiles(tree.Icons);

        Assert.Equal(2, files.Count);
        Assert.All(files, f => Assert.EndsWith(".svg", f, StringComparison.OrdinalIgnoreCase));
        Assert.Equal("one.svg", Path.GetFileName(files[0]));
        Assert.Equal("two.svg", Path.GetFileName(files[1]));
    }

    [Theory]
    [InlineData("")]
    [InlineData(@"Z:\definitely\not\here")]
    public void Unreadable_paths_are_handled_without_throwing(string path)
    {
        Assert.Equal(0, DirectoryScanner.CountSvgFiles(path));
        Assert.False(DirectoryScanner.HasSvgFiles(path));
        Assert.Empty(DirectoryScanner.GetSvgFiles(path));
        Assert.Empty(DirectoryScanner.GetSubDirectories(path));
    }

    [Fact]
    public void GetSubDirectories_returns_sorted_readable_folders()
    {
        using var tree = new TestTree();

        var names = DirectoryScanner.GetSubDirectories(tree.Root)
                                    .Select(d => d.Name)
                                    .ToArray();

        Assert.Equal(new[] { "A", "B", "C" }, names);
    }

    // Regression: a drive root arrives as "C:\". Trimming the trailing separator
    // to "C:" makes Windows treat it as drive-relative (the process working
    // directory), so the tree showed bin\Debug instead of the real drive root.
    [Theory]
    [InlineData(@"C:\", @"C:\")]
    [InlineData(@"C:", @"C:\")]
    [InlineData(@"Z:\", @"Z:\")]
    [InlineData(@"C:\Windows\", @"C:\Windows")]
    [InlineData(@"C:\Windows", @"C:\Windows")]
    [InlineData(@"C:\a\b\", @"C:\a\b")]
    public void NormalizeFolderPath_keeps_drive_roots_but_trims_folders(string input, string expected)
    {
        Assert.Equal(expected, DirectoryScanner.NormalizeFolderPath(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeFolderPath_handles_blank_input(string input)
    {
        Assert.Equal(string.Empty, DirectoryScanner.NormalizeFolderPath(input));
    }

    [Fact]
    public void NormalizeFolderPath_does_not_collapse_a_drive_root_to_the_working_directory()
    {
        // The bug in numbers: "C:" resolves to the current directory, "C:\" does not.
        var root = DirectoryScanner.NormalizeFolderPath(@"C:\");

        Assert.Equal(@"C:\", root);
        Assert.NotEqual("C:", root);
    }
}
