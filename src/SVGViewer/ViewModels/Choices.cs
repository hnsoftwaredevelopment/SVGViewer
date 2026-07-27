using CommunityToolkit.Mvvm.ComponentModel;
using SVGViewer.Localization;

namespace SVGViewer.ViewModels;

/// <summary>
/// A dropdown entry whose caption comes from the resource files and updates
/// itself when the user switches language.
/// </summary>
public sealed class LocalizedChoice<T> : ObservableObject
{
    private readonly string _resourceKey;

    public LocalizedChoice(T value, string resourceKey)
    {
        Value = value;
        _resourceKey = resourceKey;

        Loc.Instance.PropertyChanged += (_, _) => OnPropertyChanged(nameof(DisplayName));
    }

    public T Value { get; }

    public string DisplayName => Loc.Get(_resourceKey);

    public override string ToString() => DisplayName;
}

/// <summary>
/// A language entry. Language names are shown in their own language and are
/// therefore not translated.
/// </summary>
public sealed record LanguageChoice(string CultureName, string DisplayName)
{
    public override string ToString() => DisplayName;
}

/// <summary>A drive entry, e.g. "C:\ (Windows)".</summary>
public sealed record DriveChoice(string RootPath, string DisplayName)
{
    public override string ToString() => DisplayName;
}
