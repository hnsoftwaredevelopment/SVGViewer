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

        LanguageChoices = new ObservableCollection<LanguageChoice>
        {
            new("nl", "Nederlands"),
            new("en", "English"),
            new("de", "Deutsch")
        };

        // Restore persisted preferences without triggering a rebuild per change.
        _selectedFilter = FilterChoices.First(c => c.Value == _settings.FilterMode);
        _selectedPreviewSize = PreviewSizeChoices.First(c => c.Value == _settings.PreviewSize);
        _selectedLanguage = LanguageChoices.FirstOrDefault(l => l.CultureName == _settings.Language)
                            ?? LanguageChoices[0];
        _selectedDrive = Drives.FirstOrDefault(d => d.RootPath == _settings.LastDrive);

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

    /// <summary>True when the selected folder contains at least one SVG file.</summary>
    public bool HasSvgFiles => SvgFiles.Count > 0;

    /// <summary>True when a folder is selected but holds no SVG files.</summary>
    public bool ShowsEmptyFolderMessage => SelectedNode is not null && SvgFiles.Count == 0;

    /// <summary>True while no folder has been selected yet.</summary>
    public bool ShowsNoSelectionMessage => SelectedNode is null;

    public ObservableCollection<LocalizedChoice<FolderFilterMode>> FilterChoices { get; }

    public ObservableCollection<LocalizedChoice<PreviewSize>> PreviewSizeChoices { get; }

    public ObservableCollection<LanguageChoice> LanguageChoices { get; }

    [ObservableProperty]
    private DriveChoice? _selectedDrive;

    [ObservableProperty]
    private LocalizedChoice<FolderFilterMode> _selectedFilter;

    [ObservableProperty]
    private LocalizedChoice<PreviewSize> _selectedPreviewSize;

    [ObservableProperty]
    private LanguageChoice _selectedLanguage;

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

        _settings.FilterMode = value.Value;
        _settingsService.Save(_settings);
        _ = RebuildTreeAsync();
    }

    partial void OnSelectedPreviewSizeChanged(LocalizedChoice<PreviewSize> value)
    {
        OnPropertyChanged(nameof(ThumbnailSize));
        OnPropertyChanged(nameof(IsDetailsMode));

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

    partial void OnSelectedLanguageChanged(LanguageChoice value)
    {
        if (_isInitializing)
        {
            return;
        }

        Loc.SetCulture(value.CultureName);
        _settings.Language = value.CultureName;
        _settingsService.Save(_settings);

        // Re-render texts that were composed in the previous language.
        RefreshLocalizedText();
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
            var root = new DirectoryNodeViewModel(drive.RootPath, drive.DisplayName, filterMode, null);
            RootNodes.Add(root);
            root.ExpandAndLoad();
            SetStatus("StatusReady");
            return;
        }

        // Filtered mode: scan the drive first.
        _scanCancellation = new CancellationTokenSource();
        var token = _scanCancellation.Token;

        IsScanning = true;
        SetStatus("StatusScanning");

        var progress = new Progress<ScanProgress>(p =>
            SetStatus("StatusFoldersScanned", p.FoldersScanned, p.FoldersWithSvg));

        try
        {
            var index = await _indexService.BuildIndexAsync(drive.RootPath, progress, token);

            if (index.WasCancelled)
            {
                SetStatus("StatusScanCancelled");
                return;
            }

            if (index.FoldersWithSvg.Count == 0)
            {
                SetStatus("StatusNoSvgFound");
                return;
            }

            var root = new DirectoryNodeViewModel(drive.RootPath, drive.DisplayName, filterMode, index);
            RootNodes.Add(root);
            root.ExpandAndLoad();

            SetStatus("StatusFoldersWithSvg", index.FoldersWithSvg.Count);
        }
        catch (OperationCanceledException)
        {
            SetStatus("StatusScanCancelled");
        }
        finally
        {
            IsScanning = false;
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
