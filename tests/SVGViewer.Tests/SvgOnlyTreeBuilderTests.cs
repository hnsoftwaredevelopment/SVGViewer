using SVGViewer.Services;
using SVGViewer.ViewModels;
using Xunit;

namespace SVGViewer.Tests;

public class SvgOnlyTreeBuilderTests
{
    private static (DirectoryNodeViewModel root, HashSet<string> inserted) NewTree(
        TestTree tree, SvgFolderIndex index)
    {
        var root = DirectoryNodeViewModel.CreateExplicit(tree.Root, "root", index);
        return (root, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Builds_only_the_folders_that_lead_to_svgs()
    {
        using var tree = new TestTree();
        var index = await new SvgIndexService().BuildIndexAsync(tree.Root);
        var (root, inserted) = NewTree(tree, index);

        SvgOnlyTreeBuilder.Sync(root, tree.Root, index, inserted);

        // Root shows A and B (both lead to SVGs); C and A\Empty are excluded.
        Assert.Equal(new[] { "A", "B" }, root.Children.Select(c => c.DisplayName).ToArray());

        var a = root.Children.Single(c => c.DisplayName == "A");
        Assert.Equal(new[] { "Icons" }, a.Children.Select(c => c.DisplayName).ToArray());

        var icons = a.Children.Single();
        Assert.Equal(2, icons.SvgFileCount);
        Assert.True(icons.HasSvgFiles);
    }

    [Fact]
    public async Task Ancestor_nodes_are_marked_without_a_count()
    {
        using var tree = new TestTree();
        var index = await new SvgIndexService().BuildIndexAsync(tree.Root);
        var (root, inserted) = NewTree(tree, index);

        SvgOnlyTreeBuilder.Sync(root, tree.Root, index, inserted);

        var b = root.Children.Single(c => c.DisplayName == "B");
        Assert.True(b.IsAncestorOfSvg);
        Assert.False(b.HasSvgFiles);

        var deeper = b.Children.Single().Children.Single();
        Assert.Equal("deeper", deeper.DisplayName);
        Assert.Equal(1, deeper.SvgFileCount);
    }

    [Fact]
    public async Task Sync_is_additive_and_idempotent()
    {
        using var tree = new TestTree();
        var index = await new SvgIndexService().BuildIndexAsync(tree.Root);
        var (root, inserted) = NewTree(tree, index);

        SvgOnlyTreeBuilder.Sync(root, tree.Root, index, inserted);
        SvgOnlyTreeBuilder.Sync(root, tree.Root, index, inserted);
        SvgOnlyTreeBuilder.Sync(root, tree.Root, index, inserted);

        // No duplicate branches after repeated syncs.
        Assert.Equal(2, root.Children.Count);
        Assert.Single(root.Children.Single(c => c.DisplayName == "A").Children);
    }

    [Fact]
    public async Task Branches_appear_as_the_index_fills_progressively()
    {
        using var tree = new TestTree();

        // Empty index: nothing to show yet.
        var index = new SvgFolderIndex();
        var (root, inserted) = NewTree(tree, index);

        SvgOnlyTreeBuilder.Sync(root, tree.Root, index, inserted);
        Assert.Empty(root.Children);

        // The scan fills the same index; a later sync reveals the branches.
        await new SvgIndexService().BuildIndexAsync(tree.Root, index);
        SvgOnlyTreeBuilder.Sync(root, tree.Root, index, inserted);

        Assert.Equal(new[] { "A", "B" }, root.Children.Select(c => c.DisplayName).ToArray());
    }
}
