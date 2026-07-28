using System.Globalization;
using System.IO;
using SVGViewer.Localization;
using SVGViewer.Models;
using SVGViewer.Services;
using SVGViewer.ViewModels;
using Xunit;

namespace SVGViewer.Tests;

public class SettingsViewModelTests : IDisposable
{
    private readonly string _folder;
    private readonly string _path;
    private readonly CultureInfo _originalCulture = CultureInfo.CurrentUICulture;

    public SettingsViewModelTests()
    {
        _folder = Path.Combine(Path.GetTempPath(), "SVGViewerTests", Guid.NewGuid().ToString("N"));
        _path = Path.Combine(_folder, "settings.json");
    }

    private SettingsViewModel Create(AppSettings settings) =>
        new(new SettingsService(_path), settings);

    [Fact]
    public void Initializes_from_the_supplied_settings()
    {
        var vm = Create(new AppSettings { Language = "de", ConfirmBeforeDelete = false });

        Assert.Equal("de", vm.SelectedLanguage.CultureName);
        Assert.False(vm.ConfirmBeforeDelete);
    }

    [Fact]
    public void Construction_does_not_persist_anything()
    {
        _ = Create(new AppSettings());

        Assert.False(File.Exists(_path));
    }

    [Fact]
    public void Choosing_a_language_switches_culture_and_persists()
    {
        var vm = Create(new AppSettings { Language = "nl" });

        vm.SelectedLanguage = vm.LanguageChoices.First(l => l.CultureName == "de");

        Assert.Equal("de", CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);

        var reloaded = new SettingsService(_path).Load();
        Assert.Equal("de", reloaded.Language);
    }

    [Fact]
    public void Toggling_confirm_before_delete_persists()
    {
        var vm = Create(new AppSettings { ConfirmBeforeDelete = true });

        vm.ConfirmBeforeDelete = false;

        var reloaded = new SettingsService(_path).Load();
        Assert.False(reloaded.ConfirmBeforeDelete);
    }

    [Fact]
    public void Offers_the_three_supported_languages()
    {
        var vm = Create(new AppSettings());

        Assert.Equal(new[] { "nl", "en", "de" },
            vm.LanguageChoices.Select(l => l.CultureName).ToArray());
    }

    public void Dispose()
    {
        Loc.SetCulture(_originalCulture.TwoLetterISOLanguageName switch
        {
            "nl" or "en" or "de" => _originalCulture.TwoLetterISOLanguageName,
            _ => "nl"
        });

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
