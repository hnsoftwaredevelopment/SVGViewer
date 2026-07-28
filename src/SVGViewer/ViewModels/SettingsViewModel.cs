using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SVGViewer.Localization;
using SVGViewer.Models;
using SVGViewer.Services;

namespace SVGViewer.ViewModels;

/// <summary>
/// Backs the Settings screen: language choice and the delete-confirmation
/// toggle. Every change is applied immediately and persisted.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private readonly AppSettings _settings;
    private bool _isInitializing = true;

    public SettingsViewModel(SettingsService settingsService, AppSettings settings)
    {
        _settingsService = settingsService;
        _settings = settings;

        LanguageChoices = new ObservableCollection<LanguageChoice>
        {
            new("nl", "Nederlands"),
            new("en", "English"),
            new("de", "Deutsch")
        };

        _selectedLanguage = LanguageChoices.FirstOrDefault(l => l.CultureName == settings.Language)
                            ?? LanguageChoices[0];
        _confirmBeforeDelete = settings.ConfirmBeforeDelete;

        _isInitializing = false;
    }

    public ObservableCollection<LanguageChoice> LanguageChoices { get; }

    [ObservableProperty]
    private LanguageChoice _selectedLanguage;

    [ObservableProperty]
    private bool _confirmBeforeDelete;

    partial void OnSelectedLanguageChanged(LanguageChoice value)
    {
        if (_isInitializing)
        {
            return;
        }

        Loc.SetCulture(value.CultureName);
        _settings.Language = value.CultureName;
        _settingsService.Save(_settings);
    }

    partial void OnConfirmBeforeDeleteChanged(bool value)
    {
        if (_isInitializing)
        {
            return;
        }

        _settings.ConfirmBeforeDelete = value;
        _settingsService.Save(_settings);
    }
}
