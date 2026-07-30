using System.Windows;
using System.Windows.Media;
using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;

namespace SVGViewer.Services;

/// <summary>
/// Loads an SVG that is embedded as a WPF resource (e.g. "/Assets/appicon.svg")
/// into a frozen vector <see cref="ImageSource"/>. Returns <c>null</c> when the
/// resource is missing or cannot be rendered, so callers can degrade gracefully.
/// </summary>
public static class SvgResourceImage
{
    public static ImageSource? Load(string relativeResourceUri)
    {
        try
        {
            var info = Application.GetResourceStream(new Uri(relativeResourceUri, UriKind.Relative));
            if (info?.Stream is null)
            {
                return null;
            }

            var settings = new WpfDrawingSettings
            {
                IncludeRuntime = false,
                TextAsGeometry = true,
                OptimizePath = true
            };

            var reader = new FileSvgReader(settings);
            try
            {
                using var stream = info.Stream;
                var drawing = reader.Read(stream);
                if (drawing is null)
                {
                    return null;
                }

                var image = new DrawingImage(drawing);
                if (image.CanFreeze)
                {
                    image.Freeze();
                }

                return image;
            }
            finally
            {
                (reader as IDisposable)?.Dispose();
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"Could not load SVG resource '{relativeResourceUri}'.", ex);
            return null;
        }
    }
}
