using System.IO;
using SVGViewer.Services;
using Xunit;

namespace SVGViewer.Tests;

public class SvgIndexServiceTests
{
    private static Task<SvgFolderIndex> BuildAsync(TestTree tree) =>
        new SvgIndexService().BuildIndexAsync(tree.Root);

    [Fact]
    public async Task Marks_only_folders_that_contain_svg_files()
    {
        using var tree = new TestTree();
        var index = await BuildAsync(tree);

        Assert.Equal(2, index.FoldersWithSvg.Count);
        Assert.True(index.ContainsSvg(tree.Icons));
        Assert.True(index.ContainsSvg(tree.Deep));
        Assert.False(index.ContainsSvg(tree.OtherFilesOnly));
        Assert.False(index.ContainsSvg(tree.Empty));
    }

    [Fact]
    public async Task Parent_of_svg_folder_is_not_marked_itself()
    {
        using var tree = new TestTree();
        var index = await BuildAsync(tree);

        Assert.False(index.ContainsSvg(tree.Path_("B")));
    }

    [Fact]
    public async Task Ancestors_of_a_buried_svg_stay_relevant()
    {
        using var tree = new TestTree();
        var index = await BuildAsync(tree);

        // Without this the filtered tree could never reach the buried file.
        Assert.True(index.IsRelevant(tree.Root));
        Assert.True(index.IsRelevant(tree.Path_("B")));
        Assert.True(index.IsRelevant(tree.Path_("B", "deep")));
        Assert.True(index.IsRelevant(tree.Deep));
    }

    [Fact]
    public async Task Irrelevant_folders_are_excluded()
    {
        using var tree = new TestTree();
        var index = await BuildAsync(tree);

        Assert.False(index.IsRelevant(tree.OtherFilesOnly));
        Assert.False(index.IsRelevant(tree.Empty));

        // root, A, A\Icons, B, B\deep, B\deep\deeper
        Assert.Equal(6, index.RelevantFolders.Count);
    }

    [Fact]
    public async Task Reports_svg_count_per_folder()
    {
        using var tree = new TestTree();
        var index = await BuildAsync(tree);

        Assert.Equal(2, index.GetSvgCount(tree.Icons));
        Assert.Equal(1, index.GetSvgCount(tree.Deep));
        Assert.Equal(0, index.GetSvgCount(tree.OtherFilesOnly));
    }

    [Fact]
    public async Task Trailing_backslash_does_not_change_lookups()
    {
        using var tree = new TestTree();
        var index = await BuildAsync(tree);

        Assert.True(index.ContainsSvg(tree.Icons + "\\"));
        Assert.True(index.IsRelevant(tree.Path_("B") + "\\"));
    }

    [Fact]
    public async Task Walks_every_folder_once()
    {
        using var tree = new TestTree();
        var index = await BuildAsync(tree);

        // root, A, A\Icons, A\Empty, B, B\deep, B\deep\deeper, C
        Assert.Equal(8, index.TotalFoldersScanned);
        Assert.False(index.WasCancelled);
    }

    [Fact]
    public async Task Reports_progress_while_scanning()
    {
        using var tree = new TestTree();
        var reports = new List<ScanProgress>();
        var progress = new Progress<ScanProgress>(reports.Add);

        await new SvgIndexService().BuildIndexAsync(tree.Root, progress);

        // Progress is posted asynchronously, so allow the callbacks to arrive.
        for (var attempt = 0; attempt < 20 && reports.Count == 0; attempt++)
        {
            await Task.Delay(25);
        }

        Assert.NotEmpty(reports);
    }

    [Fact]
    public async Task Cancellation_is_honoured()
    {
        using var tree = new TestTree();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        try
        {
            var index = await new SvgIndexService().BuildIndexAsync(tree.Root, null, cts.Token);
            Assert.True(index.WasCancelled);
        }
        catch (OperationCanceledException)
        {
            // Equally acceptable: the task itself reports cancellation.
        }
    }

    [Fact]
    public async Task Empty_tree_yields_no_results()
    {
        var root = Path.Combine(Path.GetTempPath(), "SVGViewerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var index = await new SvgIndexService().BuildIndexAsync(root);

            Assert.Empty(index.FoldersWithSvg);
            Assert.Empty(index.RelevantFolders);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
