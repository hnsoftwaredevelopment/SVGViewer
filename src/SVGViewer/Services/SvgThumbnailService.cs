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

    /// <summary>Cache keyed on path plus last-write time, so edits are picked up.</summary>
    private readonly ConcurrentDictionary<string, ImageSource> _cache = new(StringComparer.OrdinalIgnoreCase);

    public int CachedCount => _cache.Count;

    /// <summary>
    /// Renders the SVG, or returns <c>null</c> when the file cannot be rendered.
    /// A malformed SVG is a normal occurrence and must never crash the app.
    /// </summary>
    public async Task<ImageSource?> GetThumbnailAsync(string filePath, CancellationToken cancellationToken = default)
    {
        string cacheKey;
        try
        {
            cacheKey = $"{filePath}|{File.GetLastWriteTimeUtc(filePath).Ticks}";
        }
        catch (Exception)
        {
            return null;
        }

        if (_cache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        await _throttle.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var image = await Task.Run(() => Render(filePath), cancellationToken).ConfigureAwait(false);

            if (image is not null)
            {
                _cache[cacheKey] = image;
            }

            return image;
        }
        finally
        {
            _throttle.Release();
        }
    }

    /// <summary>Clears the cache, e.g. after the user presses Refresh.</summary>
    public void ClearCache() => _cache.Clear();

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
