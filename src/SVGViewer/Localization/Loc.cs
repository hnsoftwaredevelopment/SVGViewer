using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace SVGViewer.Localization;

/// <summary>
/// Binding-friendly access to the localized resources in Resources\Strings*.resx.
/// XAML binds through the indexer, e.g.
///   Text="{Binding Source={x:Static loc:Loc.Instance}, Path=[LabelDrive]}"
/// Calling <see cref="SetCulture"/> switches language at runtime and refreshes
/// every binding without restarting the application.
/// </summary>
public sealed class Loc : INotifyPropertyChanged
{
    /// <summary>Supported UI cultures. Dutch is the neutral/default culture.</summary>
    public static readonly string[] SupportedCultures = { "nl", "en", "de" };

    private static readonly ResourceManager Resources =
        new("SVGViewer.Resources.Strings", typeof(Loc).Assembly);

    public static Loc Instance { get; } = new();

    private Loc() { }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Indexer used by XAML bindings. Returns !key! when a key is missing.</summary>
    public string this[string key] => Get(key);

    /// <summary>Looks up a resource string for the active UI culture.</summary>
    public static string Get(string key)
    {
        try
        {
            return Resources.GetString(key, CultureInfo.CurrentUICulture) ?? $"!{key}!";
        }
        catch (MissingManifestResourceException)
        {
            return $"!{key}!";
        }
    }

    /// <summary>Looks up a resource string and formats it with the given arguments.</summary>
    public static string Format(string key, params object[] args)
        => string.Format(CultureInfo.CurrentUICulture, Get(key), args);

    /// <summary>
    /// Switches the active UI culture and notifies all bindings.
    /// </summary>
    public static void SetCulture(string cultureName)
    {
        var culture = new CultureInfo(cultureName);

        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        // Notifying with "Item[]" tells WPF that every indexer binding is stale.
        Instance.PropertyChanged?.Invoke(Instance, new PropertyChangedEventArgs("Item[]"));
    }
}
