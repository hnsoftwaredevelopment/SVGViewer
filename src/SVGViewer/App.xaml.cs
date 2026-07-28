using System.Windows;
using SVGViewer.Localization;
using SVGViewer.Services;
using SVGViewer.ViewModels;

namespace SVGViewer;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Register the Syncfusion key when syncfusionlicense.txt is present.
        LicenseManager.RegisterIfAvailable();

        // Restore preferences and apply the saved language before any UI is built.
        var settingsService = new SettingsService();
        var settings = settingsService.Load();
        Loc.SetCulture(settings.Language);

        var viewModel = new MainViewModel(settingsService, settings);
        var window = new MainWindow(viewModel, settingsService, settings);
        window.Show();
    }
}
