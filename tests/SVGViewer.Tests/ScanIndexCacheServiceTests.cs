using System.IO;
using SVGViewer.Services;
using Xunit;

namespace SVGViewer.Tests;

public class ScanIndexCacheServiceTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "SVGViewerTests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Completed_scan_survives_a_service_restart()
    {
        using var tree = new TestTree();
        var path = Path.Combine(_folder, "scan-cache.json");
        var index = await new SvgIndexService().BuildIndexAsync(tree.Root);

        new ScanIndexCacheService(path).Save(tree.Root, index);
        var restored = new ScanIndexCacheService(path).Load(tree.Root);

        Assert.NotNull(restored);
        Assert.Equal(index.TotalFoldersScanned, restored.TotalFoldersScanned);
        Assert.Equal(index.FoldersWithSvg.Order(), restored.FoldersWithSvg.Order());
        Assert.Equal(index.RelevantFolders.Order(), restored.RelevantFolders.Order());
        Assert.Equal(2, restored.GetSvgCount(tree.Icons));
    }

    [Fact]
    public void Corrupt_cache_is_ignored()
    {
        Directory.CreateDirectory(_folder);
        var path = Path.Combine(_folder, "scan-cache.json");
        File.WriteAllText(path, "{ this is not valid json");

        var restored = new ScanIndexCacheService(path).Load("C:\\");

        Assert.Null(restored);
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }
}
