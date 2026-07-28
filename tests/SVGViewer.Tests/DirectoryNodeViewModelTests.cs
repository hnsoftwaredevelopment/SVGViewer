using SVGViewer.Models;
using SVGViewer.Services;
using SVGViewer.ViewModels;
using Xunit;

namespace SVGViewer.Tests;

public class DirectoryNodeViewModelTests
{
    private static DirectoryNodeViewModel Node(string path, string name, SvgFolderIndex index) =>
        new(path, name, FolderFilterMode.All, index);

    [Fact]
    public async Task Direct_svg_folder_is_marked_with_a_count()
    {
        using var tree = new TestTree();
        var index = await new SvgIndexService().BuildIndexAsync(tree.Root);

        var node = Node(tree.Icons, "Icons", index);

        Assert.True(node.HasSvgFiles);
        Assert.Equal(2, node.SvgFileCount);
        Assert.False(node.IsAncestorOfSvg);
        Assert.True(node.IsMarked);
    }

    [Fact]
    public async Task Ancestor_folder_is_marked_without_a_count()
    {
        using var tree = new TestTree();
        var index = await new SvgIndexService().BuildIndexAsync(tree.Root);

        // B leads to B\deep\deeper\buried.svg but has no SVGs of its own.
        var node = Node(tree.Path_("B"), "B", index);

        Assert.False(node.HasSvgFiles);
        Assert.Equal(0, node.SvgFileCount);
        Assert.True(node.IsAncestorOfSvg);
        Assert.True(node.IsMarked);
    }

    [Fact]
    public async Task Folders_without_svgs_anywhere_below_are_not_marked()
    {
        using var tree = new TestTree();
        var index = await new SvgIndexService().BuildIndexAsync(tree.Root);

        var otherFiles = Node(tree.OtherFilesOnly, "C", index);
        var empty = Node(tree.Empty, "Empty", index);

        Assert.False(otherFiles.IsMarked);
        Assert.False(otherFiles.IsAncestorOfSvg);
        Assert.False(empty.IsMarked);
    }

    [Fact]
    public async Task Marking_fills_in_as_the_scan_populates_the_index()
    {
        using var tree = new TestTree();

        // Start with an empty index, as when the tree is shown before scanning.
        var index = new SvgFolderIndex();
        var node = Node(tree.Path_("B"), "B", index);

        Assert.False(node.IsMarked);

        // The background scan fills the same index instance...
        await new SvgIndexService().BuildIndexAsync(tree.Root, index);

        // ...and a refresh lights the node up.
        node.RefreshMarking();

        Assert.True(node.IsAncestorOfSvg);
        Assert.True(node.IsMarked);
    }

    [Fact]
    public async Task RefreshMarking_updates_already_loaded_children()
    {
        using var tree = new TestTree();

        var index = new SvgFolderIndex();
        var root = Node(tree.Root, "root", index);
        root.ExpandAndLoad(); // realizes A, B and C

        Assert.All(root.Children, child => Assert.False(child.IsMarked));

        await new SvgIndexService().BuildIndexAsync(tree.Root, index);
        root.RefreshMarking();

        var b = root.Children.Single(c => c.DisplayName == "B");
        var c = root.Children.Single(child => child.DisplayName == "C");

        Assert.True(b.IsMarked);       // leads to the buried SVG
        Assert.True(b.IsAncestorOfSvg);
        Assert.False(c.IsMarked);      // only .txt/.png
    }
}
