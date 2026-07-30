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

    // One scan per drive, shared by both views. Switching the filter re-projects
    // this index instead of starting a new scan; a running scan keeps feeding
    // whichever view is active.
    private SvgFolderIndex _index = new();
    private bool _scanComplete;
    private DirectoryNodeViewModel? _svgOnlyRoot;
    private HashSet<string> _svgOnlyInserted = new(StringComparer.OrdinalIgnoreCase);

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
            _ = StartScanAsync();
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
        _ = StartScanAsync();
    }

    partial void OnSelectedFilterChanged(LocalizedChoice<FolderFilterMode> value)
    {
        if (_isInitializing)
        {
            return;
        }

        // Switching the filter never rescans: it re-projects the shared index for
        // the current drive. A scan that is still running keeps filling both views.
        // (The filter is intentionally not persisted: the viewer always starts in
        // "All" mode, and "SVG only" applies for the current session only.)
        ProjectView();

        if (_scanComplete)
        {
            ApplyFinalStatus();
        }
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
        return StartScanAsync();
    }

    /// <summary>
    /// Stops the running drive scan. Whatever was already found stays on screen and
    /// switching the filter will reuse it (no rescan) until the drive is changed.
    /// </summary>
    [RelayCommand]
    private void CancelScan()
    {
        _scanCancellation?.Cancel();
        IsScanning = false;
        _scanComplete = true;
        ApplyFinalStatus();
    }

    /// <summary>Opens the file in its associated application (e.g. Inkscape).</summary>
    [RelayCommand]
    private void OpenFile(SvgFileViewModel? file)
    {
        if (file is not null)
        {
            Report(_fileOpenService.OpenInAssociatedApp(file.FullPath), file.FullPath);
        }
    }

    /// <summary>Shows the Windows "Open with..." dialog for the file.</summary>
    [RelayCommand]
    private void OpenFileWith(SvgFileViewModel? file)
    {
        if (file is not null)
        {
            Report(_fileOpenService.OpenWithDialog(file.FullPath), file.FullPath);
        }
    }

    /// <summary>Reveals the file in Windows Explorer.</summary>
    [RelayCommand]
    private void ShowInExplorer(SvgFileViewModel? file)
    {
        if (file is not null)
        {
            Report(_fileOpenService.ShowInExplorer(file.FullPath), file.FullPath);
        }
    }

    /// <summary>Turns a failed file action into a localized, user-visible message, and logs it.</summary>
    private void Report(FileActionOutcome outcome, string path)
    {
        switch (outcome)
        {
            case FileActionOutcome.NoAssociation:
                Logger.Warn($"No associated application for '{path}'.");
                _notifier.Notify(Loc.Get("MsgNoAssociation"), Loc.Get("MsgNoAssociationTitle"));
                break;
            case FileActionOutcome.FileNotFound:
                Logger.Warn($"File no longer exists: '{path}'.");
                _notifier.Notify(Loc.Get("MsgFileNotFound"), Loc.Get("AppTitle"));
                break;
            case FileActionOutcome.Failed:
                Logger.Error($"File action failed for '{path}'.");
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
    /// Starts (or restarts) the single background scan for the selected drive and
    /// shows the current view immediately. The scan fills one shared index that
    /// both views read from, so switching the filter never triggers a new scan.
    /// </summary>
    private async Task StartScanAsync()
    {
        _scanCancellation?.Cancel();
        SelectedNode = null;

        var drive = SelectedDrive;

        // Fresh shared index for this drive.
        _index = new SvgFolderIndex();
        _scanComplete = false;

        if (drive is null)
        {
            RootNodes.Clear();
            IsScanning = false;
            SetStatus("StatusSelectDrive");
            return;
        }

        // Project the (still empty) index into the current view right away.
        ProjectView();

        _scanCancellation = new CancellationTokenSource();
        var token = _scanCancellation.Token;

        IsScanning = true;
        SetStatus("StatusScanning");
        _lastMarkingRefreshUtc = DateTime.UtcNow;

        var progress = new Progress<ScanProgress>(p =>
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            SetStatus("StatusFoldersScanned", p.FoldersScanned, p.FoldersWithSvg);

            // Repaint the active view a few times per second at most while scanning.
            if ((DateTime.UtcNow - _lastMarkingRefreshUtc).TotalMilliseconds >= 400)
            {
                _lastMarkingRefreshUtc = DateTime.UtcNow;
                RefreshActiveView();
            }
        });

        try
        {
            await _indexService.BuildIndexAsync(drive.RootPath, _index, progress, token);

            if (token.IsCancellationRequested)
            {
                return; // A newer drive change (or cancel) owns the UI now.
            }

            _scanComplete = true;
            RefreshActiveView();

            if (SelectedFilter.Value == FolderFilterMode.SvgOnly && _index.FoldersWithSvg.Count == 0)
            {
                RootNodes.Clear();
            }

            ApplyFinalStatus();
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer scan.
        }
        finally
        {
            if (!token.IsCancellationRequested)
            {
                IsScanning = false;
            }
        }
    }

    /// <summary>
    /// Rebuilds <see cref="RootNodes"/> for the current filter from the shared
    /// index, without scanning. Called on start-up, on every filter switch, and
    /// once more when a scan completes.
    /// </summary>
    private void ProjectView()
    {
        RootNodes.Clear();
        _svgOnlyRoot = null;

        var drive = SelectedDrive;
        if (drive is null)
        {
            return;
        }

        if (SelectedFilter.Value == FolderFilterMode.All)
        {
            // Lazy tree, usable immediately; markings come from the shared index.
            var root = new DirectoryNodeViewModel(drive.RootPath, drive.DisplayName, FolderFilterMode.All, _index);
            RootNodes.Add(root);
            root.ExpandAndLoad();
            RefreshMarkings();
            return;
        }

        // "SVG only": a compact tree of just the SVG folders, built additively
        // (no flicker). The drive root shows at once so the pane is never blank.
        _svgOnlyInserted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _svgOnlyRoot = DirectoryNodeViewModel.CreateExplicit(drive.RootPath, drive.DisplayName, _index);
        RootNodes.Add(_svgOnlyRoot);
        _svgOnlyRoot.IsExpanded = true;
        SvgOnlyTreeBuilder.Sync(_svgOnlyRoot, drive.RootPath, _index, _svgOnlyInserted);

        // If the scan already finished and found nothing, show the empty state.
        if (_scanComplete && _index.FoldersWithSvg.Count == 0)
        {
            RootNodes.Clear();
        }
    }

    /// <summary>Applies the latest scan progress to whichever view is showing.</summary>
    private void RefreshActiveView()
    {
        if (SelectedFilter.Value == FolderFilterMode.All)
        {
            RefreshMarkings();
            return;
        }

        var drive = SelectedDrive;
        if (_svgOnlyRoot is not null && drive is not null)
        {
            SvgOnlyTreeBuilder.Sync(_svgOnlyRoot, drive.RootPath, _index, _svgOnlyInserted);
        }
    }

    /// <summary>Sets the status line based on the finished (or cancelled) scan.</summary>
    private void ApplyFinalStatus()
    {
        var count = _index.FoldersWithSvg.Count;
        if (count > 0)
        {
            SetStatus("StatusFoldersWithSvg", count);
        }
        else
        {
            SetStatus("StatusNoSvgFound");
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
