using System.Globalization;
using System.IO;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using SVGViewer.Localization;
using SVGViewer.Services;

namespace SVGViewer.ViewModels;

/// <summary>
/// One SVG file in the preview pane. File details are read immediately (cheap),
/// while the thumbnail is rendered afterwards so the list appears instantly.
/// </summary>
public partial class SvgFileViewModel : ObservableObject
{
    private readonly SvgThumbnailService _thumbnailService;

    public SvgFileViewModel(FileInfo file, SvgThumbnailService thumbnailService)
    {
        _thumbnailService = thumbnailService;

        FullPath = file.FullName;
        FileName = file.Name;
        SizeInBytes = SafeLength(file);
        LastModified = SafeLastWrite(file);
    }

    public string FullPath { get; }

    public string FileName { get; }

    public long SizeInBytes { get; }

    public DateTime LastModified { get; }

    /// <summary>Human-readable file size, e.g. "12,4 kB".</summary>
    public string SizeDisplay => FormatSize(SizeInBytes);

    /// <summary>Modification date in the active culture's short format.</summary>
    public string LastModifiedDisplay =>
        LastModified.ToString("g", CultureInfo.CurrentCulture);

    public string ToolTipText => $"{FullPath}\n{SizeDisplay} · {LastModifiedDisplay}";

    [ObservableProperty]
    private ImageSource? _thumbnail;

    [ObservableProperty]
    private bool _isLoadingThumbnail;

    /// <summary>True when the file could not be rendered (malformed SVG).</summary>
    [ObservableProperty]
    private bool _renderFailed;

    /// <summary>
    /// Renders the thumbnail once. Safe to call repeatedly; later calls are no-ops.
    /// </summary>
    public async Task LoadThumbnailAsync(CancellationToken cancellationToken = default)
    {
        if (Thumbnail is not null || RenderFailed)
        {
            return;
        }

        IsLoadingThumbnail = true;
        try
        {
            var image = await _thumbnailService.GetThumbnailAsync(FullPath, cancellationToken)
                                               .ConfigureAwait(true);

            if (image is null)
            {
                RenderFailed = true;
            }
            else
            {
                Thumbnail = image;
            }
        }
        catch (OperationCanceledException)
        {
            // Folder changed while loading: leave the item without a thumbnail.
        }
        finally
        {
            IsLoadingThumbnail = false;
        }
    }

    /// <summary>Re-renders localized text after a language switch.</summary>
    public void RefreshLocalizedText()
    {
        OnPropertyChanged(nameof(SizeDisplay));
        OnPropertyChanged(nameof(LastModifiedDisplay));
        OnPropertyChanged(nameof(ToolTipText));
    }

    private static long SafeLength(FileInfo file)
    {
        try
        {
            return file.Length;
        }
        catch (Exception)
        {
            return 0;
        }
    }

    private static DateTime SafeLastWrite(FileInfo file)
    {
        try
        {
            return file.LastWriteTime;
        }
        catch (Exception)
        {
            return DateTime.MinValue;
        }
    }

    /// <summary>Formats bytes as B / kB / MB using the active culture.</summary>
    public static string FormatSize(long bytes)
    {
        if (bytes < 1024)
        {
            return Loc.Format("SizeBytes", bytes);
        }

        if (bytes < 1024 * 1024)
        {
            return Loc.Format("SizeKilobytes", (bytes / 1024.0).ToString("0.#", CultureInfo.CurrentCulture));
        }

        return Loc.Format("SizeMegabytes", (bytes / (1024.0 * 1024.0)).ToString("0.#", CultureInfo.CurrentCulture));
    }
}
