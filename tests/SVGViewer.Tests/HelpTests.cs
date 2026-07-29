using System.IO;
using SVGViewer.Services;
using Xunit;

namespace SVGViewer.Tests;

public class MarkdownToHtmlTests
{
    [Fact]
    public void Renders_headings()
    {
        var html = MarkdownToHtml.Convert("# Title\n\n## Sub");
        Assert.Contains("<h1>Title</h1>", html);
        Assert.Contains("<h2>Sub</h2>", html);
    }

    [Fact]
    public void Renders_bold_italic_and_inline_code()
    {
        var html = MarkdownToHtml.Convert("This is **bold**, *italic* and `code`.");
        Assert.Contains("<strong>bold</strong>", html);
        Assert.Contains("<em>italic</em>", html);
        Assert.Contains("<code>code</code>", html);
    }

    [Fact]
    public void Renders_unordered_and_ordered_lists()
    {
        var ul = MarkdownToHtml.Convert("- one\n- two");
        Assert.Contains("<ul>", ul);
        Assert.Contains("<li>one</li>", ul);

        var ol = MarkdownToHtml.Convert("1. first\n2. second");
        Assert.Contains("<ol>", ol);
        Assert.Contains("<li>second</li>", ol);
    }

    [Fact]
    public void Renders_links()
    {
        var html = MarkdownToHtml.Convert("See [the site](https://example.com).");
        Assert.Contains("<a href=\"https://example.com\">the site</a>", html);
    }

    [Fact]
    public void Rewrites_relative_image_sources_against_base()
    {
        var html = MarkdownToHtml.Convert("![shot](images/main.png)", null, "file:///C:/app/Help/");
        Assert.Contains("src=\"file:///C:/app/Help/images/main.png\"", html);
        Assert.Contains("alt=\"shot\"", html);
    }

    [Fact]
    public void Leaves_absolute_image_sources_untouched()
    {
        var html = MarkdownToHtml.Convert("![x](https://cdn/x.png)", null, "file:///C:/app/Help/images/");
        Assert.Contains("src=\"https://cdn/x.png\"", html);
    }

    [Fact]
    public void Escapes_html_special_characters()
    {
        var html = MarkdownToHtml.Convert("a < b & c > d");
        Assert.Contains("a &lt; b &amp; c &gt; d", html);
    }

    [Fact]
    public void Fenced_code_is_preserved_and_escaped()
    {
        var html = MarkdownToHtml.Convert("```\n<tag> & stuff\n```");
        Assert.Contains("<pre><code>", html);
        Assert.Contains("&lt;tag&gt; &amp; stuff", html);
    }

    [Fact]
    public void Produces_a_full_document()
    {
        var html = MarkdownToHtml.Convert("# Hi", "MyTitle");
        Assert.StartsWith("<!DOCTYPE html>", html);
        Assert.Contains("<title>MyTitle</title>", html);
    }
}

public class HelpServiceTests : IDisposable
{
    private readonly string _help;
    private readonly string _out;

    public HelpServiceTests()
    {
        _help = Path.Combine(Path.GetTempPath(), "svgv-help-" + Guid.NewGuid().ToString("N"));
        _out = Path.Combine(Path.GetTempPath(), "svgv-out-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_help);
    }

    public void Dispose()
    {
        try { Directory.Delete(_help, true); } catch { }
        try { Directory.Delete(_out, true); } catch { }
    }

    [Theory]
    [InlineData("nl", "nl")]
    [InlineData("en-US", "en")]
    [InlineData("de", "de")]
    [InlineData("fr", "nl")]
    [InlineData(null, "nl")]
    public void NormalizeCulture_maps_to_supported_language(string? input, string expected) =>
        Assert.Equal(expected, HelpService.NormalizeCulture(input));

    [Fact]
    public void ResolveGuidePath_falls_back_to_dutch_when_language_missing()
    {
        File.WriteAllText(Path.Combine(_help, "UserGuide.nl.md"), "# NL");
        var service = new HelpService(_help, _out);

        Assert.EndsWith("UserGuide.nl.md", service.ResolveGuidePath("de"));
    }

    [Fact]
    public void ResolveGuidePath_uses_requested_language_when_present()
    {
        File.WriteAllText(Path.Combine(_help, "UserGuide.nl.md"), "# NL");
        File.WriteAllText(Path.Combine(_help, "UserGuide.en.md"), "# EN");
        var service = new HelpService(_help, _out);

        Assert.EndsWith("UserGuide.en.md", service.ResolveGuidePath("en"));
    }

    [Fact]
    public void GenerateHelpFile_writes_converted_html()
    {
        File.WriteAllText(Path.Combine(_help, "UserGuide.nl.md"), "# Welkom\n\nHallo.");
        var service = new HelpService(_help, _out);

        var path = service.GenerateHelpFile("nl");

        Assert.True(File.Exists(path));
        var html = File.ReadAllText(path);
        Assert.Contains("<h1>Welkom</h1>", html);
        Assert.Contains("<!DOCTYPE html>", html);
    }

    [Fact]
    public void GenerateHelpFile_throws_when_no_guide_exists()
    {
        var service = new HelpService(_help, _out);
        Assert.Throws<FileNotFoundException>(() => service.GenerateHelpFile("nl"));
    }
}
