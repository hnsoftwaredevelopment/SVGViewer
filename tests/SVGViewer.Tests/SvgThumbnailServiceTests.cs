using System.IO;
using System.Windows.Media;
using SVGViewer.Services;
using Xunit;

namespace SVGViewer.Tests;

/// <summary>
/// Rendering happens on worker threads, so these tests deliberately run off any
/// UI thread: if SharpVectors needed a Dispatcher, they would fail here.
/// </summary>
public class SvgThumbnailServiceTests
{
    [Fact]
    public async Task Renders_a_valid_svg_to_a_frozen_image()
    {
        using var tree = new TestTree();
        var service = new SvgThumbnailService();

        var image = await service.GetThumbnailAsync(Path.Combine(tree.Icons, "one.svg"));

        Assert.NotNull(image);
        Assert.IsType<DrawingImage>(image);

        // Must be frozen, otherwise the UI thread cannot touch it.
        Assert.True(image!.IsFrozen);
    }

    [Fact]
    public async Task Malformed_svg_returns_null_instead_of_throwing()
    {
        using var tree = new TestTree();
        var broken = Path.Combine(tree.Root, "broken.svg");
        File.WriteAllText(broken, "<svg><this is not valid");

        var image = await new SvgThumbnailService().GetThumbnailAsync(broken);

        Assert.Null(image);
    }

    [Fact]
    public async Task Missing_file_returns_null()
    {
        var image = await new SvgThumbnailService()
            .GetThumbnailAsync(@"Z:\nope\missing.svg");

        Assert.Null(image);
    }

    [Fact]
    public async Task Non_svg_content_returns_null()
    {
        using var tree = new TestTree();

        var image = await new SvgThumbnailService()
            .GetThumbnailAsync(Path.Combine(tree.OtherFilesOnly, "notes.txt"));

        Assert.Null(image);
    }

    [Fact]
    public async Task Second_request_is_served_from_cache()
    {
        using var tree = new TestTree();
        var service = new SvgThumbnailService();
        var path = Path.Combine(tree.Icons, "one.svg");

        var first = await service.GetThumbnailAsync(path);
        var second = await service.GetThumbnailAsync(path);

        Assert.Same(first, second);
        Assert.Equal(1, service.CachedCount);
    }

    [Fact]
    public async Task Editing_a_file_invalidates_its_cache_entry()
    {
        using var tree = new TestTree();
        var service = new SvgThumbnailService();
        var path = Path.Combine(tree.Icons, "one.svg");

        await service.GetThumbnailAsync(path);

        // The cache key includes the last-write time.
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddMinutes(5));
        await service.GetThumbnailAsync(path);

        Assert.Equal(2, service.CachedCount);
    }

    [Fact]
    public async Task ClearCache_empties_the_cache()
    {
        using var tree = new TestTree();
        var service = new SvgThumbnailService();

        await service.GetThumbnailAsync(Path.Combine(tree.Icons, "one.svg"));
        Assert.Equal(1, service.CachedCount);

        service.ClearCache();

        Assert.Equal(0, service.CachedCount);
    }

    [Fact]
    public async Task Renders_many_files_concurrently_without_error()
    {
        using var tree = new TestTree();
        var service = new SvgThumbnailService();

        var paths = new[]
        {
            Path.Combine(tree.Icons, "one.svg"),
            Path.Combine(tree.Icons, "two.svg"),
            Path.Combine(tree.Deep, "buried.svg")
        };

        var images = await Task.WhenAll(paths.Select(p => service.GetThumbnailAsync(p)));

        Assert.All(images, image => Assert.NotNull(image));
    }
}
