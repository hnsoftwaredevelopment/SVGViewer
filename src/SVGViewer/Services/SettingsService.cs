using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using SVGViewer.Models;

namespace SVGViewer.Services;

/// <summary>
/// Loads and saves <see cref="AppSettings"/> as JSON under
/// %AppData%\SVGViewer\settings.json. Failures are non-fatal: the application
/// falls back to defaults rather than refusing to start.
/// </summary>
public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _settingsPath;

    public SettingsService()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SVGViewer");

        _settingsPath = Path.Combine(folder, "settings.json");
    }

    /// <summary>Reads the settings file, or returns defaults when unavailable.</summary>
    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                return new AppSettings();
            }

            var json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch (Exception)
        {
            // Corrupt or unreadable settings must never block startup.
            return new AppSettings();
        }
    }

    /// <summary>Writes the settings file, creating the folder when needed.</summary>
    public void Save(AppSettings settings)
    {
        try
        {
            var folder = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
            }

            File.WriteAllText(_settingsPath, JsonSerializer.Serialize(settings, JsonOptions));
        }
        catch (Exception)
        {
            // Saving preferences is best-effort only.
        }
    }
}
