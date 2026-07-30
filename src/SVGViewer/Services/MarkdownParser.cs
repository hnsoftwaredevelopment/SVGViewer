using System.Collections.Generic;
using System.Text;

namespace SVGViewer.Services;

/// <summary>Kind of block the quick-reference Markdown produces.</summary>
public enum MarkdownBlockKind
{
    Heading1,
    Heading2,
    Paragraph,
    BulletList
}

/// <summary>A styled run of inline text.</summary>
public sealed record MarkdownSegment(string Text, bool Bold = false, bool Italic = false, bool Code = false);

/// <summary>
/// A parsed block. Headings and paragraphs have a single line of segments; a
/// bullet list has one line of segments per item.
/// </summary>
public sealed class MarkdownBlock
{
    public MarkdownBlockKind Kind { get; init; }

    public IReadOnlyList<IReadOnlyList<MarkdownSegment>> Lines { get; init; } =
        new List<IReadOnlyList<MarkdownSegment>>();
}

/// <summary>
/// Pure (WPF-free) parser for the small Markdown subset used by the quick
/// reference: headings (# / ##), paragraphs, bullet lists (-), and inline
/// <c>**bold**</c>, <c>*italic*</c> and <c>`code`</c>. Kept free of WPF types so
/// the logic is unit-testable; <see cref="MarkdownToFlowDocument"/> renders it.
/// </summary>
public static class MarkdownParser
{
    public static IReadOnlyList<MarkdownBlock> Parse(string? markdown)
    {
        var blocks = new List<MarkdownBlock>();
        var lines = (markdown ?? string.Empty)
            .Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');

        var i = 0;
        var paragraph = new StringBuilder();

        void FlushParagraph()
        {
            if (paragraph.Length > 0)
            {
                blocks.Add(new MarkdownBlock
                {
                    Kind = MarkdownBlockKind.Paragraph,
                    Lines = new[] { ParseInline(paragraph.ToString()) }
                });
                paragraph.Clear();
            }
        }

        while (i < lines.Length)
        {
            var trimmed = lines[i].Trim();

            if (trimmed.Length == 0)
            {
                FlushParagraph();
                i++;
            }
            else if (trimmed.StartsWith("## "))
            {
                FlushParagraph();
                blocks.Add(Single(MarkdownBlockKind.Heading2, trimmed[3..].Trim()));
                i++;
            }
            else if (trimmed.StartsWith("# "))
            {
                FlushParagraph();
                blocks.Add(Single(MarkdownBlockKind.Heading1, trimmed[2..].Trim()));
                i++;
            }
            else if (trimmed.StartsWith("- "))
            {
                FlushParagraph();
                blocks.Add(BulletList(lines, ref i));
            }
            else
            {
                if (paragraph.Length > 0) paragraph.Append(' ');
                paragraph.Append(trimmed);
                i++;
            }
        }

        FlushParagraph();
        return blocks;
    }

    private static MarkdownBlock Single(MarkdownBlockKind kind, string text) => new()
    {
        Kind = kind,
        Lines = new[] { ParseInline(text) }
    };

    private static MarkdownBlock BulletList(string[] lines, ref int i)
    {
        var items = new List<IReadOnlyList<MarkdownSegment>>();

        while (i < lines.Length && lines[i].Trim().StartsWith("- "))
        {
            var item = new StringBuilder(lines[i].Trim()[2..].Trim());
            i++;

            // Wrapped continuation lines belong to the current bullet.
            while (i < lines.Length)
            {
                var t = lines[i].Trim();
                if (t.Length == 0 || t.StartsWith("- ") || t.StartsWith("#")) break;
                item.Append(' ').Append(t);
                i++;
            }

            items.Add(ParseInline(item.ToString()));
        }

        return new MarkdownBlock { Kind = MarkdownBlockKind.BulletList, Lines = items };
    }

    /// <summary>Splits a line into styled segments (**bold**, *italic*, `code`).</summary>
    public static IReadOnlyList<MarkdownSegment> ParseInline(string text)
    {
        var result = new List<MarkdownSegment>();
        var buffer = new StringBuilder();

        void Flush()
        {
            if (buffer.Length > 0)
            {
                result.Add(new MarkdownSegment(buffer.ToString()));
                buffer.Clear();
            }
        }

        var i = 0;
        while (i < text.Length)
        {
            if (text[i] == '*' && i + 1 < text.Length && text[i + 1] == '*')
            {
                var end = text.IndexOf("**", i + 2, StringComparison.Ordinal);
                if (end > 0)
                {
                    Flush();
                    result.Add(new MarkdownSegment(text.Substring(i + 2, end - (i + 2)), Bold: true));
                    i = end + 2;
                    continue;
                }
            }
            else if (text[i] == '`')
            {
                var end = text.IndexOf('`', i + 1);
                if (end > 0)
                {
                    Flush();
                    result.Add(new MarkdownSegment(text.Substring(i + 1, end - (i + 1)), Code: true));
                    i = end + 1;
                    continue;
                }
            }
            else if (text[i] == '*')
            {
                var end = text.IndexOf('*', i + 1);
                if (end > 0)
                {
                    Flush();
                    result.Add(new MarkdownSegment(text.Substring(i + 1, end - (i + 1)), Italic: true));
                    i = end + 1;
                    continue;
                }
            }

            buffer.Append(text[i]);
            i++;
        }

        Flush();
        return result;
    }
}
