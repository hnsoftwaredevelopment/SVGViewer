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
    private readonly AppSettings _settings;

    private CancellationTokenSource? _scanCancellation;
    private bool _isInitializing = true;

    // Remembered so the status line can be re-rendered after a language switch.
    private string _statusKey = "StatusSelectDrive";
    private object[] _statusArgs = Array.Empty<object>();

    public MainViewModel(SettingsService settingsService, AppSettings settings)
    {
        _settingsService = settingsService;
        _settings = settings;

        Drives = new ObservableCollection<DriveChoice>(LoadDrives());
        RootNodes = new ObservableCollection<DirectoryNodeViewModel>();

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
        if (_isInitializing)
        {
            return;
        }

        _settings.PreviewSize = value.Value;
        _settingsService.Save(_settings);
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
    }

    /// <summary>Re-runs the current selection, e.g. after pressing Refresh.</summary>
    [RelayCommand]
    private Task Refresh() => RebuildTreeAsync();

    /// <summary>Stops a running drive scan.</summary>
    [RelayCommand]
    private void CancelScan() => _scanCancellation?.Cancel();

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
    }
}
