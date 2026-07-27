using System.IO;
using SVGViewer.Models;
using SVGViewer.Services;
using Xunit;

namespace SVGViewer.Tests;

public class SettingsServiceTests : IDisposable
{
    private readonly string _folder;
    private readonly string _path;

    public SettingsServiceTests()
    {
        _folder = Path.Combine(Path.GetTempPath(), "SVGViewerTests", Guid.NewGuid().ToString("N"));
        _path = Path.Combine(_folder, "settings.json");
    }

    [Fact]
    public void Defaults_are_dutch_and_medium()
    {
        var settings = new SettingsService(_path).Load();

        Assert.Equal("nl", settings.Language);
        Assert.Equal(PreviewSize.Medium, settings.PreviewSize);
        Assert.Equal(FolderFilterMode.All, settings.FilterMode);
    }

    [Fact]
    public void Preferences_survive_a_save_and_load()
    {
        var service = new SettingsService(_path);

        service.Save(new AppSettings
        {
            Language = "de",
            PreviewSize = PreviewSize.Small,
            FilterMode = FolderFilterMode.SvgOnly,
            LastDrive = @"D:\"
        });

        var loaded = service.Load();

        Assert.Equal("de", loaded.Language);
        Assert.Equal(PreviewSize.Small, loaded.PreviewSize);
        Assert.Equal(FolderFilterMode.SvgOnly, loaded.FilterMode);
        Assert.Equal(@"D:\", loaded.LastDrive);
    }

    [Fact]
    public void Enums_are_stored_as_readable_names()
    {
        var service = new SettingsService(_path);
        service.Save(new AppSettings { PreviewSize = PreviewSize.Small });

        var json = File.ReadAllText(_path);

        Assert.Contains("Small", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Corrupt_file_falls_back_to_defaults()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(_path, "{ this is not valid json");

        var settings = new SettingsService(_path).Load();

        Assert.Equal("nl", settings.Language);
        Assert.Equal(PreviewSize.Medium, settings.PreviewSize);
    }

    [Fact]
    public void Saving_to_an_unwritable_path_does_not_throw()
    {
        // A settings file must never be able to crash the application.
        var service = new SettingsService(@"Z:\nope\settings.json");

        service.Save(new AppSettings());
        var settings = service.Load();

        Assert.Equal("nl", settings.Language);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_folder))
            {
                Directory.Delete(_folder, recursive: true);
            }
        }
        catch (IOException)
        {
        }
    }
}
