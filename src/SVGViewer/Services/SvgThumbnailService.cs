using System.Collections.Concurrent;
using System.IO;
using System.Windows.Media;
using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;

namespace SVGViewer.Services;

/// <summary>
/// Renders SVG files to WPF image sources on a background thread.
/// </summary>
/// <remarks>
/// The result is a vector <see cref="DrawingImage"/>, so a single render serves
/// every preview size: WPF scales it without loss and without re-rendering.
/// Images are frozen before they leave the worker thread, which is what makes
/// them safe to hand to the UI thread.
/// </remarks>
public sealed class SvgThumbnailService
{
    /// <summary>Limits parallel renders so a large folder cannot saturate the CPU.</summary>
    private readonly SemaphoreSlim _throttle = new(Math.Max(2, Environment.ProcessorCount / 2));

    /// <summary>
    /// Cache keyed on normalized path. The recorded last-write time invalidates an
    /// entry after an edit, while retaining only the latest render per file.
    /// </summary>
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<RenderKey, Lazy<Task<ImageSource?>>> _inFlight = new();

    public int CachedCount => _cache.Count;

    /// <summary>
    /// Renders the SVG, or returns <c>null</c> when the file cannot be rendered.
    /// A malformed SVG is a normal occurrence and must never crash the app.
    /// </summary>
    public async Task<ImageSource?> GetThumbnailAsync(string filePath, CancellationToken cancellationToken = default)
    {
        string normalizedPath;
        long lastWriteUtcTicks;
        try
        {
            normalizedPath = Path.GetFullPath(filePath);
            lastWriteUtcTicks = File.GetLastWriteTimeUtc(normalizedPath).Ticks;
        }
        catch (Exception)
        {
            return null;
        }

        if (_cache.TryGetValue(normalizedPath, out var cached) &&
            cached.LastWriteUtcTicks == lastWriteUtcTicks)
        {
            return cached.Image;
        }

        var key = new RenderKey(normalizedPath, lastWriteUtcTicks);
        var render = _inFlight.GetOrAdd(
            key,
            static (_, request) => new Lazy<Task<ImageSource?>>(
                () => request.Service.RenderAndCacheAsync(request.Key),
                LazyThreadSafetyMode.ExecutionAndPublication),
            (Service: this, Key: key));

        try
        {
            return await render.Value.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (render.IsValueCreated && render.Value.IsCompleted)
            {
                _inFlight.TryRemove(new KeyValuePair<RenderKey, Lazy<Task<ImageSource?>>>(key, render));
            }
        }
    }

    /// <summary>Clears the cache, e.g. after the user presses Refresh.</summary>
    public void ClearCache()
    {
        _cache.Clear();
        _inFlight.Clear();
    }

    private async Task<ImageSource?> RenderAndCacheAsync(RenderKey key)
    {
        await _throttle.WaitAsync().ConfigureAwait(false);
        try
        {
            var image = await Task.Run(() => Render(key.Path)).ConfigureAwait(false);
            if (image is not null)
            {
                _cache[key.Path] = new CacheEntry(key.LastWriteUtcTicks, image);
            }

            return image;
        }
        finally
        {
            _throttle.Release();
        }
    }

    private sealed record CacheEntry(long LastWriteUtcTicks, ImageSource Image);
    private sealed record RenderKey(string Path, long LastWriteUtcTicks);

    private static ImageSource? Render(string filePath)
    {
        var settings = new WpfDrawingSettings
        {
            IncludeRuntime = false,
            TextAsGeometry = true,
            OptimizePath = true
        };

        var reader = new FileSvgReader(settings);
        try
        {
            var drawing = reader.Read(filePath);
            if (drawing is null)
            {
                return null;
            }

            var image = new DrawingImage(drawing);

            // Freezing makes the image usable from the UI thread.
            if (image.CanFreeze)
            {
                image.Freeze();
            }

            return image;
        }
        catch (Exception)
        {
            // Broken or unsupported SVG: show the placeholder instead.
            return null;
        }
        finally
        {
            (reader as IDisposable)?.Dispose();
        }
    }
}
