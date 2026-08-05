using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using SVGViewer.Localization;
using SVGViewer.Services;
using SVGViewer.ViewModels;
using SVGViewer.Views;

namespace SVGViewer;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Catch and log failures so problems in the field are traceable instead of
        // crashing silently.
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        Logger.Info($"SVG Viewer starting (v{version}).");

        // Register the Syncfusion key when syncfusionlicense.txt is present.
        LicenseManager.RegisterIfAvailable();

        // Restore preferences and apply the saved language before any UI is built.
        var settingsService = new SettingsService();
        var settings = settingsService.Load();
        Loc.SetCulture(settings.Language);

        // Show the splash (with the version number) first, then build the main window.
        var splash = new SplashWindow();
        splash.Show();

        var viewModel = new MainViewModel(settingsService, settings);
        var window = new MainWindow(viewModel, settingsService, settings);
        window.Show();

        // The splash stays on top for a moment, then closes to reveal the window.
        var splashTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        splashTimer.Tick += (_, _) =>
        {
            splashTimer.Stop();
            splash.Close();
        };
        splashTimer.Start();
    }

    /// <summary>
    /// A failure on the UI thread: log it, tell the user where the log is, and keep
    /// the app alive rather than crashing on a non-fatal glitch.
    /// </summary>
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Logger.Error("Unhandled UI exception.", e.Exception);

        MessageBox.Show(
            Loc.Format("MsgUnexpectedError", Logger.LogDirectory),
            Loc.Get("AppTitle"),
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        e.Handled = true;
    }

    private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Logger.Error("Unhandled non-UI exception.", e.ExceptionObject as Exception);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Logger.Error("Unobserved task exception.", e.Exception);
        e.SetObserved();
    }
}
