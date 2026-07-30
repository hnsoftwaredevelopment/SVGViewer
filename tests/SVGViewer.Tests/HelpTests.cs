using System.Collections.Generic;
using System.IO;
using System.Linq;
using SVGViewer.Services;
using Xunit;

namespace SVGViewer.Tests;

public class MarkdownParserTests
{
    private static string Text(IReadOnlyList<MarkdownSegment> segments) =>
        string.Concat(segments.Select(s => s.Text));

    [Fact]
    public void Hash_line_is_a_heading()
    {
        var block = Assert.Single(MarkdownParser.Parse("# Title"));
        Assert.Equal(MarkdownBlockKind.Heading1, block.Kind);
        Assert.Equal("Title", Text(block.Lines[0]));
    }

    [Fact]
    public void Double_hash_is_a_sub_heading()
    {
        var block = Assert.Single(MarkdownParser.Parse("## Sub"));
        Assert.Equal(MarkdownBlockKind.Heading2, block.Kind);
        Assert.Equal("Sub", Text(block.Lines[0]));
    }

    [Fact]
    public void Plain_text_is_a_paragraph()
    {
        var block = Assert.Single(MarkdownParser.Parse("Hello world."));
        Assert.Equal(MarkdownBlockKind.Paragraph, block.Kind);
        Assert.Equal("Hello world.", Text(block.Lines[0]));
    }

    [Fact]
    public void Dash_lines_become_a_bullet_list()
    {
        var block = Assert.Single(MarkdownParser.Parse("- one\n- two\n- three"));
        Assert.Equal(MarkdownBlockKind.BulletList, block.Kind);
        Assert.Equal(3, block.Lines.Count);
        Assert.Equal("one", Text(block.Lines[0]));
    }

    [Fact]
    public void Wrapped_bullet_continuation_is_merged()
    {
        var block = Assert.Single(MarkdownParser.Parse("- first line\n  continued\n- second"));
        Assert.Equal(2, block.Lines.Count);
        Assert.Equal("first line continued", Text(block.Lines[0]));
    }

    [Fact]
    public void Bold_markup_is_flagged()
    {
        var segments = MarkdownParser.ParseInline("This is **bold** text.");
        Assert.Contains(segments, s => s.Bold && s.Text == "bold");
        Assert.Equal("This is bold text.", Text(segments));
    }

    [Fact]
    public void Code_markup_is_flagged()
    {
        var segments = MarkdownParser.ParseInline("Use `code` now.");
        Assert.Contains(segments, s => s.Code && s.Text == "code");
    }

    [Fact]
    public void Italic_markup_is_flagged()
    {
        var segments = MarkdownParser.ParseInline("An *italic* word.");
        Assert.Contains(segments, s => s.Italic && s.Text == "italic");
    }
}

public class HelpServiceTests : IDisposable
{
    private readonly string _help;

    public HelpServiceTests()
    {
        _help = Path.Combine(Path.GetTempPath(), "svgv-help-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_help);
    }

    public void Dispose()
    {
        try { Directory.Delete(_help, true); } catch { }
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
        File.WriteAllText(Path.Combine(_help, "QuickReference.nl.md"), "# NL");
        var service = new HelpService(_help);

        Assert.EndsWith("QuickReference.nl.md", service.ResolveGuidePath("de"));
    }

    [Fact]
    public void ResolveGuidePath_uses_requested_language_when_present()
    {
        File.WriteAllText(Path.Combine(_help, "QuickReference.nl.md"), "# NL");
        File.WriteAllText(Path.Combine(_help, "QuickReference.en.md"), "# EN");
        var service = new HelpService(_help);

        Assert.EndsWith("QuickReference.en.md", service.ResolveGuidePath("en"));
    }

    [Fact]
    public void ReadQuickReference_returns_the_markdown()
    {
        File.WriteAllText(Path.Combine(_help, "QuickReference.nl.md"), "# Welkom\n\nHallo.");
        var service = new HelpService(_help);

        var markdown = service.ReadQuickReference("nl");

        Assert.Contains("# Welkom", markdown);
        Assert.Contains("Hallo.", markdown);
    }

    [Fact]
    public void ReadQuickReference_throws_when_no_file_exists()
    {
        var service = new HelpService(_help);
        Assert.Throws<FileNotFoundException>(() => service.ReadQuickReference("nl"));
    }
}
