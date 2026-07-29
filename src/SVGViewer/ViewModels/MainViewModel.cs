using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SVGViewer.Localization;
using SVGViewer.Models;
using SVGViewer.Services;

namespace SVGViewer.ViewModels;

/// <summary>
/// Drives the main window: drive selection, the folder tree, the structure
/// filter, the preview-size choice and the language choice.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private readonly SvgIndexService _indexService = new();
    private readonly SvgThumbnailService _thumbnailService = new();
    private readonly FileOpenService _fileOpenService;
    private readonly IUserNotifier _notifier;
    private readonly AppSettings _settings;

    private CancellationTokenSource? _scanCancellation;
    private CancellationTokenSource? _previewCancellation;
    private DateTime _lastMarkingRefreshUtc;
    private bool _isInitializing = true;

    // Remembered so the status line can be re-rendered after a language switch.
    private string _statusKey = "StatusSelectDrive";
    private object[] _statusArgs = Array.Empty<object>();

    public MainViewModel(
        SettingsService settingsService,
        AppSettings settings,
        FileOpenService? fileOpenService = null,
        IUserNotifier? notifier = null)
    {
        _settingsService = settingsService;
        _settings = settings;
        _fileOpenService = fileOpenService ?? new FileOpenService();
        _notifier = notifier ?? new MessageBoxNotifier();

        Drives = new ObservableCollection<DriveChoice>(LoadDrives());
        RootNodes = new ObservableCollection<DirectoryNodeViewModel>();
        SvgFiles = new ObservableCollection<SvgFileViewModel>();

        FilterChoices = new ObservableCollection<LocalizedChoice<FolderFilterMode>>
        {
            new(FolderFilterMode.All, "FilterAll"),
            new(FolderFilterMode.SvgOnly, "FilterSvgOnly")
        };

        PreviewSizeChoices = new ObservableCollection<LocalizedChoice<PreviewSize>>
        {
            new(PreviewSize.Large, "SizeLarge"),
            new(PreviewSize.Medium, "SizeMedium"),
            new(PreviewSize.Small, "SizeSmall"),
            new(PreviewSize.DetailsOnly, "SizeDetailsOnly")
        };

        // Restore persisted preferences without triggering a rebuild per change.
        // The tree filter always starts on "All" so startup shows structure
        // immediately; the "SVG only" choice applies for the session only.
        _selectedFilter = FilterChoices.First(c => c.Value == FolderFilterMode.All);
        _selectedPreviewSize = PreviewSizeChoices.First(c => c.Value == _settings.PreviewSize);
        _selectedDrive = Drives.FirstOrDefault(d => d.RootPath == _settings.LastDrive);

        // Language now lives in the Settings screen; re-render composed strings
        // (status line, file sizes/dates) whenever the culture changes there.
        Loc.CultureChanged += (_, _) => RefreshLocalizedText();

        SetStatus("StatusSelectDrive");
        _isInitializing = false;

        if (_selectedDrive is not null)
        {
            _ = RebuildTreeAsync();
        }
    }

    public ObservableCollection<DriveChoice> Drives { get; }

    public ObservableCollection<DirectoryNodeViewModel> RootNodes { get; }

    /// <summary>SVG files in the selected folder. Never contains other file types.</summary>
    public ObservableCollection<SvgFileViewModel> SvgFiles { get; }

    /// <summary>Edge length in pixels for a thumbnail, derived from the size choice.</summary>
    public double ThumbnailSize => SelectedPreviewSize.Value switch
    {
        PreviewSize.Large => 192,
        PreviewSize.Medium => 128,
        PreviewSize.Small => 72,
        _ => 0
    };

    /// <summary>True when the user chose "only details": a list instead of thumbnails.</summary>
    public bool IsDetailsMode => SelectedPreviewSize.Value == PreviewSize.DetailsOnly;

    /// <summary>
    /// The preview size as a plain enum, so the Explorer-style view toggle buttons
    /// can bind two-way. Setting it selects the matching localized choice.
    /// </summary>
    public PreviewSize PreviewSize
    {
        get => SelectedPreviewSize.Value;
        set
        {
            if (SelectedPreviewSize.Value != value)
            {
                SelectedPreviewSize = PreviewSizeChoices.First(c => c.Value == value);
            }
        }
    }

    /// <summary>True when the selected folder contains at least one SVG file.</summary>
    public bool HasSvgFiles => SvgFiles.Count > 0;

    /// <summary>True when a folder is selected but holds no SVG files.</summary>
    public bool ShowsEmptyFolderMessage => SelectedNode is not null && SvgFiles.Count == 0;

    /// <summary>True while no folder has been selected yet.</summary>
    public bool ShowsNoSelectionMessage => SelectedNode is null;

    public ObservableCollection<LocalizedChoice<FolderFilterMode>> FilterChoices { get; }

    public ObservableCollection<LocalizedChoice<PreviewSize>> PreviewSizeChoices { get; }

    [ObservableProperty]
    private DriveChoice? _selectedDrive;

    [ObservableProperty]
    private LocalizedChoice<FolderFilterMode> _selectedFilter;

    [ObservableProperty]
    private LocalizedChoice<PreviewSize> _selectedPreviewSize;

    [ObservableProperty]
    private DirectoryNodeViewModel? _selectedNode;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private string _selectedFolderInfo = string.Empty;

    partial void OnSelectedDriveChanged(DriveChoice? value)
    {
        if (_isInitializing)
        {
            return;
        }

        _settings.LastDrive = value?.RootPath;
        _settingsService.Save(_settings);
        _ = RebuildTreeAsync();
    }

    partial void OnSelectedFilterChanged(LocalizedChoice<FolderFilterMode> value)
    {
        if (_isInitializing)
        {
            return;
        }

        // The filter is intentionally not persisted: the viewer always starts in
        // "All" mode, and "SVG only" applies for the current session only.
        _ = RebuildTreeAsync();
    }

    partial void OnSelectedPreviewSizeChanged(LocalizedChoice<PreviewSize> value)
    {
        OnPropertyChanged(nameof(ThumbnailSize));
        OnPropertyChanged(nameof(IsDetailsMode));
        OnPropertyChanged(nameof(PreviewSize));

        if (_isInitializing)
        {
            return;
        }

        _settings.PreviewSize = value.Value;
        _settingsService.Save(_settings);

        // Details mode renders nothing, so thumbnails may still be missing when
        // the user switches back to a thumbnail size.
        if (!IsDetailsMode)
        {
            _ = LoadThumbnailsAsync(CancellationToken.None);
        }
    }

    partial void OnSelectedNodeChanged(DirectoryNodeViewModel? value)
    {
        UpdateSelectedFolderInfo();
        _ = LoadPreviewAsync(value);
    }

    /// <summary>Re-runs the current selection, e.g. after pressing Refresh.</summary>
    [RelayCommand]
    private Task Refresh()
    {
        // Drop cached renders so edited files show their new content.
        _thumbnailService.ClearCache();
        return RebuildTreeAsync();
    }

    /// <summary>Stops a running drive scan.</summary>
    [RelayCommand]
    private void CancelScan() => _scanCancellation?.Cancel();

    /// <summary>Opens the file in its associated application (e.g. Inkscape).</summary>
    [RelayCommand]
    private void OpenFile(SvgFileViewModel? file)
    {
        if (file is not null)
        {
            Report(_fileOpenService.OpenInAssociatedApp(file.FullPath));
        }
    }

    /// <summary>Shows the Windows "Open with..." dialog for the file.</summary>
    [RelayCommand]
    private void OpenFileWith(SvgFileViewModel? file)
    {
        if (file is not null)
        {
            Report(_fileOpenService.OpenWithDialog(file.FullPath));
        }
    }

    /// <summary>Reveals the file in Windows Explorer.</summary>
    [RelayCommand]
    private void ShowInExplorer(SvgFileViewModel? file)
    {
        if (file is not null)
        {
            Report(_fileOpenService.ShowInExplorer(file.FullPath));
        }
    }

    /// <summary>Turns a failed file action into a localized, user-visible message.</summary>
    private void Report(FileActionOutcome outcome)
    {
        switch (outcome)
        {
            case FileActionOutcome.NoAssociation:
                _notifier.Notify(Loc.Get("MsgNoAssociation"), Loc.Get("MsgNoAssociationTitle"));
                break;
            case FileActionOutcome.FileNotFound:
                _notifier.Notify(Loc.Get("MsgFileNotFound"), Loc.Get("AppTitle"));
                break;
            case FileActionOutcome.Failed:
                _notifier.Notify(Loc.Get("MsgOpenFailed"), Loc.Get("AppTitle"));
                break;
            case FileActionOutcome.Opened:
            default:
                break;
        }
    }

    private static IEnumerable<DriveChoice> LoadDrives()
    {
        DriveInfo[] drives;
        try
        {
            drives = DriveInfo.GetDrives();
        }
        catch (Exception)
        {
            return Array.Empty<DriveChoice>();
        }

        var result = new List<DriveChoice>();

        foreach (var drive in drives)
        {
            try
            {
                if (!drive.IsReady)
                {
                    continue;
                }

                var label = string.IsNullOrWhiteSpace(drive.VolumeLabel)
                    ? drive.DriveType.ToString()
                    : drive.VolumeLabel;

                result.Add(new DriveChoice(drive.RootDirectory.FullName, $"{drive.Name} ({label})"));
            }
            catch (Exception)
            {
                // Unreadable drive (e.g. disconnected network share): skip it.
            }
        }

        return result;
    }

    /// <summary>
    /// Rebuilds the tree for the selected drive. In "SVG only" mode this first
    /// walks the whole drive on a background thread to find the relevant folders.
    /// </summary>
    private async Task RebuildTreeAsync()
    {
        _scanCancellation?.Cancel();
        RootNodes.Clear();
        SelectedNode = null;

        var drive = SelectedDrive;
        if (drive is null)
        {
            SetStatus("StatusSelectDrive");
            return;
        }

        var filterMode = SelectedFilter.Value;

        if (filterMode == FolderFilterMode.All)
        {
            // Show the tree immediately (lazy). A background scan then lights up
            // folders that contain SVGs (blue + count) or that lead to them
            // (blue, no count), so collapsed branches reveal where SVGs live.
            var index = new SvgFolderIndex();
            var root = new DirectoryNodeViewModel(drive.RootPath, drive.DisplayName, filterMode, index);
            RootNodes.Add(root);
            root.ExpandAndLoad();

            _scanCancellation = new CancellationTokenSource();
            var scanToken = _scanCancellation.Token;

            IsScanning = true;
            SetStatus("StatusScanning");
            _lastMarkingRefreshUtc = DateTime.UtcNow;

            var scanProgress = new Progress<ScanProgress>(p =>
            {
                if (scanToken.IsCancellationRequested)
                {
                    return;
                }

                SetStatus("StatusFoldersScanned", p.FoldersScanned, p.FoldersWithSvg);

                // Repaint markings a few times per second at most while scanning.
                if ((DateTime.UtcNow - _lastMarkingRefreshUtc).TotalMilliseconds >= 400)
                {
                    _lastMarkingRefreshUtc = DateTime.UtcNow;
                    RefreshMarkings();
                }
            });

            try
            {
                var built = await _indexService.BuildIndexAsync(drive.RootPath, index, scanProgress, scanToken);

                if (scanToken.IsCancellationRequested)
                {
                    return; // A newer drive/filter change owns the UI now.
                }

                RefreshMarkings();

                SetStatus(built.FoldersWithSvg.Count > 0 ? "StatusFoldersWithSvg" : "StatusNoSvgFound",
                          built.FoldersWithSvg.Count);
            }
            catch (OperationCanceledException)
            {
                // Superseded by a newer rebuild.
            }
            finally
            {
                if (!scanToken.IsCancellationRequested)
                {
                    IsScanning = false;
                }
            }

            return;
        }

        // "SVG only": show a compact tree of just the SVG folders, built up
        // progressively as the background scan discovers them (additive, no
        // flicker). The drive root is shown at once so there is never a blank pane.
        var svgIndex = new SvgFolderIndex();
        var svgRoot = DirectoryNodeViewModel.CreateExplicit(drive.RootPath, drive.DisplayName, svgIndex);
        RootNodes.Add(svgRoot);
        svgRoot.IsExpanded = true;

        _scanCancellation = new CancellationTokenSource();
        var token = _scanCancellation.Token;

        IsScanning = true;
        SetStatus("StatusScanning");
        _lastMarkingRefreshUtc = DateTime.UtcNow;

        var inserted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var progress = new Progress<ScanProgress>(p =>
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            SetStatus("StatusFoldersScanned", p.FoldersScanned, p.FoldersWithSvg);

            if ((DateTime.UtcNow - _lastMarkingRefreshUtc).TotalMilliseconds >= 400)
            {
                _lastMarkingRefreshUtc = DateTime.UtcNow;
                SvgOnlyTreeBuilder.Sync(svgRoot, drive.RootPath, svgIndex, inserted);
            }
        });

        try
        {
            var built = await _indexService.BuildIndexAsync(drive.RootPath, svgIndex, progress, token);

            if (token.IsCancellationRequested)
            {
                return; // A newer drive/filter change owns the UI now.
            }

            SvgOnlyTreeBuilder.Sync(svgRoot, drive.RootPath, svgIndex, inserted);

            if (built.FoldersWithSvg.Count == 0)
            {
                RootNodes.Clear();
                SetStatus("StatusNoSvgFound");
            }
            else
            {
                SetStatus("StatusFoldersWithSvg", built.FoldersWithSvg.Count);
            }
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer rebuild.
        }
        finally
        {
            if (!token.IsCancellationRequested)
            {
                IsScanning = false;
            }
        }
    }

    /// <summary>Re-reads the scan index into the currently realized tree nodes.</summary>
    private void RefreshMarkings()
    {
        foreach (var node in RootNodes)
        {
            node.RefreshMarking();
        }
    }

    /// <summary>
    /// Fills the preview pane for the selected folder. Only SVG files are listed;
    /// every other file type is ignored entirely.
    /// </summary>
    private async Task LoadPreviewAsync(DirectoryNodeViewModel? node)
    {
        _previewCancellation?.Cancel();
        SvgFiles.Clear();
        NotifyPreviewStateChanged();

        if (node is null || node.IsPlaceholder || string.IsNullOrEmpty(node.FullPath))
        {
            return;
        }

        _previewCancellation = new CancellationTokenSource();
        var token = _previewCancellation.Token;

        // File details are cheap, so the list is shown before any rendering starts.
        foreach (var path in DirectoryScanner.GetSvgFiles(node.FullPath))
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            SvgFiles.Add(new SvgFileViewModel(new FileInfo(path), _thumbnailService));
        }

        NotifyPreviewStateChanged();

        if (!IsDetailsMode)
        {
            await LoadThumbnailsAsync(token);
        }
    }

    /// <summary>Renders thumbnails for the files currently listed.</summary>
    private async Task LoadThumbnailsAsync(CancellationToken cancellationToken)
    {
        // Snapshot: the collection can change while rendering.
        var files = SvgFiles.ToList();

        foreach (var file in files)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            await file.LoadThumbnailAsync(cancellationToken);
        }
    }

    private void NotifyPreviewStateChanged()
    {
        OnPropertyChanged(nameof(HasSvgFiles));
        OnPropertyChanged(nameof(ShowsEmptyFolderMessage));
        OnPropertyChanged(nameof(ShowsNoSelectionMessage));
    }

    private void UpdateSelectedFolderInfo()
    {
        SelectedFolderInfo = SelectedNode is null
            ? string.Empty
            : Loc.Format("LabelSvgFilesInFolder", SelectedNode.SvgFileCount);
    }

    private void SetStatus(string resourceKey, params object[] args)
    {
        _statusKey = resourceKey;
        _statusArgs = args;
        StatusText = args.Length == 0 ? Loc.Get(resourceKey) : Loc.Format(resourceKey, args);
    }

    private void RefreshLocalizedText()
    {
        SetStatus(_statusKey, _statusArgs);
        UpdateSelectedFolderInfo();

        // File sizes and dates are culture-dependent too.
        foreach (var file in SvgFiles)
        {
            file.RefreshLocalizedText();
        }
    }
}
