using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace SVGViewer.Services;

/// <summary>
/// Persists completed SVG scan indexes between application sessions. The cache
/// is deliberately best-effort: unreadable or obsolete data is simply ignored
/// and the normal scan starts instead.
/// </summary>
public sealed class ScanIndexCacheService
{
    private const int MaxCachedScopes = 8;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _cachePath;

    /// <summary>Uses %AppData%\SVGViewer\scan-cache.json.</summary>
    public ScanIndexCacheService()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SVGViewer",
            "scan-cache.json"))
    {
    }

    /// <summary>Uses an explicit path, primarily for isolated tests.</summary>
    public ScanIndexCacheService(string cachePath)
    {
        _cachePath = cachePath;
    }

    public string CachePath => _cachePath;

    /// <summary>Returns a completed cached index for this location, when valid.</summary>
    public SvgFolderIndex? Load(string rootPath)
    {
        try
        {
            var normalizedRoot = DirectoryScanner.NormalizeFolderPath(rootPath);
            var document = ReadDocument();
            var entry = document.Entries.FirstOrDefault(candidate =>
                string.Equals(candidate.RootPath, normalizedRoot, StringComparison.OrdinalIgnoreCase));

            if (entry is null || !MatchesCurrentVolume(entry, normalizedRoot))
            {
                return null;
            }

            return SvgFolderIndex.FromCachedData(
                entry.SvgFolders.Select(folder => new KeyValuePair<string, int>(folder.Path, folder.Count)),
                entry.RelevantFolders,
                entry.TotalFoldersScanned);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>Stores a completed scan. A failed cache write never affects the viewer.</summary>
    public void Save(string rootPath, SvgFolderIndex index)
    {
        try
        {
            var normalizedRoot = DirectoryScanner.NormalizeFolderPath(rootPath);
            var document = ReadDocument();
            document.Entries.RemoveAll(entry =>
                string.Equals(entry.RootPath, normalizedRoot, StringComparison.OrdinalIgnoreCase));

            document.Entries.Add(new CachedScanEntry
            {
                RootPath = normalizedRoot,
                VolumeId = GetVolumeId(normalizedRoot),
                SavedUtc = DateTime.UtcNow,
                TotalFoldersScanned = index.TotalFoldersScanned,
                SvgFolders = index.FoldersWithSvg
                    .Select(path => new CachedSvgFolder
                    {
                        Path = path,
                        Count = index.GetSvgCount(path)
                    })
                    .ToList(),
                RelevantFolders = index.RelevantFolders.ToList()
            });

            document.Entries = document.Entries
                .OrderByDescending(entry => entry.SavedUtc)
                .Take(MaxCachedScopes)
                .ToList();

            var folder = Path.GetDirectoryName(_cachePath);
            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var temporaryPath = _cachePath + ".tmp";
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(document, JsonOptions));
            File.Move(temporaryPath, _cachePath, overwrite: true);
        }
        catch (Exception)
        {
            // The in-memory scan remains useful when persistence is unavailable.
        }
    }

    private ScanCacheDocument ReadDocument()
    {
        if (!File.Exists(_cachePath))
        {
            return new ScanCacheDocument();
        }

        var json = File.ReadAllText(_cachePath);
        return JsonSerializer.Deserialize<ScanCacheDocument>(json, JsonOptions) ?? new ScanCacheDocument();
    }

    private static bool MatchesCurrentVolume(CachedScanEntry entry, string rootPath)
    {
        if (string.IsNullOrWhiteSpace(entry.VolumeId))
        {
            return true;
        }

        var currentVolumeId = GetVolumeId(rootPath);
        return string.IsNullOrWhiteSpace(currentVolumeId) ||
               string.Equals(entry.VolumeId, currentVolumeId, StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetVolumeId(string path)
    {
        try
        {
            var root = Path.GetPathRoot(path);
            if (string.IsNullOrWhiteSpace(root))
            {
                return null;
            }

            // GetVolumeInformation requires a trailing separator for UNC share
            // roots, whereas local drive roots already contain one.
            if (!Path.EndsInDirectorySeparator(root))
            {
                root += Path.DirectorySeparatorChar;
            }

            if (
                !GetVolumeInformation(root, null, 0, out var serial, out _, out _, null, 0))
            {
                return null;
            }

            return serial.ToString("X8");
        }
        catch (Exception)
        {
            return null;
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool GetVolumeInformation(
        string rootPathName,
        StringBuilder? volumeNameBuffer,
        uint volumeNameSize,
        out uint volumeSerialNumber,
        out uint maximumComponentLength,
        out uint fileSystemFlags,
        StringBuilder? fileSystemNameBuffer,
        uint fileSystemNameSize);

    private sealed class ScanCacheDocument
    {
        public List<CachedScanEntry> Entries { get; set; } = [];
    }

    private sealed class CachedScanEntry
    {
        public string RootPath { get; set; } = string.Empty;
        public string? VolumeId { get; set; }
        public DateTime SavedUtc { get; set; }
        public int TotalFoldersScanned { get; set; }
        public List<CachedSvgFolder> SvgFolders { get; set; } = [];
        public List<string> RelevantFolders { get; set; } = [];
    }

    private sealed class CachedSvgFolder
    {
        public string Path { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
