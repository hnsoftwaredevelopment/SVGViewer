using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SVGViewer.Views;

/// <summary>
/// A modal, in-window overlay that shows one SVG large and lets the user zoom
/// (mouse wheel or buttons) and pan (drag). Because the source is a vector
/// <see cref="DrawingImage"/>, zooming stays perfectly crisp at any level.
/// </summary>
public partial class SvgZoomViewer : UserControl
{
    private const double MinScale = 0.1;
    private const double MaxScale = 40.0;
    private const double ZoomStep = 1.2;

    private Point _lastMousePosition;
    private bool _isPanning;

    public SvgZoomViewer()
    {
        InitializeComponent();
    }

    /// <summary>True while the overlay is showing an image.</summary>
    public bool IsOpen => Visibility == Visibility.Visible;

    /// <summary>Shows the overlay for the given image and resets the view to "fit".</summary>
    public void Show(ImageSource image, string fileName)
    {
        PreviewImage.Source = image;
        FileNameText.Text = fileName;

        Visibility = Visibility.Visible;

        // Reset once the control has its final size, so "fit" is accurate.
        Dispatcher.BeginInvoke(new Action(ResetToFit), System.Windows.Threading.DispatcherPriority.Loaded);

        Focus();
    }

    /// <summary>Closes the preview by closing its host window.</summary>
    public void Close() => Window.GetWindow(this)?.Close();

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (!IsOpen)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Escape:
                Close();
                e.Handled = true;
                break;
            case Key.Add:
            case Key.OemPlus:
                ZoomAtCenter(ZoomStep);
                e.Handled = true;
                break;
            case Key.Subtract:
            case Key.OemMinus:
                ZoomAtCenter(1 / ZoomStep);
                e.Handled = true;
                break;
            case Key.D0:
            case Key.NumPad0:
                ResetToActualSize();
                e.Handled = true;
                break;
        }

        base.OnKeyDown(e);
    }

    private void Viewport_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        var factor = e.Delta > 0 ? ZoomStep : 1 / ZoomStep;
        ZoomAt(e.GetPosition(PreviewImage), factor);
        e.Handled = true;
    }

    private void Viewport_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isPanning = true;
        _lastMousePosition = e.GetPosition(ViewportBorder);
        ViewportBorder.CaptureMouse();
        ViewportBorder.Cursor = Cursors.SizeAll;
    }

    private void Viewport_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isPanning = false;
        ViewportBorder.ReleaseMouseCapture();
        ViewportBorder.Cursor = Cursors.Arrow;
    }

    private void Viewport_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isPanning)
        {
            return;
        }

        var position = e.GetPosition(ViewportBorder);
        var delta = position - _lastMousePosition;
        _lastMousePosition = position;

        var matrix = ImageTransform.Matrix;
        matrix.Translate(delta.X, delta.Y);
        ImageTransform.Matrix = matrix;
    }

    private void ZoomIn_Click(object sender, RoutedEventArgs e) => ZoomAtCenter(ZoomStep);

    private void ZoomOut_Click(object sender, RoutedEventArgs e) => ZoomAtCenter(1 / ZoomStep);

    private void ZoomReset_Click(object sender, RoutedEventArgs e) => ResetToActualSize();

    private void ZoomFit_Click(object sender, RoutedEventArgs e) => ResetToFit();

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    /// <summary>Zooms around a point expressed in the image's own coordinates.</summary>
    private void ZoomAt(Point center, double factor)
    {
        var matrix = ImageTransform.Matrix;

        var newScale = matrix.M11 * factor;
        if (newScale < MinScale || newScale > MaxScale)
        {
            return;
        }

        matrix.ScaleAt(factor, factor, center.X, center.Y);
        ImageTransform.Matrix = matrix;
        UpdateZoomLabel();
    }

    private void ZoomAtCenter(double factor)
    {
        var center = new Point(PreviewImage.ActualWidth / 2, PreviewImage.ActualHeight / 2);
        ZoomAt(center, factor);
    }

    /// <summary>Fit mode: the base layout already fits, so identity is "fit".</summary>
    private void ResetToFit()
    {
        ImageTransform.Matrix = Matrix.Identity;
        UpdateZoomLabel();
    }

    /// <summary>
    /// Actual size: scale so the rendered image matches its intrinsic pixel size,
    /// centred in the viewport.
    /// </summary>
    private void ResetToActualSize()
    {
        if (PreviewImage.Source is not { } source ||
            PreviewImage.ActualWidth <= 0 ||
            source.Width <= 0)
        {
            ResetToFit();
            return;
        }

        // The image is laid out "Uniform", so work out the current fit scale and
        // invert it to reach 1:1.
        var fitScale = PreviewImage.ActualWidth / source.Width;
        var target = fitScale > 0 ? 1 / fitScale : 1;

        var matrix = Matrix.Identity;
        matrix.ScaleAt(target, target,
            PreviewImage.ActualWidth / 2,
            PreviewImage.ActualHeight / 2);
        ImageTransform.Matrix = matrix;
        UpdateZoomLabel();
    }

    private void UpdateZoomLabel()
    {
        var effective = ImageTransform.Matrix.M11;

        // Show zoom relative to actual size when the intrinsic size is known.
        if (PreviewImage.Source is { } source && source.Width > 0 && PreviewImage.ActualWidth > 0)
        {
            var fitScale = PreviewImage.ActualWidth / source.Width;
            effective = ImageTransform.Matrix.M11 * fitScale;
        }

        ZoomLevelText.Text = $"{effective * 100:0}%";
    }
}
