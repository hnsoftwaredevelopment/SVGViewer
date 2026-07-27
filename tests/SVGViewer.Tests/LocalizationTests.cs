using System.Globalization;
using SVGViewer.Localization;
using Xunit;

namespace SVGViewer.Tests;

/// <summary>
/// A missing resource shows up as "!Key!" in the UI, so these tests assert that
/// every supported culture actually resolves real text.
/// </summary>
public class LocalizationTests : IDisposable
{
    private readonly CultureInfo _original = CultureInfo.CurrentUICulture;

    [Theory]
    [InlineData("nl")]
    [InlineData("en")]
    [InlineData("de")]
    public void Every_culture_resolves_the_main_labels(string culture)
    {
        Loc.SetCulture(culture);

        foreach (var key in new[] { "AppTitle", "LabelDrive", "LabelView", "LabelPreviewSize", "LabelLanguage" })
        {
            var value = Loc.Get(key);

            Assert.False(string.IsNullOrWhiteSpace(value));
            Assert.DoesNotContain("!", value);
        }
    }

    [Fact]
    public void Missing_key_is_reported_instead_of_throwing()
    {
        Assert.Equal("!NoSuchKeyExists!", Loc.Get("NoSuchKeyExists"));
    }

    [Fact]
    public void Dutch_is_the_default_language()
    {
        Loc.SetCulture("nl");

        Assert.Equal("Schijf:", Loc.Get("LabelDrive"));
    }

    [Fact]
    public void Switching_culture_changes_the_text()
    {
        Loc.SetCulture("nl");
        var dutch = Loc.Get("FilterSvgOnly");

        Loc.SetCulture("de");
        var german = Loc.Get("FilterSvgOnly");

        Assert.NotEqual(dutch, german);
    }

    [Fact]
    public void Switching_culture_raises_a_change_notification()
    {
        var raised = false;
        void Handler(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => raised = true;

        Loc.Instance.PropertyChanged += Handler;
        try
        {
            Loc.SetCulture("en");
        }
        finally
        {
            Loc.Instance.PropertyChanged -= Handler;
        }

        Assert.True(raised);
    }

    [Fact]
    public void Format_inserts_arguments()
    {
        Loc.SetCulture("nl");

        var text = Loc.Format("TooltipContainsSvg", 3);

        Assert.Contains("3", text);
        Assert.DoesNotContain("{0}", text);
    }

    [Fact]
    public void Three_cultures_are_supported()
    {
        Assert.Equal(new[] { "nl", "en", "de" }, Loc.SupportedCultures);
    }

    public void Dispose() => Loc.SetCulture(_original.Name switch
    {
        "" => "nl",
        var name => name
    });
}
